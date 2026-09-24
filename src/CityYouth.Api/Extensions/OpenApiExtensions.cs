using Microsoft.OpenApi;

namespace CityYouth.Api.Extensions;

public static class OpenApiExtensions
{
    public static void AddBearerSecurityScheme(this Microsoft.AspNetCore.OpenApi.OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Supabase-issued access token. Paste the raw JWT (no 'Bearer ' prefix).",
            };
            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            });
            return Task.CompletedTask;
        });
    }
}
