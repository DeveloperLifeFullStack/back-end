using DevLife.Backend.Common;
using DevLife.Backend.Common.Extensions;
using DevLife.Backend.Features.CodeCasino;
using DevLife.Backend.Features.Dating;
using DevLife.Backend.Modules.Auth;
using DevLife.Backend.Modules.BugChase;
using DevLife.Backend.Modules.Casino;
using DevLife.Backend.Modules.Dating;
using DevLife.Backend.Modules.Personality;
using DevLife.Backend.Modules.Roaster;
using DevLife.Backend.Modules.RunFromMeetings;
using DevLife.Backend.Persistence;
using DevLife.Backend.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<MongoDbContext>();

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "DevLife API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Authentication & Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddSingleton<JwtService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Application Services
builder.Services.AddSingleton<AiSnippetService>();
builder.Services.AddHttpClient<AiRoaster>();
builder.Services.AddHttpClient<GitHubAnalyzerService>();

// Validation
builder.Services.AddScoped<IValidator<RegisterUserRequest>, RegisterUserValidator>();

var app = builder.Build();

// Development Configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware Pipeline
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// API Endpoints
app.MapGroup("/auth")
   .WithTags("Auth")
   .MapLoginUser()
   .MapAuthRoutes();

app.MapGroup("/casino")
   .RequireAuthorization()
   .WithTags("Code Casino")
   .MapSnippetEndpoints()
   .MapLeaderboard();

app.MapGroup("/roaster")
   .WithTags("Code Roaster")
   .MapRoasterEndpoints();

app.MapBugChaseEndpoints();
app.MapAnalyzeRepoEndpoint();

// Dating Feature Endpoints
CreateDatingProfileEndpoint.Map(app);
GetMatchesEndpoint.Map(app);
SwipeProfileEndpoint.Map(app);

app.MapGroup("/dating/ai")
   .RequireAuthorization()
   .WithTags("DatingApp")
   .MapAiChatEndpoint();


app.MapRunFromMeetingsEndpoints();


app.Run();