using DemoHotelBooking.Models;
using DemoHotelBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoHotelBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Revenue(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Now.Date.AddDays(-30);
            var end = endDate ?? DateTime.Now.Date;

            var bookings = _context.Bookings
                .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Room)
                .Include(b => b.Customer)
                .Where(b => b.CreateDate >= start && 
                           b.CreateDate <= end && 
                           b.Status != 3) // Loại trừ booking đã hủy
                .ToList();

            var revenueData = bookings
                .GroupBy(b => b.CreateDate.Date)
                .Select(g => new ReportRevenue
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(b => b.BookingDetails.Sum(bd => bd.Price * 
                        (b.CheckoutDate - b.CheckinDate).Days)),
                    BookingCount = g.Count(),
                    RoomCount = g.Sum(b => b.BookingDetails.Count())
                })
                .OrderBy(r => r.Date)
                .ToList();

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.TotalRevenue = revenueData.Sum(r => r.TotalRevenue);
            ViewBag.TotalBookings = revenueData.Sum(r => r.BookingCount);

            return View(revenueData);
        }

        public IActionResult BookingDetail(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Now.Date.AddDays(-7);
            var end = endDate ?? DateTime.Now.Date.AddDays(1);

            var bookings = _context.Bookings
                .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Room)
                .Include(b => b.Customer)
                .Where(b => b.CreateDate >= start && 
                           b.CreateDate <= end)
                .OrderByDescending(b => b.CreateDate)
                .ToList();

            var reportDetails = bookings.Select(b => new ReportDetail
            {
                BookingId = b.Id,
                CustomerName = b.Customer?.FullName ?? "N/A",
                CustomerPhone = b.Customer?.PhoneNumber ?? "N/A",
                BookingDate = b.CreateDate,
                CheckInDate = b.CheckinDate,
                CheckOutDate = b.CheckoutDate,
                RoomNames = string.Join(", ", b.BookingDetails.Select(bd => bd.Room?.Name ?? "N/A")),
                TotalAmount = b.BookingDetails.Sum(bd => bd.Price * 
                    (b.CheckoutDate - b.CheckinDate).Days),
                Status = b.Status switch
                {
                    1 => "Đã cọc",
                    2 => "Đã xác nhận",
                    3 => "Đã hủy",
                    4 => "Đã checkin",
                    5 => "Chờ dọn phòng",
                    6 => "Hoàn thành",
                    _ => "Không xác định"
                }
            }).ToList();

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;

            return View(reportDetails);
        }

        public IActionResult RoomOccupancy(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Now.Date;
            var endDate = selectedDate.AddDays(1);

            var rooms = _context.Rooms.ToList();
            var bookings = _context.Bookings
                .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Room)
                .Include(b => b.Customer)
                .Where(b => b.Status != 3 && // Không bao gồm booking đã hủy
                           b.CheckinDate < endDate && 
                           b.CheckoutDate > selectedDate)
                .ToList();

            var occupancyData = rooms.Select(room => 
            {
                var booking = bookings.FirstOrDefault(b => 
                    b.BookingDetails.Any(bd => bd.RoomId == room.Id));
                
                return new DetailedRoomStatusViewModel
                {
                    Room = room,
                    IsAvailable = booking == null,
                    CustomerName = booking?.Customer?.FullName,
                    CustomerPhone = booking?.Customer?.PhoneNumber,
                    BookedCheckInDate = booking?.CheckinDate,
                    BookedCheckOutDate = booking?.CheckoutDate,
                    BookingStatus = booking?.Status switch
                    {
                        1 => "Pending",
                        2 => "Confirmed", 
                        4 => "CheckedIn",
                        5 => "Cleaning",
                        6 => "CheckedOut",
                        _ => "Pending"
                    }
                };
            }).ToList();

            ViewBag.SelectedDate = selectedDate;
            ViewBag.OccupiedRooms = occupancyData.Count(r => !r.IsAvailable);
            ViewBag.AvailableRooms = occupancyData.Count(r => r.IsAvailable);
            ViewBag.OccupancyRate = rooms.Count > 0 ? 
                (double)ViewBag.OccupiedRooms / rooms.Count * 100 : 0;

            return View(occupancyData);
        }
    }
}