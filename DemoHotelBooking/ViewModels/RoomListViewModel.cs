using DemoHotelBooking.Models;

namespace DemoHotelBooking.ViewModels
{
    public class RoomListViewModel
    {
        public Room Room { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public string AvailabilityStatus { get; set; } = "Có sẵn";
        public string StatusClass { get; set; } = "bg-success";
    }
}
