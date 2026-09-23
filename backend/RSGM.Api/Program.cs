using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;
using RSGM.Api.Services;
using RSGM.Api.Services.HrAgenticServices;

var builder = WebApplication.CreateBuilder(args);




// ======================================================
// 1. CONTROLLERS
// ======================================================

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ReactFrontend",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


// ======================================================
// 2. SWAGGER / OPENAPI
// ======================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RSGM.Api",
        Version = "v1",
        Description = "Recruitment & Skill-Gap Matching Platform API"
    });

    // JWT Authorize button
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


// ======================================================
// 3. DATABASE CONNECTION
// ======================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection is not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


// ======================================================
// 4. ASP.NET CORE IDENTITY
// ======================================================

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // Password rules
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


// ======================================================
// 5. JWT AUTHENTICATION
// ======================================================

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key is not configured.");

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Jwt:Issuer is not configured.");

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Jwt:Audience is not configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Check who created the token
                ValidateIssuer = true,

                // Check who the token is intended for
                ValidateAudience = true,

                // Check token expiration
                ValidateLifetime = true,

                // Verify token signature
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,

                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                // Expired means expired immediately
                ClockSkew = TimeSpan.Zero
            };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var idClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(idClaim, out var userId))
                {
                    context.Fail("Invalid user identifier.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userId.ToString());

                if (user == null || !user.IsActive)
                {
                    context.Fail("This account is inactive or no longer exists.");
                    return;
                }

                var currentRoles = (await userManager.GetRolesAsync(user))
                    .OrderBy(role => role)
                    .ToArray();
                var tokenRoles = context.Principal!
                    .FindAll(ClaimTypes.Role)
                    .Select(claim => claim.Value)
                    .OrderBy(role => role)
                    .ToArray();

                if (!currentRoles.SequenceEqual(tokenRoles))
                {
                    context.Fail("The account roles have changed. Please sign in again.");
                }
            }
        };
    });


// ======================================================
// 6. AUTHORIZATION
// ======================================================

builder.Services.AddAuthorization();


// ======================================================
// 7. APPLICATION SERVICES
// ======================================================

// Handles JWT generation
builder.Services.AddScoped<TokenService>();

// Handles Skill business logic
builder.Services.AddScoped<SkillService>();

builder.Services.AddScoped<JobSeekerProfileService>();

builder.Services.AddScoped<JobSeekerEducationService>();

builder.Services.AddScoped<JobSeekerWorkExperienceService>();

builder.Services.AddScoped<JobSeekerSkillService>();

builder.Services.AddScoped<JobSeekerCvService>();

builder.Services.AddScoped<JobPostingService>();

builder.Services.AddScoped<JobSeekerApplicationService>();

builder.Services.AddScoped<JobSeekerDashboardService>();

builder.Services.AddScoped<JobSeekerAccountService>();

builder.Services.AddScoped<AdminUserService>();

builder.Services.AddScoped<AdminDashboardService>();

builder.Services.AddScoped<AdminCompanyService>();

builder.Services.AddScoped<RecruiterJobPostingService>();

builder.Services.AddScoped<JobRequisitionService>();

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// ======================================================
// 7.1 AGENTIC AI (HR FUNCTIONS - COMPONENT A)
// ======================================================
builder.Services.AddHttpClient<IHrAiCompletionService, HrGeminiOrFallbackAiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IHrAgentTool, HrValidateRequisitionReadinessTool>();
builder.Services.AddScoped<IHrAgentTool, HrRecommendRequisitionSkillsTool>();
builder.Services.AddScoped<IHrAgentTool, HrAuditSalaryBenchmarkTool>();
builder.Services.AddScoped<IHrAgentTool, HrGenerateApprovalSummaryTool>();
builder.Services.AddScoped<IHrAgentTool, HrCreateApprovalRequestTool>();

builder.Services.AddScoped<HrJobRequisitionAgent>();
builder.Services.AddScoped<HrWorkflowCoordinator>();



// ======================================================
// 8. HEALTH CHECKS
// ======================================================

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();


// ======================================================
// BUILD APPLICATION
// ======================================================

var app = builder.Build();


// ======================================================
// 9. SWAGGER
// ======================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "RSGM.Api v1"
        );
    });
}


// ======================================================
// 10. HTTPS
// ======================================================

app.UseHttpsRedirection();


// ======================================================
// 11. AUTHENTICATION + AUTHORIZATION
// ======================================================

// IMPORTANT:
// Authentication must come BEFORE Authorization.

app.UseCors("ReactFrontend");

app.UseAuthentication();

app.UseAuthorization();


// ======================================================
// 12. MAP CONTROLLERS
// ======================================================

app.MapControllers();


// ======================================================
// 13. HEALTH ENDPOINT
// ======================================================

app.MapHealthChecks("/health");


// ======================================================
// 14. SEED IDENTITY ROLES
// ======================================================

// Creates these roles if they do not already exist:
//
// JobSeeker
// Recruiter
// HRManager
// HiringPanelist
// SystemAdmin

await IdentitySeeder.SeedAsync(
    app.Services,
    builder.Configuration);

await CompanySeeder.SeedAsync(
    app.Services);

await CompanyBackfillSeeder.SeedAsync(
    app.Services);

// Job postings now require HR approval; do not seed published sample jobs.



// ======================================================
// 15. RUN APPLICATION
// ======================================================

app.Run();
