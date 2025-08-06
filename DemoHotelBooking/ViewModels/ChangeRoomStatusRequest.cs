namespace DemoHotelBooking.ViewModels
{
    public class ChangeRoomStatusRequest
    {
        public int RoomId { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}
