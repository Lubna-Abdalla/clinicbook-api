using ClinicBook.Core.Interfaces;
using ClinicBook.Infrastructure.Data;
using ClinicBook.Infrastructure.Identity;
using ClinicBook.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicBook.Infrastructure;

/// <summary>
/// One place that registers everything the Infrastructure layer provides.
/// The API only has to call services.AddInfrastructure(configuration), which keeps Program.cs short.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found. " +
                "Set it with: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\"");

        services.AddDbContext<ClinicBookDbContext>(options =>
            options.UseSqlServer(connectionString));

        // AddIdentityCore gives us UserManager/RoleManager (create users, hash and check passwords)
        // without the cookie-based login UI, because this is a token-based Web API.
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Identity's default password rules (upper, lower, digit and a symbol),
                // with a slightly longer minimum length than the default of 6.
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ClinicBookDbContext>();

        // Binds the "Jwt" configuration section (kept in User Secrets) to JwtSettings.
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<UserAccountCreator>();

        // Scoped: one instance per HTTP request, the same lifetime as the DbContext they use.
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISpecialtyService, SpecialtyService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IDoctorScheduleService, DoctorScheduleService>();
        services.AddScoped<IAppointmentService, AppointmentService>();

        return services;
    }
}
