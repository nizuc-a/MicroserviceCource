using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Settings;

namespace Shared.Api;

public static class SettingsExtensions
{
    public static IServiceCollection AddSharedAppSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<UserSettings>(configuration.GetSection(UserSettings.SectionName));
        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));

        return services;
    }
}
