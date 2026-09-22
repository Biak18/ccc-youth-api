using CityYouth.Api.Authorization;
using CityYouth.Api.Middleware;
using CityYouth.Api.Services;
using CityYouth.Application;
using CityYouth.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Supabase (access-token style: anon key for reads, user JWTs for identity)
// ---------------------------------------------------------------------------

var supabaseUrl = builder.Configuration["Supabase:Url"]?.TrimEnd('/');
if (string.IsNullOrWhiteSpace(supabaseUrl))
    throw new InvalidOperationException(
        "Supabase:Url is required (user-secrets, env var, or appsettings).");

var supabaseIssuer = $"{supabaseUrl}/auth/v1";

// Frontend Supabase login tokens validate directly — no separate user store.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = supabaseIssuer;
        options.Audience = "authenticated";
        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = supabaseIssuer,
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });
builder.Services.AddAuthorization(options =>
{
    // Leaders/announcements writes check ownership in handlers; this policy
    // gates admin-only areas (leaders mgmt, settings, users) on the
    // profiles-table role — never on client-supplied claims (§49).
    options.AddPolicy("Admin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new AdminRequirement());
    });
});
builder.Services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();

// Layers (§27): composition root stays small.
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CityYouth.Application.Abstractions.ICurrentUser, CurrentUser>();
builder.Services.AddControllers();

builder.Services.AddCors(o => o.AddPolicy("web", p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Bearer scheme so Scalar gets an Authorize button (paste the raw Supabase
// access token from the site's login — no 'Bearer ' prefix).
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Supabase-issued access token. Paste the raw JWT (no 'Bearer ' prefix) here.",
    };
    document.Security ??= [];
    document.Security.Add(new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
    });
    return Task.CompletedTask;
}));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseExceptionHandler();

app.Run();

public partial class Program;
