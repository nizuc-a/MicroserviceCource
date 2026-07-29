using Serilog;
using Serilog.Formatting.Compact;
using UserService.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddUserServiceApi(builder.Configuration);

var app = builder.Build();

app.UseUserServiceApi();

app.Run();
