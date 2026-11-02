using System.Security.Claims;
using System.Text;
using FCG.Application.Authentication;
using FCG.Application.Users;
using FCG.Domain.Users;
using FCG.Infrastructure.Authentication;
using FCG.Infrastructure.Persistence;
using FCG.Infrastructure.Messaging;
using FCG.Infrastructure.Security;
using FCG.Infrastructure.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Users") ?? "Data Source=users.db"));
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddSingleton<IUserCreatedEventPublisher, RabbitMqUserCreatedEventPublisher>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserAdministrationService, UserAdministrationService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience) ||
    string.IsNullOrWhiteSpace(jwt.SigningKey))
    throw new InvalidOperationException("JWT issuer, audience, and signing key must be configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier
    });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdministratorOnly", policy => policy.RequireRole(UserRole.Administrator.ToString())));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    var email = app.Configuration["BootstrapAdmin:Email"];
    var password = app.Configuration["BootstrapAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
    {
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        if (await repository.FindByNormalizedEmailAsync(RegistrationRules.NormalizeEmail(email), CancellationToken.None) is null)
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var admin = UserAccount.Register(
                "Administrator", email, hasher.HashPassword(password), UserRole.Administrator, DateTime.UtcNow);
            await repository.AddAsync(admin, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/auth/register", async (
    RegisterUserCommand command, IUserRegistrationService service, ILogger<Program> logger, CancellationToken ct) =>
{
    var result = await service.RegisterAsync(command, ct);
    if (result is RegistrationOutcome.Success { EventPublished: false } unpublishedSuccess)
        logger.LogWarning("User {UserId} was registered, but UserCreatedEvent was not published.", unpublishedSuccess.User.Id);
    return result switch
    {
        RegistrationOutcome.Success success => Results.Created($"/api/admin/users/{success.User.Id}", success.User),
        RegistrationOutcome.ValidationFailure failure => Results.ValidationProblem(
            failure.Errors.ToDictionary(pair => pair.Key, pair => pair.Value)),
        RegistrationOutcome.Conflict => Results.Conflict(new { message = "Já existe uma conta com este e-mail." }),
        _ => throw new InvalidOperationException("Unexpected registration outcome.")
    };
});

app.MapPost("/api/auth/login", async (
    LoginCommand command, IAuthenticationService service, CancellationToken ct) =>
{
    var result = await service.LoginAsync(command, ct);
    return result switch
    {
        LoginOutcome.Success success => Results.Ok(new { accessToken = success.AccessToken }),
        LoginOutcome.Failure => Results.Unauthorized(),
        _ => throw new InvalidOperationException("Unexpected login outcome.")
    };
});

app.MapGet("/api/admin/users", [Authorize(Policy = "AdministratorOnly")] async (
    IUserAdministrationService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)));

app.MapGet("/api/admin/users/{userId:guid}", [Authorize(Policy = "AdministratorOnly")] async (
    Guid userId, IUserAdministrationService service, CancellationToken ct) =>
{
    var user = await service.GetByIdAsync(userId, ct);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.MapPatch("/api/admin/users/{userId:guid}/role", [Authorize(Policy = "AdministratorOnly")] async (
    Guid userId, UpdateUserRoleCommand command, ClaimsPrincipal principal,
    IUserAdministrationService service, CancellationToken ct) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId))
        return Results.Unauthorized();
    if (!Enum.TryParse<UserRole>(command.Role, true, out var role) || !Enum.IsDefined(role))
        return Results.ValidationProblem(new Dictionary<string, string[]>
            { [nameof(command.Role)] = ["O papel deve ser User ou Administrator."] });
    var result = await service.ChangeRoleAsync(userId, adminId, role, ct);
    return result.Kind switch
    {
        UserAdministrationResultKind.Success => Results.Ok(result.User),
        UserAdministrationResultKind.NotFound => Results.NotFound(),
        UserAdministrationResultKind.Conflict => Results.Conflict(new { message = result.Detail }),
        _ => throw new InvalidOperationException("Unexpected user administration outcome.")
    };
});

app.MapDelete("/api/admin/users/{userId:guid}", [Authorize(Policy = "AdministratorOnly")] async (
    Guid userId, ClaimsPrincipal principal, IUserAdministrationService service, CancellationToken ct) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId))
        return Results.Unauthorized();
    var result = await service.DeleteAsync(userId, adminId, ct);
    return result.Kind switch
    {
        UserAdministrationResultKind.Success => Results.NoContent(),
        UserAdministrationResultKind.NotFound => Results.NotFound(),
        UserAdministrationResultKind.Conflict => Results.Conflict(new { message = result.Detail }),
        _ => throw new InvalidOperationException("Unexpected user administration outcome.")
    };
});

app.Run();

public partial class Program { }
