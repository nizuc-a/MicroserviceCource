using UserService.Domain.Entities;

namespace UserService.UnitTests;

public class UserTests
{
    [Fact]
    public void AddBooking_AddsBookingId()
    {
        var user = new User("user1", "hash");
        var bookingId = Guid.NewGuid();

        user.AddBooking(bookingId);

        Assert.Single(user.BookingIds);
        Assert.Contains(bookingId, user.BookingIds);
    }

    [Fact]
    public void AddBooking_DuplicateBookingId_IsIdempotent()
    {
        var user = new User("user1", "hash");
        var bookingId = Guid.NewGuid();

        user.AddBooking(bookingId);
        user.AddBooking(bookingId);

        Assert.Single(user.BookingIds);
    }

    [Fact]
    public void RemoveBooking_RemovesBookingId()
    {
        var user = new User("user1", "hash");
        var bookingId = Guid.NewGuid();

        user.AddBooking(bookingId);
        user.RemoveBooking(bookingId);

        Assert.Empty(user.BookingIds);
    }
}
