using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Comun;
using Servidor.Infraestructura;
using Servidor.Infraestructura.Persistence;
using Servidor.ApiWeb.Autenticacion;
using Servidor.ApiWeb.Contexto;
using Servidor.ApiWeb.Middleware;

var builder = WebApplication.CreateBuilder(args);
var isDesktopMode = string.Equals(
    builder.Configuration["POS_APP_MODE"] ?? Environment.GetEnvironmentVariable("POS_APP_MODE"),
    "desktop",
    StringComparison.OrdinalIgnoreCase);

ConfigureDesktopMode(builder, isDesktopMode);

// Soporte de DATABASE_URL estilo Railway/Heroku
var configuredConn = Servidor.Infraestructura.DependencyInjection.ResolveConnectionString(builder.Configuration);

var shouldResolveFromDatabaseUrl = string.IsNullOrWhiteSpace(configuredConn)
    || string.Equals(configuredConn, "DATABASE_URL", StringComparison.OrdinalIgnoreCase)
    || string.Equals(configuredConn, "DATABASE_PUBLIC_URL", StringComparison.OrdinalIgnoreCase)
    || configuredConn.StartsWith("postgres", StringComparison.OrdinalIgnoreCase);

if (shouldResolveFromDatabaseUrl && !string.IsNullOrWhiteSpace(configuredConn))
{
    if (configuredConn.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
    {
        var mapped = MapDatabaseUrl(configuredConn);
        if (!string.IsNullOrWhiteSpace(mapped))
        {
            builder.Configuration["ConnectionStrings:Default"] = mapped;
        }
    }
}

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Default")))
{
    builder.Configuration["ConnectionStrings:Default"] = configuredConn;
}

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>();
if (corsOrigins == null || corsOrigins.Length == 0)
{
    var raw = builder.Configuration["Cors:Origins"] ?? builder.Configuration["Cors__Origins"];
    if (!string.IsNullOrWhiteSpace(raw))
    {
        corsOrigins = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

corsOrigins ??= new[] { "http://localhost:5173", "http://127.0.0.1:5173" };

var allowAnyOrigin = corsOrigins.Contains("*");
var explicitOrigins = new HashSet<string>(corsOrigins.Where(o => o != "*"), StringComparer.OrdinalIgnoreCase);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        if (allowAnyOrigin)
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(explicitOrigins.ToArray());
        }

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPosAuthentication(builder.Configuration);
builder.Services.AddScoped<IRequestContext, RequestContext>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PosDbContext>();
    await DatabaseBootstrapper.InitializeAsync(dbContext);
}

app.UseMiddleware<ProblemDetailsExceptionMiddleware>();

app.UseRouting();
app.UseCors("AppCors");

app.UseAuthentication();
app.UseMiddleware<RequestContextMiddleware>();
app.UseAuthorization();

if (HasStaticFrontend(app.Environment))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapControllers();

if (HasStaticFrontend(app.Environment))
{
    app.MapFallbackToFile("index.html");
}

app.Run();

static void ConfigureDesktopMode(WebApplicationBuilder builder, bool isDesktopMode)
{
    if (!isDesktopMode)
    {
        return;
    }

    var dataDirectory = builder.Configuration["POS_DATA_DIR"]
        ?? Environment.GetEnvironmentVariable("POS_DATA_DIR")
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ERP_STOCK_V2",
            "data");

    Directory.CreateDirectory(dataDirectory);
    var databasePath = Path.Combine(dataDirectory, "erp-stock.db");
    builder.Configuration["ConnectionStrings:Default"] = $"Data Source={databasePath}";
}

static bool HasStaticFrontend(IWebHostEnvironment environment)
{
    return File.Exists(Path.Combine(environment.WebRootPath ?? string.Empty, "index.html"));
}

static string? MapDatabaseUrl(string? databaseUrl)
{
    if (string.IsNullOrWhiteSpace(databaseUrl))
    {
        return null;
    }

    if (!databaseUrl.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
    {
        return null;
    }

    var userInfo = uri.UserInfo.Split(':', 2);
    var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var database = uri.AbsolutePath.TrimStart('/');

    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port.ToString() : "5432";

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Require;Trust Server Certificate=true";
}

public partial class Program
{
}


