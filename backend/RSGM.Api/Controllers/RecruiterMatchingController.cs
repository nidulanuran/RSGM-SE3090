using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSGM.Api.Common;
using RSGM.Api.Services;

namespace RSGM.Api.Controllers;

public sealed class RunRecruiterMatchingRequest
{
    public Guid JobId { get; set; }
}

[ApiController]
[Route("api/recruiter/matching")]
[Authorize(Roles = AppRoles.Recruiter)]
public class RecruiterMatchingController : ControllerBase
{
    private readonly RecruiterApplicantService _applicants;

    public RecruiterMatchingController(
        RecruiterApplicantService applicants)
    {
        _applicants = applicants;
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run(
        RunRecruiterMatchingRequest request)
    {
        var recruiterId = CurrentUserId();

        if (recruiterId == null)
        {
            return Unauthorized();
        }

        if (request.JobId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid job posting is required."
            });
        }

        var result = await _applicants.GetMatchingAsync(
            recruiterId.Value,
            request.JobId);

        if (!result.Found)
        {
            return NotFound(new
            {
                message =
                    "The job posting was not found or does not belong to your company."
            });
        }

        return Ok(new
        {
            jobId = request.JobId,
            generatedAt = DateTime.UtcNow,
            candidateCount = result.Applicants.Count,
            candidates = result.Applicants
        });
    }

    private Guid? CurrentUserId()
    {
        var claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(claim, out var id)
            ? id
            : null;
    }
}
