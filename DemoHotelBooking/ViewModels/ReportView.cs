using DemoHotelBooking.Models;

namespace DemoHotelBooking.ViewModels
{
    public class ReportView
    {
        public ReportRevenue ReportRevenue { get; set; }
        public List<ReportDetailView> ReportDetails { get; set; }
    }
}
