using DemoHotelBooking.Models;
using DemoHotelBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoHotelBooking.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

           

            var thisMonth = new DateTime(today.Year, today.Month, 1);
            
            // Thống kê tổng quan
            var totalRooms = _context.Rooms.Count();
            var availableRooms = GetAvailableRoomsCount(today, today.AddDays(1));
            var todayBookings = _context.Bookings.Count(b =>
               b.CreateDate >= today && b.CreateDate < tomorrow);

            // Tính doanh thu từ các invoice đã thanh toán trong tháng
            var monthlyRevenue = _context.Invoices
                .Where(i => i.PaymentDate >= thisMonth && i.PaymentDate < thisMonth.AddMonths(1) && i.Status == 3)
                .Sum(i => (decimal)i.Amount);
            
            // Nếu không có invoice, tính từ booking đã hoàn thành
            if (monthlyRevenue == 0)
            {
                monthlyRevenue = _context.Bookings
                    .Where(b => b.CreateDate >= thisMonth && (b.Status == 5 || b.Status == 6))
                    .Sum(b => (decimal)(b.TotalPrice ?? 0));
            }
            
            // Đếm các trạng thái booking khác nhau
            var pendingBookings = _context.Bookings.Count(b => b.Status == 0); // Chờ nhận phòng
            var confirmedBookings = _context.Bookings.Count(b => b.Status == 2); // Đã thay đổi
            var checkedInBookings = _context.Bookings.Count(b => b.Status == 2); // Đã nhận phòng
            var completedBookings = _context.Bookings.Count(b => b.Status == 3 || b.Status == 5); // Hoàn thành

            // Booking gần đây
            var recentBookings = _context.Bookings
                .Include(b => b.Customer)
                .OrderByDescending(b => b.CreateDate)
                .Take(10)
                .Select(b => new BookingView { Booking = b })
                .ToList();

            // Booking cần check-in hôm nay
            var todayCheckins = _context.Bookings
                .Include(b => b.Customer)
                .Where(b => b.CheckinDate.Date == today && b.Status == 2)
                .Select(b => new BookingView { Booking = b })
                .ToList();

            var model = new AdminDashboardViewModel
            {
                TotalRooms = totalRooms,
                AvailableRooms = availableRooms,
                TodayBookings = todayBookings,
                PendingPayments = pendingBookings,
                MonthlyRevenue = monthlyRevenue,
                RecentBookings = recentBookings,
                TodayCheckins = todayCheckins,
                // Thêm các thống kê mới
                ConfirmedBookings = confirmedBookings,
                CheckedInBookings = checkedInBookings,
                CompletedBookings = completedBookings
            };

            return View(model);
        }

        private int GetAvailableRoomsCount(DateTime start, DateTime end)
        {
            var bookedRooms = _context.BookingDetails
                .Where(bd => _context.Bookings
                    .Any(b => b.Id == bd.BookingId && 
                             (b.Status != 2 || b.Status != 1 || b.Status != 0) && 
                             b.CheckinDate < end && 
                             b.CheckoutDate > start))
                .Select(bd => bd.RoomId)
                .Distinct()
                .Count();

            return _context.Rooms.Count() - bookedRooms;
        }

        public IActionResult ProcessRefund(int bookingId)
        {
            var booking = _context.Bookings.Include(x=>x.BookingDetails).SingleOrDefault(x=>x.Id == bookingId);
            if (booking == null || booking.Status == 1)
                return NotFound();

            // Cập nhật trạng thái để đánh dấu đã xử lý hoàn tiền
            booking.Status = 4; // Đã hủy và hoàn tiền
            _context.Bookings.Update(booking);

            // Cập nhật trạng thái phòng liên quan
            foreach (var detail in booking.BookingDetails)
            {
                var room = _context.Rooms.Find(detail.RoomId);
                if (room != null)
                {
                    room.Status = "Available"; // Đặt lại trạng thái phòng
                    _context.Rooms.Update(room);
                }
            }

            _context.SaveChanges();

            TempData["Success"] = "Đã xử lý hoàn tiền thành công.";
            return RedirectToAction("Index");
        }

        // Action: Check-in booking (admin dashboard button)
        [HttpPost]
        public IActionResult CheckIn(int bookingId)
        {
            var booking = _context.Bookings.Find(bookingId);
            if (booking == null || booking.Status == 0)
                return NotFound();

            booking.Status = 2; // Đã nhận phòng
            booking.CheckinDate = DateTime.Now;
            _context.Bookings.Update(booking);

            // Cập nhật trạng thái phòng liên quan
            foreach (var detail in booking.BookingDetails)
            {
                var room = _context.Rooms.Find(detail.RoomId);
                if (room != null)
                {
                    room.Status = "Occupied"; // Đặt trạng thái phòng là đã nhận phòng
                    _context.Rooms.Update(room);
                }
            }

            _context.SaveChanges();

            TempData["Success"] = "Khách đã nhận phòng thành công.";
            return RedirectToAction("Index");
        }

        // Action: Booking Details (admin dashboard button)
        public IActionResult Details(int bookingId)
        {
            var booking = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Room)
                .FirstOrDefault(b => b.Id == bookingId);
            if (booking == null)
                return NotFound();

            var viewModel = new BookingView { Booking = booking };
            return View(viewModel);
        }
    }
}
