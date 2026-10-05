using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSGM.Api.Common;
using RSGM.Api.Models.DTOs.RecruiterJobs;
using RSGM.Api.Services;

namespace RSGM.Api.Controllers;

[ApiController]
[Route("api/recruiter/postings")]
[Authorize(Roles = AppRoles.Recruiter)]
public class RecruiterJobPostingsController : ControllerBase
{
    private readonly RecruiterJobPostingService _service;

    public RecruiterJobPostingsController(
        RecruiterJobPostingService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _service.GetMineAsync(userId.Value);

        return result.Result switch
        {
            RecruiterJobResult.NoCompany => BadRequest(new
            {
                message =
                    "Ask the System Administrator to assign your account to a company."
            }),

            RecruiterJobResult.CompanyInactive => StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = "Your company is inactive."
                }),

            _ => Ok(result.Jobs)
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        return ToActionResult(
            await _service.GetMineByIdAsync(
                userId.Value,
                id));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateRecruiterJobRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _service.CreateAsync(
            userId.Value,
            request);

        if (result.Result != RecruiterJobResult.Success)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Job!.Id },
            result.Job);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateRecruiterJobRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        return ToActionResult(
            await _service.UpdateAsync(
                userId.Value,
                id,
                request));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        UpdateRecruiterJobStatusRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        return ToActionResult(
            await _service.UpdateStatusAsync(
                userId.Value,
                id,
                request.Status));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _service.DeleteAsync(
            userId.Value,
            id);

        return result switch
        {
            RecruiterJobResult.NotFound => NotFound(new
            {
                message = "Job posting not found."
            }),

            RecruiterJobResult.NoCompany => BadRequest(new
            {
                message =
                    "Ask the System Administrator to assign your account to a company."
            }),

            RecruiterJobResult.CompanyInactive => StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = "Your company is inactive."
                }),

            RecruiterJobResult.HasApplications => Conflict(new
            {
                message =
                    "A job posting with applications cannot be deleted. Close it instead."
            }),

            _ => NoContent()
        };
    }

    private IActionResult ToActionResult(
        (RecruiterJobResult Result,
            RecruiterJobPostingResponse? Job) result)
    {
        return result.Result switch
        {
            RecruiterJobResult.NotFound => NotFound(new
            {
                message = "Job posting not found."
            }),

            RecruiterJobResult.NoCompany => BadRequest(new
            {
                message =
                    "Ask the System Administrator to assign your account to a company."
            }),

            RecruiterJobResult.CompanyInactive => StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = "Your company is inactive."
                }),

            RecruiterJobResult.RequisitionRequired => BadRequest(new
            {
                message =
                    "Select an approved job requisition."
            }),

            RecruiterJobResult.RequisitionNotApproved => BadRequest(new
            {
                message =
                    "This job requires an approved requisition from your company."
            }),

            RecruiterJobResult.RequisitionAlreadyUsed => Conflict(new
            {
                message =
                    "A posting already exists for this requisition."
            }),

            RecruiterJobResult.RequisitionMismatch => BadRequest(new
            {
                message =
                    "Position, location, employment, experience and salary must match the approved requisition."
            }),

            RecruiterJobResult.InvalidSkills => BadRequest(new
            {
                message =
                    "Select at least one active required skill."
            }),

            RecruiterJobResult.InvalidSkillWeights => BadRequest(new
            {
                message =
                    "Each selected skill must have a weight greater than 0 and no greater than 100, and weights can only be supplied for selected skills."
            }),

            RecruiterJobResult.InvalidContent => BadRequest(new
            {
                message =
                    "Title, location, description, responsibilities and requirements must contain meaningful text."
            }),

            RecruiterJobResult.InvalidStatus => BadRequest(new
            {
                message =
                    "Status must be Draft, Published, or Closed, and the requested transition must be valid."
            }),

            RecruiterJobResult.InvalidEmploymentType => BadRequest(new
            {
                message =
                    "Employment type must be FullTime, PartTime, Contract, or Internship."
            }),

            RecruiterJobResult.InvalidWorkMode => BadRequest(new
            {
                message =
                    "Work mode must be OnSite, Remote, or Hybrid."
            }),

            RecruiterJobResult.InvalidExperienceLevel => BadRequest(new
            {
                message =
                    "Experience level must be Entry, Junior, Mid, or Senior."
            }),

            RecruiterJobResult.InvalidExperience => BadRequest(new
            {
                message =
                    "Minimum experience is required for non-internship roles above entry level."
            }),

            RecruiterJobResult.InvalidSalary => BadRequest(new
            {
                message =
                    "Check the salary range and provide a three-letter currency when salary is entered."
            }),

            RecruiterJobResult.InvalidDeadline => BadRequest(new
            {
                message =
                    "Application deadline must be today or a future date."
            }),

            RecruiterJobResult.ClosedJob => Conflict(new
            {
                message =
                    "A closed job posting cannot be reopened or edited."
            }),

            _ => Ok(result.Job)
        };
    }

    private Guid? GetCurrentUserId()
    {
        var claim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            claim,
            out var id)
            ? id
            : null;
    }
}