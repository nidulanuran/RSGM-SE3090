using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RSGM.Api.Services.HrAgenticServices;

public class HrGeminiOrFallbackAiService : IHrAiCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HrGeminiOrFallbackAiService> _logger;

    public HrGeminiOrFallbackAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<HrGeminiOrFallbackAiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HrAiAnalysisOutput> AnalyzeRequisitionReadinessAsync(
        string positionTitle,
        string department,
        int headcount,
        string employmentType,
        string workMode,
        string location,
        string experienceLevel,
        int? minExperienceYears,
        decimal? minSalary,
        decimal? maxSalary,
        string? currency,
        string? description,
        string? responsibilities,
        string? requirements,
        string? justification,
        IReadOnlyList<string> availableMasterSkills,
        CancellationToken cancellationToken = default)
    {
        // 1. Sanitize all untrusted inputs to defend against prompt injection
        var cleanTitle = SanitizeUntrustedText(positionTitle);
        var cleanDept = SanitizeUntrustedText(department);
        var cleanDesc = SanitizeUntrustedText(description ?? string.Empty);
        var cleanResp = SanitizeUntrustedText(responsibilities ?? string.Empty);
        var cleanReq = SanitizeUntrustedText(requirements ?? string.Empty);
        var cleanJust = SanitizeUntrustedText(justification ?? string.Empty);

        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                _logger.LogInformation("Gemini API key detected. Calling Google Gemini ({Model}) for semantic requisition readiness analysis...", model);

                var geminiResult = await CallGeminiAsync(
                    apiKey,
                    model,
                    cleanTitle,
                    cleanDept,
                    headcount,
                    employmentType,
                    workMode,
                    location,
                    experienceLevel,
                    minExperienceYears,
                    minSalary,
                    maxSalary,
                    currency,
                    cleanDesc,
                    cleanResp,
                    cleanReq,
                    cleanJust,
                    availableMasterSkills,
                    cancellationToken);

                if (geminiResult != null)
                {
                    _logger.LogInformation("Google Gemini ({Model}) analysis succeeded with readiness score {Score}/100.", model, geminiResult.ReadinessScore);
                    return geminiResult;
                }

                _logger.LogWarning("Gemini returned null response. Falling back to deterministic HR analysis engine.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini API call failed. Falling back to deterministic HR analysis engine.");
            }
        }

        // 2. Deterministic Rule-Based Fallback Engine
        return RunDeterministicAnalysis(
            cleanTitle,
            cleanDept,
            headcount,
            employmentType,
            workMode,
            location,
            experienceLevel,
            minExperienceYears,
            minSalary,
            maxSalary,
            currency,
            cleanDesc,
            cleanResp,
            cleanReq,
            cleanJust,
            availableMasterSkills);
    }

    private static string SanitizeUntrustedText(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Strip common prompt injection tokens and escape control delimiters
        var sanitized = Regex.Replace(input, @"(?i)(ignore\s+previous\s+instructions|system\s+prompt|reveal\s+secret|developer\s+mode|bypass\s+approval)", "[FILTERED_ADVERSARIAL_TOKEN]");
        return sanitized.Trim();
    }

    private HrAiAnalysisOutput RunDeterministicAnalysis(
        string title,
        string dept,
        int headcount,
        string employmentType,
        string workMode,
        string location,
        string experienceLevel,
        int? minExperienceYears,
        decimal? minSalary,
        decimal? maxSalary,
        string? currency,
        string desc,
        string resp,
        string req,
        string just,
        IReadOnlyList<string> availableMasterSkills)
    {
        var strengths = new List<string>();
        var risks = new List<string>();
        var missing = new List<string>();
        var suggestions = new List<HrAiSkillSuggestion>();

        int score = 100;

        // Title & Department
        if (string.IsNullOrWhiteSpace(title))
        {
            missing.Add("Position title is missing.");
            score -= 30;
        }
        else
        {
            strengths.Add($"Position title clearly designated as '{title}'.");
        }

        if (string.IsNullOrWhiteSpace(dept))
        {
            missing.Add("Department is missing.");
            score -= 10;
        }

        // Headcount
        if (headcount < 1)
        {
            risks.Add("Requested headcount is less than 1.");
            score -= 20;
        }
        else
        {
            strengths.Add($"Headcount requested: {headcount}.");
        }

        // Description, Responsibilities, Requirements
        if (desc.Length < 30)
        {
            risks.Add("Job description is very brief or lacks comprehensive role context.");
            score -= 10;
        }
        else
        {
            strengths.Add("Job description provides foundational role context.");
        }

        if (resp.Length < 30)
        {
            missing.Add("Detailed key responsibilities are missing or insufficiently defined.");
            score -= 15;
        }
        else
        {
            strengths.Add("Defined core responsibilities for candidate evaluation.");
        }

        if (req.Length < 30)
        {
            missing.Add("Clear candidate requirements and qualifications are missing.");
            score -= 15;
        }
        else
        {
            strengths.Add("Clear baseline qualifications and competencies specified.");
        }

        // Salary benchmarking
        if (minSalary.HasValue && maxSalary.HasValue)
        {
            if (minSalary.Value > maxSalary.Value)
            {
                risks.Add($"Minimum salary ({minSalary.Value}) exceeds maximum salary ({maxSalary.Value}).");
                score -= 25;
            }
            else if (minSalary.Value <= 0)
            {
                risks.Add("Salary budget cannot be zero or negative.");
                score -= 15;
            }
            else
            {
                strengths.Add($"Realistic salary band specified ({minSalary:N0} - {maxSalary:N0} {currency ?? "LKR"}).");
            }
        }
        else
        {
            risks.Add("Explicit salary range is not specified. HR budget approval may require clarification.");
            score -= 10;
        }

        // Justification
        if (string.IsNullOrWhiteSpace(just))
        {
            missing.Add("Business justification for hiring headcount is missing.");
            score -= 10;
        }
        else
        {
            strengths.Add("Business hiring justification documented.");
        }

        // Match skills against available master skills based on text occurrences
        var combinedText = $"{title} {desc} {resp} {req}".ToLowerInvariant();

        foreach (var skill in availableMasterSkills)
        {
            var lowerSkill = skill.ToLowerInvariant();
            if (combinedText.Contains(lowerSkill))
            {
                suggestions.Add(new HrAiSkillSuggestion
                {
                    SkillName = skill,
                    Weight = combinedText.IndexOf(lowerSkill) < title.Length + 50 ? 1.5m : 1.0m,
                    Reason = $"Mentioned or strongly aligned with requisition duties."
                });
            }
        }

        // If no skills matched directly, suggest standard baseline competencies
        if (suggestions.Count == 0 && availableMasterSkills.Count > 0)
        {
            var fallbackCount = Math.Min(3, availableMasterSkills.Count);
            for (int i = 0; i < fallbackCount; i++)
            {
                suggestions.Add(new HrAiSkillSuggestion
                {
                    SkillName = availableMasterSkills[i],
                    Weight = 1.0m,
                    Reason = "Recommended standard competency for requisition profile."
                });
            }
        }

        score = Math.Clamp(score, 0, 100);

        string recommendation;
        if (score >= 80)
        {
            recommendation = "Requisition is well-defined and meets standard HR hiring compliance. Ready for HR Manager approval.";
        }
        else if (score >= 50)
        {
            recommendation = "Requisition has minor gaps. Recommended for HR review or slight revision before final publishing.";
        }
        else
        {
            recommendation = "Requisition has critical missing information or invalid parameters. Recommended for revision before HR approval.";
        }

        var executiveSummary = $"The Job Posting & Requisition Agent evaluated this requisition for '{title}' in '{dept}'. " +
            $"Readiness score is {score}/100. {strengths.Count} strengths identified, {risks.Count} potential risks or ambiguities noted, " +
            $"and {suggestions.Count} competencies recommended for job posting skill matching.";

        return new HrAiAnalysisOutput
        {
            ReadinessScore = score,
            Strengths = strengths,
            IdentifiedRisks = risks,
            MissingInformation = missing,
            SuggestedSkills = suggestions,
            ExecutiveSummary = executiveSummary,
            RecommendationForApprover = recommendation
        };
    }

    // 3. Google Gemini API Integration
    private async Task<HrAiAnalysisOutput?> CallGeminiAsync(
        string apiKey,
        string model,
        string title,
        string dept,
        int headcount,
        string employmentType,
        string workMode,
        string location,
        string experienceLevel,
        int? minExperienceYears,
        decimal? minSalary,
        decimal? maxSalary,
        string? currency,
        string desc,
        string resp,
        string req,
        string just,
        IReadOnlyList<string> availableMasterSkills,
        CancellationToken cancellationToken)
    {
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var prompt = $$"""
You are the Job Posting & Requisition Agent for an enterprise recruitment platform.
Your task is to analyze a job requisition draft and evaluate its readiness for HR Manager approval.
Enforce standard HR compliance, budget feasibility, and skill alignment.

Requisition Data:
- Position Title: {{title}}
- Department: {{dept}}
- Headcount: {{headcount}}
- Employment Type: {{employmentType}}
- Work Mode: {{workMode}}
- Location: {{location}}
- Experience Level: {{experienceLevel}} (Min Years: {{minExperienceYears}})
- Salary Band: {{minSalary}} - {{maxSalary}} {{currency}}
- Description: {{desc}}
- Responsibilities: {{resp}}
- Requirements: {{req}}
- Justification: {{just}}

Available System Skills: {{string.Join(", ", availableMasterSkills)}}

Respond ONLY with valid JSON matching this schema:
{
  "readinessScore": <number between 0 and 100>,
  "strengths": ["<strength 1>", "<strength 2>"],
  "identifiedRisks": ["<risk 1>"],
  "missingInformation": ["<missing field 1>"],
  "suggestedSkills": [
    {"skillName": "<name from available skills>", "weight": 1.0, "reason": "<rationale>"}
  ],
  "executiveSummary": "<concise summary>",
  "recommendationForApprover": "<guidance for HR Manager>"
}
""";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.2
            }
        };

        var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini API returned status code {StatusCode}", response.StatusCode);
            return null;
        }

        var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        if (jsonDoc == null) return null;

        var text = jsonDoc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text)) return null;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<HrAiAnalysisOutput>(text, options);
    }
}
