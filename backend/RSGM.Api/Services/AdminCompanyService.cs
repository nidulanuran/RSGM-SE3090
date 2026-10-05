using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RSGM.Api.Common;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.Companies;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services;

public enum CompanyOperationResult
{
    Success,
    NotFound,
    DuplicateName,
    CompanyInactive,
    UserNotFound,
    InvalidMemberRole,
    StaffRequiresCompany,
    HasRelatedData
}

public class AdminCompanyService
{
    private static readonly string[] CompanyRoles =
    {
        AppRoles.Recruiter,
        AppRoles.HRManager,
        AppRoles.HiringPanelist
    };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminCompanyService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<CompanyResponse>> GetAllAsync()
    {
        return await _context.Companies
            .AsNoTracking()
            .OrderBy(company => company.Name)
            .Select(company => new CompanyResponse
            {
                Id = company.Id,
                Name = company.Name,
                Description = company.Description,
                Website = company.Website,
                LogoUrl = company.LogoUrl,
                IsActive = company.IsActive,
                MemberCount = company.Members.Count,
                JobPostingCount = company.JobPostings.Count,
                CreatedAt = company.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CompanyResponse?> GetByIdAsync(Guid id)
    {
        return await _context.Companies
            .AsNoTracking()
            .Where(company => company.Id == id)
            .Select(company => new CompanyResponse
            {
                Id = company.Id,
                Name = company.Name,
                Description = company.Description,
                Website = company.Website,
                LogoUrl = company.LogoUrl,
                IsActive = company.IsActive,
                MemberCount = company.Members.Count,
                JobPostingCount = company.JobPostings.Count,
                CreatedAt = company.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(CompanyOperationResult Result, CompanyResponse? Company)> CreateAsync(
        CreateCompanyRequest request)
    {
        var name = request.Name.Trim();
        var normalizedName = NormalizeName(name);

        if (await NameExistsAsync(normalizedName))
            return (CompanyOperationResult.DuplicateName, null);

        var company = new Company
        {
            Name = name,
            NormalizedName = normalizedName,
            Description = Clean(request.Description),
            Website = Clean(request.Website),
            LogoUrl = Clean(request.LogoUrl)
        };

        _context.Companies.Add(company);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsDuplicateCompanyName(ex))
        {
            // Handles the small race condition where another request inserts
            // the same normalized company name after our existence check.
            return (CompanyOperationResult.DuplicateName, null);
        }

        return (CompanyOperationResult.Success, await GetByIdAsync(company.Id));
    }

    public async Task<(CompanyOperationResult Result, CompanyResponse? Company)> UpdateAsync(
        Guid id,
        UpdateCompanyRequest request)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null)
            return (CompanyOperationResult.NotFound, null);

        var name = request.Name.Trim();
        var normalizedName = NormalizeName(name);

        if (await NameExistsAsync(normalizedName, id))
            return (CompanyOperationResult.DuplicateName, null);

        company.Name = name;
        company.NormalizedName = normalizedName;
        company.Description = Clean(request.Description);
        company.Website = Clean(request.Website);
        company.LogoUrl = Clean(request.LogoUrl);
        company.UpdatedAt = DateTime.UtcNow;

        // Keep the temporary display field synchronized for existing UI/queries.
        await _context.JobPostings
            .Where(job => job.CompanyId == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.Company, name)
                .SetProperty(job => job.UpdatedAt, DateTime.UtcNow));

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsDuplicateCompanyName(ex))
        {
            return (CompanyOperationResult.DuplicateName, null);
        }

        return (CompanyOperationResult.Success, await GetByIdAsync(id));
    }

    public async Task<(CompanyOperationResult Result, CompanyResponse? Company)> UpdateStatusAsync(
        Guid id,
        bool isActive)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null)
            return (CompanyOperationResult.NotFound, null);

        company.IsActive = isActive;
        company.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (CompanyOperationResult.Success, await GetByIdAsync(id));
    }

    public async Task<CompanyOperationResult> DeleteAsync(Guid id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null)
            return CompanyOperationResult.NotFound;

        var hasMembers = await _context.CompanyMembers.AnyAsync(member => member.CompanyId == id);
        var hasJobs = await _context.JobPostings.AnyAsync(job => job.CompanyId == id);
        if (hasMembers || hasJobs)
            return CompanyOperationResult.HasRelatedData;

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();
        return CompanyOperationResult.Success;
    }

    public async Task<CompanyOperationResult> AssignMemberAsync(Guid companyId, Guid userId)
    {
        var company = await _context.Companies.FindAsync(companyId);
        if (company == null)
            return CompanyOperationResult.NotFound;
        if (!company.IsActive)
            return CompanyOperationResult.CompanyInactive;

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
            return CompanyOperationResult.UserNotFound;

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Any(role => CompanyRoles.Contains(role)))
            return CompanyOperationResult.InvalidMemberRole;

        var membership = await _context.CompanyMembers
            .FirstOrDefaultAsync(member => member.UserId == userId);

        if (membership == null)
        {
            _context.CompanyMembers.Add(new CompanyMember
            {
                CompanyId = companyId,
                UserId = userId
            });
        }
        else
        {
            membership.CompanyId = companyId;
            membership.IsActive = true;
            membership.JoinedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return CompanyOperationResult.Success;
    }

    public async Task<CompanyOperationResult> RemoveMemberAsync(Guid companyId, Guid userId)
    {
        var membership = await _context.CompanyMembers.FirstOrDefaultAsync(member =>
            member.CompanyId == companyId && member.UserId == userId);

        if (membership == null)
            return CompanyOperationResult.NotFound;

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(role => CompanyRoles.Contains(role)))
                return CompanyOperationResult.StaffRequiresCompany;
        }

        _context.CompanyMembers.Remove(membership);
        await _context.SaveChangesAsync();
        return CompanyOperationResult.Success;
    }

    private Task<bool> NameExistsAsync(string normalizedName, Guid? excludingId = null)
    {
        var query = _context.Companies
            .Where(company => company.NormalizedName == normalizedName);

        if (excludingId.HasValue)
            query = query.Where(company => company.Id != excludingId.Value);

        return query.AnyAsync();
    }

    private static string NormalizeName(string name) =>
        name.Trim().ToUpperInvariant();

    private static bool IsDuplicateCompanyName(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
        && postgresException.ConstraintName == "IX_Companies_NormalizedName";

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

}
