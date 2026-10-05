using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Common;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;
using RSGM.Api.Services;

namespace RSGM.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.HiringPanelist)]
[Route("api/hiring/panelist/shortlists")]
public class PanelistCandidateController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JobSeekerCvService _cvService;

    public PanelistCandidateController(
        ApplicationDbContext db,
        JobSeekerCvService cvService)
    {
        _db = db;
        _cvService = cvService;
    }

    private Guid UserId =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var id)
            ? id
            : Guid.Empty;

    private IQueryable<Application> AccessibleApplications =>
        _db.Applications.Where(application =>
            (application.Status == ApplicationStatus.Shortlisted ||
             application.Status == ApplicationStatus.Interview ||
             application.Status == ApplicationStatus.Offer) &&
        application.JobPosting.CompanyId != null &&
        application.JobPosting.CompanyEntity != null &&
        application.JobPosting.CompanyEntity.IsActive &&
        _db.ShortlistDispatches.Any(dispatch =>
            dispatch.JobPostingId == application.JobPostingId &&
            dispatch.PanelistId == UserId) &&
        _db.CompanyMembers.Any(member =>
            member.UserId == UserId &&
            member.IsActive &&
            member.Company.IsActive &&
            member.CompanyId ==
                application.JobPosting.CompanyId));

    [HttpGet("applications/{applicationId:guid}/candidate")]
    public async Task<IActionResult> GetCandidate(Guid applicationId)
    {
        var application = await AccessibleApplications
            .Where(application =>
                application.Id == applicationId)
            .Include(application => application.User)
            .Include(application => application.JobPosting)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (application == null)
        {
            return NotFound(new
            {
                message =
                    "This candidate is not available to you."
            });
        }

        var candidateId = application.UserId;

        var profile = await _db.JobSeekerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(profile =>
                profile.UserId == candidateId);

        var skills = await _db.JobSeekerSkills
            .AsNoTracking()
            .Where(skill =>
                skill.UserId == candidateId)
            .Select(skill => new
            {
                skill.Skill.Name,
                skill.ProficiencyLevel
            })
            .OrderBy(skill => skill.Name)
            .ToListAsync();

        var education = await _db.EducationRecords
            .AsNoTracking()
            .Where(record =>
                record.UserId == candidateId)
            .OrderByDescending(record =>
                record.StartDate)
            .Select(record => new
            {
                record.Degree,
                record.Institution,
                record.FieldOfStudy,
                record.StartDate,
                record.EndDate,
                record.IsCurrent
            })
            .ToListAsync();

        var experience = await _db.WorkExperiences
            .AsNoTracking()
            .Where(record =>
                record.UserId == candidateId)
            .OrderByDescending(record =>
                record.StartDate)
            .Select(record => new
            {
                record.JobTitle,
                record.CompanyName,
                record.Location,
                record.StartDate,
                record.EndDate,
                record.IsCurrent
            })
            .ToListAsync();

        var cv = await _db.JobSeekerCvs
            .AsNoTracking()
            .Where(record =>
                record.UserId == candidateId)
            .Select(record => new
            {
                record.FileName
            })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            Id = application.Id,
            JobTitle =
                application.JobPosting.Title,
            FullName =
                application.User.FullName,
            Email =
                application.User.Email,
            PhoneNumber =
                application.User.PhoneNumber,

            Headline =
                profile?.Headline,
            Location =
                profile?.Location,
            Bio =
                profile?.Bio,

            LinkedInUrl =
                profile?.LinkedInUrl,
            GitHubUrl =
                profile?.GitHubUrl,
            PortfolioUrl =
                profile?.PortfolioUrl,

            Skills = skills,
            Education = education,
            WorkExperience = experience,

            HasCv = cv != null,
            CvFileName =
                cv?.FileName
        });
    }

    [HttpGet("applications/{applicationId:guid}/cv")]
    public async Task<IActionResult> DownloadCv(Guid applicationId)
    {
        var candidateId = await AccessibleApplications
            .AsNoTracking()
            .Where(application =>
                application.Id == applicationId)
            .Select(application =>
                (Guid?)application.UserId)
            .FirstOrDefaultAsync();

        if (candidateId == null)
        {
            return NotFound(new
            {
                message =
                    "This candidate is not available to you."
            });
        }

        var cv =
            await _cvService.GetFileForDownloadAsync(
                candidateId.Value);

        if (cv == null)
        {
            return NotFound(new
            {
                message =
                    "No CV is available for this candidate."
            });
        }

        return File(
            cv.Value.Stream,
            cv.Value.ContentType,
            cv.Value.FileName);
    }
}