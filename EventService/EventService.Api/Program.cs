using EventService.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEventServiceApi(builder.Configuration);

var app = builder.Build();

app.UseEventServiceApi();

app.Run();
