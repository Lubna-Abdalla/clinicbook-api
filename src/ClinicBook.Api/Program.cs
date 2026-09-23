using ClinicBook.Api.Extensions;
using ClinicBook.Api.Middleware;
using ClinicBook.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Database, Identity and the application services (Infrastructure/DependencyInjection.cs).
builder.Services.AddInfrastructure(builder.Configuration);

// Who is calling (JWT) and what they are allowed to do (roles).
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

// Outermost middleware, so it can catch exceptions from everything that follows.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    await app.MigrateAndSeedDatabaseAsync();

    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "ClinicBook API v1"));
}

app.UseHttpsRedirection();

// Order matters: first work out who the caller is, then whether they may do this.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
