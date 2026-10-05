using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSGM.Api.Common;
using RSGM.Api.Models.DTOs.RecruiterJobs;
using RSGM.Api.Services;

namespace RSGM.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Recruiter)]
[Route("api/recruiter/applications")]
public class RecruiterApplicationsController : ControllerBase
{
    private readonly RecruiterApplicantService _applicants;
    private readonly JobSeekerCvService _cvs;

    public RecruiterApplicationsController(
        RecruiterApplicantService applicants,
        JobSeekerCvService cvs)
    {
        _applicants = applicants;
        _cvs = cvs;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] Guid? jobId)
    {
        var recruiterId = CurrentUserId();

        return recruiterId == null
            ? Unauthorized()
            : Ok(await _applicants.GetMineAsync(
                recruiterId.Value,
                jobId));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id)
    {
        var recruiterId = CurrentUserId();

        if (recruiterId == null)
        {
            return Unauthorized();
        }

        var applicant =
            (await _applicants.GetMineAsync(
                recruiterId.Value))
            .FirstOrDefault(
                item => item.Id == id);

        return applicant == null
            ? NotFound(new
            {
                message = "Application not found."
            })
            : Ok(applicant);
    }

    [HttpPatch("{id:guid}/decision")]
    public async Task<IActionResult> Review(
        Guid id,
        ReviewApplicantRequest request)
    {
        var recruiterId = CurrentUserId();

        if (recruiterId == null)
        {
            return Unauthorized();
        }

        var (result, applicant) =
            await _applicants.ReviewAsync(
                recruiterId.Value,
                id,
                request.Status);

        return result switch
        {
            RecruiterReviewResult.NotFound =>
                NotFound(new
                {
                    message = "Application not found."
                }),

            RecruiterReviewResult.InvalidStatus =>
                BadRequest(new
                {
                    message =
                        "Choose UnderReview, Shortlisted, or Rejected."
                }),

            RecruiterReviewResult.Locked =>
                Conflict(new
                {
                    message =
                        "This application can no longer be reviewed."
                }),

            RecruiterReviewResult.ShortlistLimitReached =>
                Conflict(new
                {
                    message =
                        "The shortlist cannot exceed the approved requisition headcount."
                }),

            RecruiterReviewResult.ShortlistAlreadySent =>
                Conflict(new
                {
                    message =
                        "This shortlist has already been sent to the hiring panelist and can no longer be changed."
                }),

            _ => Ok(applicant)
        };
    }

    [HttpPut("jobs/{jobId:guid}/shortlist/rank")]
    public async Task<IActionResult> Rank(
        Guid jobId,
        RankShortlistRequest request)
    {
        var recruiterId = CurrentUserId();

        if (recruiterId == null)
        {
            return Unauthorized();
        }

        var result = await _applicants.RankAsync(
            recruiterId.Value,
            jobId,
            request.OrderedApplicationIds);

        return result switch
        {
            RecruiterReviewResult.NotFound =>
                NotFound(new
                {
                    message = "Job or shortlist not found."
                }),

            RecruiterReviewResult.InvalidRanking =>
                BadRequest(new
                {
                    message =
                        "Include each shortlisted applicant exactly once."
                }),

            RecruiterReviewResult.ShortlistAlreadySent =>
                Conflict(new
                {
                    message =
                        "This shortlist has already been sent to the hiring panelist and cannot be re-ranked."
                }),

            _ => Ok(await _applicants.GetMineAsync(
                recruiterId.Value,
                jobId))
        };
    }

    [HttpGet("{id:guid}/cv")]
    public async Task<IActionResult> DownloadCv(Guid id)
    {
        var recruiterId = CurrentUserId();

        if (recruiterId == null)
        {
            return Unauthorized();
        }

        var application =
            await _applicants.GetOwnedApplicationAsync(
                recruiterId.Value,
                id);

        if (application == null)
        {
            return NotFound(new
            {
                message = "Application not found."
            });
        }

        var cv = await _cvs.GetFileForDownloadAsync(
            application.UserId);

        return cv == null
            ? NotFound(new
            {
                message = "No CV is available."
            })
            : File(
                cv.Value.Stream,
                cv.Value.ContentType,
                cv.Value.FileName);
    }

    private Guid? CurrentUserId() =>
        Guid.TryParse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),
            out var id)
            ? id
            : null;
}
