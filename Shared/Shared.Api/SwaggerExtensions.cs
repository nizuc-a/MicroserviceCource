using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Shared.Api;

public static class SwaggerExtensions
{
    public static IServiceCollection AddJwtBearerSwaggerGen(
        this IServiceCollection services,
        string apiTitle,
        string version = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = apiTitle,
                Version = version
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

        return services;
    }
}
