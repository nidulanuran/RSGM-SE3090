using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.JobSeeker;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services;

public enum UploadCvResult
{
    Success,
    InvalidFileType,
    FileTooLarge,
    EmptyFile
}

public class JobSeekerCvService
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx" };
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly HttpClient HttpClient = new();

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JobSeekerCvService> _logger;

    public JobSeekerCvService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<JobSeekerCvService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CvResponse?> GetMetadataAsync(Guid userId)
    {
        var cv = await _context.JobSeekerCvs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        return cv == null ? null : ToResponse(cv);
    }

    public async Task<(UploadCvResult Result, CvResponse? Cv)> UploadAsync(
        Guid userId,
        IFormFile file)
    {
        if (file.Length == 0)
        {
            return (UploadCvResult.EmptyFile, null);
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return (UploadCvResult.FileTooLarge, null);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension) ||
            !AllowedContentTypes.Contains(file.ContentType))
        {
            return (UploadCvResult.InvalidFileType, null);
        }

        var objectPath = $"{userId:N}/{Guid.NewGuid():N}{extension}";
        var existing = await _context.JobSeekerCvs
            .FirstOrDefaultAsync(x => x.UserId == userId);
        var oldObjectPath = existing?.StoredFileName;

        await UploadToSupabaseAsync(objectPath, file);

        try
        {
            if (existing == null)
            {
                existing = new JobSeekerCv
                {
                    UserId = userId
                };

                _context.JobSeekerCvs.Add(existing);
            }

            existing.FileName = file.FileName;
            existing.StoredFileName = objectPath;
            existing.ContentType = file.ContentType;
            existing.FileSizeBytes = file.Length;
            existing.UploadedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
        catch
        {
            // Avoid leaving a newly uploaded object orphaned if the DB save fails.
            await TryDeleteFromSupabaseAsync(objectPath);
            throw;
        }

        // Delete the previous object only after the new DB state is committed.
        if (!string.IsNullOrWhiteSpace(oldObjectPath) &&
            !string.Equals(oldObjectPath, objectPath, StringComparison.Ordinal))
        {
            await TryDeleteFromSupabaseAsync(oldObjectPath);
        }

        return (UploadCvResult.Success, ToResponse(existing));
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> GetFileForDownloadAsync(
        Guid userId)
    {
        var cv = await _context.JobSeekerCvs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cv == null)
        {
            return null;
        }

        var bytes = await DownloadFromSupabaseAsync(cv.StoredFileName);

        if (bytes == null)
        {
            return null;
        }

        return (new MemoryStream(bytes, writable: false), cv.ContentType, cv.FileName);
    }

    public async Task<bool> DeleteAsync(Guid userId)
    {
        var cv = await _context.JobSeekerCvs
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cv == null)
        {
            return false;
        }

        await DeleteFromSupabaseAsync(cv.StoredFileName);

        _context.JobSeekerCvs.Remove(cv);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task UploadToSupabaseAsync(string objectPath, IFormFile file)
    {
        var settings = GetSupabaseSettings();
        var url = BuildObjectUrl(settings.Url, settings.Bucket, objectPath);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        AddAuthHeaders(request, settings.ServiceRoleKey);
        request.Headers.TryAddWithoutValidation("x-upsert", "false");

        await using var input = file.OpenReadStream();
        using var content = new StreamContent(input);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        content.Headers.ContentLength = file.Length;
        request.Content = content;

        using var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Supabase CV upload failed ({(int)response.StatusCode} {response.StatusCode}): {body}");
        }
    }

    private async Task<byte[]?> DownloadFromSupabaseAsync(string objectPath)
    {
        var settings = GetSupabaseSettings();
        var url = BuildAuthenticatedObjectUrl(settings.Url, settings.Bucket, objectPath);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeaders(request, settings.ServiceRoleKey);

        using var response = await HttpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Supabase CV download failed ({(int)response.StatusCode} {response.StatusCode}): {body}");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    private async Task DeleteFromSupabaseAsync(string objectPath)
    {
        if (string.IsNullOrWhiteSpace(objectPath))
        {
            return;
        }

        var settings = GetSupabaseSettings();
        var url = $"{settings.Url.TrimEnd('/')}/storage/v1/object/{Uri.EscapeDataString(settings.Bucket)}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        AddAuthHeaders(request, settings.ServiceRoleKey);

        var payload = JsonSerializer.Serialize(new
        {
            prefixes = new[] { objectPath }
        });

        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Supabase CV delete failed ({(int)response.StatusCode} {response.StatusCode}): {body}");
        }
    }

    private async Task TryDeleteFromSupabaseAsync(string objectPath)
    {
        try
        {
            await DeleteFromSupabaseAsync(objectPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to clean up Supabase CV object {ObjectPath}", objectPath);
        }
    }

    private (string Url, string ServiceRoleKey, string Bucket) GetSupabaseSettings()
    {
        var url = _configuration["Supabase:Url"];
        var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"];
        var bucket = _configuration["Supabase:CvBucket"] ?? "cvs";

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException(
                "Supabase:Url is not configured. Add it with dotnet user-secrets or an environment variable.");
        }

        if (string.IsNullOrWhiteSpace(serviceRoleKey))
        {
            throw new InvalidOperationException(
                "Supabase:ServiceRoleKey is not configured. Add it with dotnet user-secrets or an environment variable.");
        }

        return (url, serviceRoleKey, bucket);
    }

    private static void AddAuthHeaders(HttpRequestMessage request, string serviceRoleKey)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
        request.Headers.TryAddWithoutValidation("apikey", serviceRoleKey);
    }

    private static string BuildObjectUrl(string baseUrl, string bucket, string objectPath)
    {
        return $"{baseUrl.TrimEnd('/')}/storage/v1/object/{Uri.EscapeDataString(bucket)}/{EncodeObjectPath(objectPath)}";
    }

    private static string BuildAuthenticatedObjectUrl(string baseUrl, string bucket, string objectPath)
    {
        return $"{baseUrl.TrimEnd('/')}/storage/v1/object/authenticated/{Uri.EscapeDataString(bucket)}/{EncodeObjectPath(objectPath)}";
    }

    private static string EncodeObjectPath(string objectPath)
    {
        return string.Join(
            "/",
            objectPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
    }

    private static CvResponse ToResponse(JobSeekerCv cv)
    {
        return new CvResponse
        {
            FileName = cv.FileName,
            FileSizeBytes = cv.FileSizeBytes,
            UploadedAt = cv.UploadedAt
        };
    }
}
