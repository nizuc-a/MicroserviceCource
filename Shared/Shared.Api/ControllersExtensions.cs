using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Api;

public static class ControllersExtensions
{
    public static IServiceCollection AddApiControllers(
        this IServiceCollection services,
        bool withJsonEnumConverter = true)
    {
        var builder = services.AddControllers();
        if (withJsonEnumConverter)
        {
            builder.AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        }

        return services;
    }
}
