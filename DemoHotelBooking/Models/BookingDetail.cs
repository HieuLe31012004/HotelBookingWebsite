using System.ComponentModel.DataAnnotations;

namespace DemoHotelBooking.Models
{
    public class BookingDetail
    {
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public int RoomId { get; set; }
        public Room Room { get; set; }

        public double Price { get; set; }
    }
}
