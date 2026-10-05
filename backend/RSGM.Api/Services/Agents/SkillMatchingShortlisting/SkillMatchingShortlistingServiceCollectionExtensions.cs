using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RSGM.Api.Agents.SkillMatchingShortlisting;
using RSGM.Api.Models;
using RSGM.Api.Tools.SkillMatchingShortlisting;

namespace RSGM.Api.Services.Agents.SkillMatchingShortlisting;

public static class SkillMatchingShortlistingServiceCollectionExtensions
{
    public static IServiceCollection AddSkillMatchingShortlistingAgent(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.Configure<GroqOptions>(
            configuration.GetSection("Groq"));

        services.AddDbContext<SkillMatchingShortlistingDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__SkillMatchingAgentMigrationsHistory")));

        services.AddHttpClient<SkillMatchingGroqLlmService>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<GroqOptions>>()
                .Value;

            client.Timeout = TimeSpan.FromSeconds(
                Math.Max(5, options.TimeoutSeconds));
        });

        services.AddSingleton<SkillMatchingAgentToolRegistry>();

        services.AddScoped<GetSkillMatchingJobRequirementsTool>();
        services.AddScoped<GetEligibleSkillMatchingCandidatesTool>();
        services.AddScoped<CalculateSkillMatchTool>();
        services.AddScoped<GenerateSkillGapTool>();
        services.AddScoped<ValidateSkillMatchingShortlistTool>();

        services.AddScoped<SkillMatchingJobRequirementsAgent>();
        services.AddScoped<SkillMatchingCandidateRetrievalAgent>();
        services.AddScoped<SkillMatchingAnalysisAgent>();
        services.AddScoped<SkillMatchingShortlistValidationAgent>();
        services.AddScoped<SkillMatchingShortlistingOrchestrator>();
        services.AddScoped<SkillMatchingShortlistDispatchService>();

        return services;
    }
}