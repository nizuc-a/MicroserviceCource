using MicroserviceCourse.Data;
using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Interfaces.TaskQueue;
using MicroserviceCourse.Middleware;
using MicroserviceCourse.Services;
using MicroserviceCourse.Services.BackgroundServices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("EventDb"));

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddSingleton<IBookingTaskQueue, InMemoryBookingTaskQueue>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<BookingBackgroundService>(); 

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();