using DemoHotelBooking.Models;

namespace DemoHotelBooking.ViewModels
{
    public class BookingView
    {
        public Booking Booking { get; set; }
        public List<BookingDetail>? Rooms { get; set; }
        // Computed properties for details page
        public int TotalNights => (Booking.CheckoutDate.Date - Booking.CheckinDate.Date).Days > 0 ? (Booking.CheckoutDate.Date - Booking.CheckinDate.Date).Days : 1;
        public int TotalRooms => Booking.BookingDetails?.Count ?? 0;
        public double BasePrice => Booking.BookingDetails?.Sum(d => d.Room.Price * TotalNights) ?? 0;
        public double DiscountAmount => Math.Round(BasePrice * 0.02, 0);
        public double VatAmount => Math.Round((BasePrice - DiscountAmount) * 0.08, 0);
        public double FinalAmount => BasePrice - DiscountAmount + VatAmount;
        public double DepositAmount => Math.Round(FinalAmount * 0.2, 0);

        // Helper for room images
        public List<string> GetRoomImageUrls(Room room)
        {
            if (room.RoomImages != null && room.RoomImages.Any())
                return room.RoomImages.Select(img => img.Path).ToList();
            return new List<string>();
        }
        public string Status
        {
            get
            {
                switch (Booking.Status)
                {
                    case 0: return "Đã đặt cọc - Chờ nhận phòng";
                    case 1: return "Đã hủy - Chờ hoàn cọc";
                    case 2: return "Đã Check-in";
                    case 3: return "Đã trả phòng";
                    case 4: return "Đã hủy - Đã hoàn cọc";
                    case 5: return "Đã trả phòng - Hoàn thành";
                    default: return "Chưa xác định";
                }
            }
        }
        public string StatusColor
        {
            get
            {
                switch (Booking.Status)
                {
                    case 0: return "text-primary";
                    case 1: return "text-warning";
                    case 2: return "text-primary";
                    case 3: return "text-success";
                    case 4: return "text-danger";
                    case 5: return "text-success";
                    default: return "text-muted";
                }
            }
        }

    }
}
