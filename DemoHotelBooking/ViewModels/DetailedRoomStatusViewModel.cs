using DemoHotelBooking.Models;

namespace DemoHotelBooking.ViewModels
{
    public class DetailedRoomStatusViewModel
    {
        public Room Room { get; set; } = new Room();
        public bool IsAvailable { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public string AvailabilityStatus { get; set; } = "Có sẵn";
        public string StatusClass { get; set; } = "bg-success";
        
        // Thông tin booking chi tiết
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime? BookingDate { get; set; }
        public DateTime? BookedCheckInDate { get; set; }
        public DateTime? BookedCheckOutDate { get; set; }
        public string? BookingStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public double? TotalAmount { get; set; }
        public double? DepositAmount { get; set; }
        public bool IsDeposited { get; set; }
        public int? BookingId { get; set; }
        
        // Thông tin chi tiết trạng thái
        public string DetailedStatus
        {
            get
            {
                if (Room.Status == "Available") return "Phòng trống";
                
                return BookingStatus switch
                {
                    "Deposited" => "Đã đặt cọc",
                    "CancelRequested" => "Yêu cầu hủy (chờ hoàn tiền)",
                    "CheckedIn" => "Đã nhận phòng",
                    "CheckedOut" => "Đã trả phòng",
                    "Cancelled" => "Đã hủy",
                    _ => "Hết phòng"
                };
            }
        }
        
        public string DetailedStatusClass
        {
            get
            {
                if (Room.Status == "Available") return "bg-success";
                
                return BookingStatus switch
                {
                    "Deposited" => "bg-warning",
                    "CancelRequested" => "bg-info",
                    "CheckedIn" => "bg-primary",
                    "Cleaning" => "bg-primary",
                    "CheckedOut" => "bg-secondary",
                    "Cancelled" => "bg-danger",
                    _ => "bg-warning"
                };
            }
        }
    }
}
