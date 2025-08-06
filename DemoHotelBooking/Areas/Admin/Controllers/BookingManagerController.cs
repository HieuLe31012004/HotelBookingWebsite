using DemoHotelBooking.Models;
using DemoHotelBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoHotelBooking.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class BookingManagerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookingManagerController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Danh sách tất cả booking
        public async Task<IActionResult> Index(string status = "", string search = "")
        {
            var bookingsQuery = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Room)
                .AsQueryable();

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status) && int.TryParse(status, out int statusInt))
            {
                bookingsQuery = bookingsQuery.Where(b => b.Status == statusInt);
            }

            // Tìm kiếm theo tên khách hàng hoặc ID booking
            if (!string.IsNullOrEmpty(search))
            {
                if (int.TryParse(search, out int searchId))
                {
                    bookingsQuery = bookingsQuery.Where(b => b.Id == searchId ||
                        b.Customer.FullName.Contains(search) ||
                        b.Customer.Email.Contains(search));
                }
                else
                {
                    bookingsQuery = bookingsQuery.Where(b =>
                        b.Customer.FullName.Contains(search) ||
                        b.Customer.Email.Contains(search));
                }
            }

            var bookings = await bookingsQuery
                .OrderByDescending(b => b.CreateDate)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.SearchFilter = search;

            return View(bookings);
        }

        // Chi tiết booking
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Room)
                    .ThenInclude(bd => bd.RoomImages)
                .Include(b => b.Invoices)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Tính toán thống kê
            var totalRooms = booking.BookingDetails.Count;
            var totalNights = (decimal)(booking.CheckoutDate - booking.CheckinDate).Days;
            var roomPricesSum = (decimal)booking.BookingDetails.Sum(bd => bd.Room.Price);
            var basePrice = roomPricesSum * totalNights;
            var discountAmount = roomPricesSum * totalNights * 0.02m; // 2% member discount
            var vatAmount = (basePrice - discountAmount) * 0.08m; // 8% VAT
            var finalAmount = basePrice - discountAmount + vatAmount;

            ViewBag.TotalRooms = totalRooms;
            ViewBag.TotalNights = totalNights;
            ViewBag.BasePrice = basePrice;
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.VatAmount = vatAmount;
            ViewBag.FinalAmount = finalAmount;
            ViewBag.DepositAmount = finalAmount * 0.2m; // 20% deposit

            var data = new BookingView()
            {
                Booking = booking,
                Rooms = booking.BookingDetails.ToList()
            };


            return View(data);
        }

        // Cập nhật trạng thái booking
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, int status, string note = "")
        {
            List<Booking> bookings = new List<Booking>();
            var booking = await _context.Bookings.Include(x => x.BookingDetails).SingleOrDefaultAsync(x=>x.Id == id);
            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy booking" });
            }

            booking.Status = status;
            if (!string.IsNullOrEmpty(note))
            {
                booking.Note = note;
            }
            bookings.Add(booking);
            // Cập nhật ngày checkin nếu trạng thái là đã nhận phòng
            var roomIds = booking.BookingDetails.Select(bd => bd.RoomId).ToList();

            var rooms = await _context.Rooms
                .Where(r => roomIds.Contains(r.Id))
                .ToListAsync();
            RoomStatusHelper.UpdateAllRoomStatuses(rooms, bookings);
            _context.UpdateRange(rooms);

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Xóa booking (chỉ admin)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingDetails)
                .Include(b => b.Invoices)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy booking" });
            }

            try
            {
                // Xóa booking details trước
                _context.BookingDetails.RemoveRange(booking.BookingDetails);

                // Xóa invoices nếu có
                if (booking.Invoices.Any())
                {
                    _context.Invoices.RemoveRange(booking.Invoices);
                }

                // Xóa booking
                _context.Bookings.Remove(booking);

                // Cập nhật trạng thái phòng
                var roomIds = booking.BookingDetails.Select(bd => bd.RoomId).ToList();
                var rooms = await _context.Rooms
                    .Where(r => roomIds.Contains(r.Id))
                    .ToListAsync();
                RoomStatusHelper.UpdateAllRoomStatuses(rooms, new List<Booking> { booking });
                _context.UpdateRange(rooms);

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa booking thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Export booking data (placeholder)
        public async Task<IActionResult> Export(DateTime? fromDate, DateTime? toDate)
        {
            return RedirectToAction("Index");
        }

        // Thống kê booking theo ngày/tháng
        public async Task<IActionResult> Statistics()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var nextMonth = thisMonth.AddMonths(1);

            var lastMonth = thisMonth.AddMonths(-1);
            var afterLastMonth = thisMonth;

            var stats = new
            {
                TodayBookings = await _context.Bookings
                    .CountAsync(b => b.CreateDate >= today && b.CreateDate < tomorrow),

                ThisMonthBookings = await _context.Bookings
                    .CountAsync(b => b.CreateDate >= thisMonth && b.CreateDate < nextMonth),

                LastMonthBookings = await _context.Bookings
                    .CountAsync(b => b.CreateDate >= lastMonth && b.CreateDate < thisMonth),

                TotalRevenue = await _context.Bookings
                    .Where(b => b.Status == 2 || b.Status == 3 || b.Status == 5)
                    .SumAsync(b => (double)(b.TotalPrice ?? 0)),

                PendingBookings = await _context.Bookings
                    .CountAsync(b => b.Status == 0),

                ConfirmedBookings = await _context.Bookings
                    .CountAsync(b => b.Status == 2),

                CompletedBookings = await _context.Bookings
                    .CountAsync(b => b.Status == 3 || b.Status == 5),

                CancelledBookings = await _context.Bookings
                    .CountAsync(b => b.Status == 1 || b.Status == 4)
            };

            return View(stats);
        }
    }
}
