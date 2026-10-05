using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RSGM.Api.Agents.InterviewSchedulingCoordinationAgent;
using RSGM.Api.Data;
using RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

namespace RSGM.Api.Services.Agents.InterviewSchedulingCoordinationAgent;

public static class InterviewSchedulingCoordinationServiceCollectionExtensions
{
    public static IServiceCollection AddInterviewSchedulingCoordinationAgent(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        // -----------------------------------------------------
        // GROQ CONFIGURATION
        // -----------------------------------------------------

        services.Configure<GroqOptions>(
            configuration.GetSection("Groq"));

        services.AddHttpClient<InterviewSchedulingGroqLlmService>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<GroqOptions>>()
                    .Value;

                client.Timeout = TimeSpan.FromSeconds(
                    Math.Max(
                        5,
                        options.TimeoutSeconds));
            });

        // -----------------------------------------------------
        // AGENT WORKFLOW DATABASE
        // -----------------------------------------------------

        services.AddDbContext<
            InterviewSchedulingCoordinationDbContext>(
            options =>
                options.UseNpgsql(
                    connectionString,
                    npgsql =>
                        npgsql.MigrationsHistoryTable(
                            "__InterviewSchedulingAgentMigrationsHistory")));

        // -----------------------------------------------------
        // TOOL REGISTRY
        // -----------------------------------------------------

        services.AddSingleton<
            InterviewSchedulingAgentToolRegistry>();

        // -----------------------------------------------------
        // TOOLS
        // -----------------------------------------------------

        services.AddScoped<
            GetInterviewSchedulingContextTool>();

        services.AddScoped<
            GetInterviewAvailableSlotsTool>();

        services.AddScoped<
            SelectInterviewSlotTool>();

        services.AddScoped<
            ValidateInterviewScheduleTool>();

        // -----------------------------------------------------
        // AGENTS
        // -----------------------------------------------------

        services.AddScoped<
            InterviewSchedulingContextAgent>();

        services.AddScoped<
            InterviewAvailabilityAgent>();

        services.AddScoped<
            InterviewCoordinationAgent>();

        services.AddScoped<
            InterviewScheduleValidationAgent>();

        // -----------------------------------------------------
        // ORCHESTRATOR
        // -----------------------------------------------------

        services.AddScoped<
            InterviewSchedulingCoordinationOrchestrator>();

        return services;
    }
}