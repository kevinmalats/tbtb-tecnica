using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Tbtb.Application.Abstractions;
using Tbtb.Application.UseCases;
using Tbtb.Infrastructure;
using Tbtb.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddDbContext<TbtbDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Tbtb")));
builder.Services.AddScoped<SqlTbtbUseCases>();
builder.Services.AddScoped<IActorRepository>(sp => sp.GetRequiredService<SqlTbtbUseCases>());
builder.Services.AddScoped<ICatalogRepository>(sp => sp.GetRequiredService<SqlTbtbUseCases>());
builder.Services.AddScoped<IPatientRepository>(sp => sp.GetRequiredService<SqlTbtbUseCases>());
builder.Services.AddScoped<IFollowUpRepository>(sp => sp.GetRequiredService<SqlTbtbUseCases>());
builder.Services.AddScoped<IContactRepository>(sp => sp.GetRequiredService<SqlTbtbUseCases>());
builder.Services.AddScoped<IMonthlyContactQuery>(sp => sp.GetRequiredService<SqlTbtbUseCases>());
builder.Services.AddScoped<IHealthProbe>(sp => sp.GetRequiredService<SqlTbtbUseCases>());
builder.Services.AddScoped<ITbtbUseCases, TbtbUseCases>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "TBTB Contactos API", Version = "v1" });
    options.AddSecurityDefinition("DemoActor", new() { Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Name = "X-Demo-Actor-Id" });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "DemoActor" } }] = [] });
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    var http = context.HttpContext; if (http.Response.HasStarted) return; var status = http.Response.StatusCode;
    var code = status == 415 ? "UNSUPPORTED_MEDIA_TYPE" : status == 400 ? "VALIDATION_ERROR" : "HTTP_ERROR";
    http.Response.ContentType = "application/problem+json";
    await http.Response.WriteAsJsonAsync(new { type = "about:blank", title = status == 415 ? "El tipo de contenido no es compatible." : "La solicitud no es valida.", status, code, traceId = http.TraceIdentifier });
});
app.UseSwagger(); app.UseSwaggerUI();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapGet("/health/ready", async (ITbtbUseCases cases, CancellationToken ct) => await cases.IsReady(ct) ? Results.Ok(new { status = "healthy" }) : Results.Json(new { status = "unhealthy", code = "DEPENDENCY_UNAVAILABLE" }, statusCode: 503)).AllowAnonymous();

var api = app.MapGroup("/api/v1").AddEndpointFilter(async (context, next) =>
{
    var http = context.HttpContext;
    if (!Guid.TryParse(http.Request.Headers["X-Demo-Actor-Id"], out var id)) return Problem(http, UseCaseResult.Fail(401, "DEMO_ACTOR_REQUIRED", "Se requiere un actor valido."));
    if (!http.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>("DemoAuthEnabled")) return Problem(http, UseCaseResult.Fail(401, "DEMO_AUTH_DISABLED", "El modo de actores de demostracion esta deshabilitado."));
    var actor = await http.RequestServices.GetRequiredService<ITbtbUseCases>().FindActor(id, http.RequestAborted);
    if (actor is null) return Problem(http, UseCaseResult.Fail(401, "DEMO_ACTOR_REQUIRED", "Se requiere un actor valido."));
    http.Items["actor"] = actor; return await next(context);
});

api.MapGet("/catalogs", async (ITbtbUseCases cases, HttpContext http, CancellationToken ct) => ToHttp(http, await cases.GetCatalogs(Actor(http), ct)));
api.MapPost("/patients", async (PatientCommand command, ITbtbUseCases cases, HttpContext http, CancellationToken ct) => ToHttp(http, await cases.CreatePatient(Actor(http), command, ct)));
api.MapGet("/patients", async (ITbtbUseCases cases, HttpContext http, int page = 1, int pageSize = 20, CancellationToken ct = default) => ToHttp(http, await cases.GetPatients(Actor(http), page, pageSize, ct)));
api.MapGet("/patients/{id:guid}", async (Guid id, ITbtbUseCases cases, HttpContext http, CancellationToken ct) => ToHttp(http, await cases.GetPatient(Actor(http), id, ct)));
api.MapPost("/follow-ups", async (FollowUpCommand command, ITbtbUseCases cases, HttpContext http, CancellationToken ct) => ToHttp(http, await cases.CreateFollowUp(Actor(http), command, ct)));
api.MapGet("/follow-ups", async (string patientId, ITbtbUseCases cases, HttpContext http, CancellationToken ct) => ToHttp(http, await cases.GetFollowUps(Actor(http), patientId, ct)));
api.MapPost("/contacts", async (ContactCommand command, ITbtbUseCases cases, HttpContext http, CancellationToken ct) => ToHttp(http, await cases.CreateContact(Actor(http), command, ct)));
api.MapGet("/contacts", async (ITbtbUseCases cases, HttpContext http, string? month = null, string? managerId = null, int? cityId = null, int page = 1, int pageSize = 20, CancellationToken ct = default) => ToHttp(http, await cases.GetContacts(Actor(http), month, managerId, cityId, page, pageSize, ct)));
api.MapGet("/contacts/{id:guid}", async (Guid id, ITbtbUseCases cases, HttpContext http, CancellationToken ct) => ToHttp(http, await cases.GetContact(Actor(http), id, ct)));
api.MapPost("/contacts/{id:guid}/corrections", async (Guid id, CorrectionCommand command, ITbtbUseCases cases, HttpContext http, CancellationToken ct) => ToHttp(http, await cases.CorrectContact(Actor(http), id, command, ct)));
api.MapGet("/contacts/{id:guid}/history", async (Guid id, ITbtbUseCases cases, HttpContext http, CancellationToken ct) => ToHttp(http, await cases.GetContactHistory(Actor(http), id, ct)));

app.Run();

static ActorContext Actor(HttpContext http) => (ActorContext)http.Items["actor"]!;
static IResult ToHttp(HttpContext http, UseCaseResult result) => result.Status switch { 200 => Results.Ok(result.Value), 201 => Results.Created(result.Location!, result.Value), _ => Problem(http, result) };
static IResult Problem(HttpContext http, UseCaseResult result) => Results.Json(new { type = "about:blank", title = result.Title, status = result.Status, code = result.Code, traceId = http.TraceIdentifier, errors = result.Errors }, statusCode: result.Status, contentType: "application/problem+json");

public partial class Program;
