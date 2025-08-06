namespace DemoHotelBooking.ViewModels
{
    public class ReportDetail
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public DateTime BookingDate { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string RoomNames { get; set; } = "";
        public double TotalAmount { get; set; }
        public string Status { get; set; } = "";
    }
}
