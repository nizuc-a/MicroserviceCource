using UserService.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddUserServiceApi(builder.Configuration);

var app = builder.Build();

app.UseUserServiceApi();

app.Run();
