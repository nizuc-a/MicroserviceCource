using EventService.Api.Middleware;
using EventService.Api.Services;
using EventService.Application;
using EventService.Domain.Settings;
using EventService.Infrastructure;
using EventService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'Default' not found.");

var jwtSettings = builder.Configuration
                      .GetSection(JwtSettings.SectionName)
                      .Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Jwt settings not configured.");

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(connectionString);
builder.Services.AddJwtAuthentication(jwtSettings);

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.Configure<UserSettings>(
    builder.Configuration.GetSection(UserSettings.SectionName));

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Event Service API",
        Version = "v1"
    });

    const string schemeId = "Bearer";

    options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT-токен. Введите значение в формате: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(schemeId, document)] = []
    });
});

//Todo: Сделать слушателя
//builder.Services.AddHostedService<BookingBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();