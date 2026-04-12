using MicroserviceCourse.Model.Entity;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceCourse.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events { get; set; }
    public DbSet<Booking> Bookings { get; set; }
}