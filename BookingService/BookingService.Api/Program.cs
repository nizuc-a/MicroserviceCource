using BookingService.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBookingServiceApi(builder.Configuration);

var app = builder.Build();

app.UseBookingServiceApi();

app.Run();
