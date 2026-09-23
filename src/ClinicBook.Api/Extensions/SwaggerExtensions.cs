using Microsoft.OpenApi;

namespace ClinicBook.Api.Extensions;

public static class SwaggerExtensions
{
    private const string SchemeName = "Bearer";

    /// <summary>
    /// Swagger UI plus an "Authorize" button, so the API can be tried out in the browser:
    /// log in, paste the returned token once, and every protected endpoint becomes testable.
    /// </summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ClinicBook API",
                Version = "v1",
                Description = "Appointment booking API for a small clinic: specialties, doctors, weekly schedules and appointments."
            });

            options.AddSecurityDefinition(SchemeName, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste only the token returned by /api/auth/login."
            });

            // Applies the bearer scheme to every operation, so the "Authorize" button
            // sends the token with each request you try out.
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference(SchemeName, document), new List<string>() }
            });
        });

        return services;
    }
}
