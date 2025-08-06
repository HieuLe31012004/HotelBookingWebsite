using DemoHotelBooking.Models;

namespace DemoHotelBooking.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int TodayBookings { get; set; }
        public int PendingPayments { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<BookingView> RecentBookings { get; set; } = new List<BookingView>();
        public List<BookingView> TodayCheckins { get; set; } = new List<BookingView>();
        
        // Thêm các thống kê mới
        public int ConfirmedBookings { get; set; }
        public int CheckedInBookings { get; set; }
        public int CompletedBookings { get; set; }
    }
}
