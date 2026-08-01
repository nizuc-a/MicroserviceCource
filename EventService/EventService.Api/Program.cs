using EventService.Api.Extensions;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddEventServiceApi(builder.Configuration);

var app = builder.Build();

app.UseEventServiceApi();

app.Run();
