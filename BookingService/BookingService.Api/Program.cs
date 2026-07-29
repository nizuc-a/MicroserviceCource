using BookingService.Api.Extensions;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddBookingServiceApi(builder.Configuration);

var app = builder.Build();

app.UseBookingServiceApi();

app.Run();
