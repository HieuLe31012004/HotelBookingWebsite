using DemoHotelBooking.Models;
using DemoHotelBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoHotelBooking.Controllers
{
    public class RoomController : Controller
    {
        public readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        public RoomController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var list = _context.Feedbacks.Where(i => i.Status == true).Include(a => a.User).ToList();
            var customer = await _userManager.GetUserAsync(HttpContext.User);
            if (customer != null)
            {
                ViewData["user"] = customer;
                var fb = list.FirstOrDefault(i => i.CusId == customer.Id);
                if (fb != null)
                    ViewData["feedback"] = fb;
                if (_context.Bookings.Any(i => i.CusID == customer.Id))
                    ViewBag.flag = true;
                else ViewBag.flag = false;
            }
            return View(list);
        }
        public IActionResult Rooms(string? s)
        {
            var list = _context.Rooms
                .Include(r => r.RoomImages).ToList();
            if (!string.IsNullOrEmpty(s))
            {
                s = s.ToLower();
                int id;
                if (int.TryParse(s, out id))
                    list = list.Where(i => i.Name.ToLower().Contains(s) || i.Type.ToLower().Contains(s) || i.Id == id).ToList();
                else
                    list = list.Where(i => i.Name.ToLower().Contains(s) || i.Type.ToLower().Contains(s)).ToList();
            }

            // Tạo ViewModel với thông tin tính khả dụng
            var roomViewModels = list.Select(room => CreateDetailedRoomViewModel(room, DateTime.Now, DateTime.Now.AddDays(1))).ToList();

            return View(roomViewModels);
        }
        [HttpPost]
        public IActionResult RoomList(string s)
        {
            var list = _context.Rooms.ToList();
            if (!string.IsNullOrEmpty(s))
            {
                s = s.ToLower();
                int id;
                if (int.TryParse(s, out id))
                    list = list.Where(i => i.Name.ToLower().Contains(s) || i.Type.ToLower().Contains(s) || i.Id == id).ToList();
                else
                    list = list.Where(i => i.Name.ToLower().Contains(s) || i.Type.ToLower().Contains(s)).ToList();
            }

            // Tạo ViewModel với thông tin tính khả dụng
            var roomViewModels = list.Select(room => CreateDetailedRoomViewModel(room, DateTime.Now, DateTime.Now.AddDays(1))).ToList();

            return PartialView("RoomList", roomViewModels);
        }
        [HttpPost]
        public IActionResult RoomListWithDates(string s, DateTime? checkIn, DateTime? checkOut)
        {
            var list = _context.Rooms.ToList();
            if (!string.IsNullOrEmpty(s))
            {
                s = s.ToLower();
                int id;
                if (int.TryParse(s, out id))
                    list = list.Where(i => i.Name.ToLower().Contains(s) || i.Type.ToLower().Contains(s) || i.Id == id).ToList();
                else
                    list = list.Where(i => i.Name.ToLower().Contains(s) || i.Type.ToLower().Contains(s)).ToList();
            }

            // Sử dụng ngày được cung cấp hoặc mặc định là ngày hôm nay
            var startDate = checkIn ?? DateTime.Now;
            var endDate = checkOut ?? DateTime.Now.AddDays(1);

            // Tạo ViewModel với thông tin tính khả dụng theo ngày cụ thể
            var roomViewModels = list.Select(room => CreateDetailedRoomViewModel(room, startDate, endDate)).ToList();

            return PartialView("RoomSearchResult", roomViewModels);
        }

        public bool RoomIsAvailable(int roomId, DateTime startDate, DateTime endDate)
        {
            return !_context.Bookings
                .Where(b =>
                    (b.Status == DemoHotelBooking.Models.BookingStatus.Deposited ||
                     b.Status == DemoHotelBooking.Models.BookingStatus.CancelRequested ||
                     b.Status == DemoHotelBooking.Models.BookingStatus.CheckedIn) &&
                    b.CheckinDate.Date < endDate.Date &&
                    b.CheckoutDate.Date > startDate.Date)
                .Any(b => _context.BookingDetails.Any(d => d.RoomId == roomId && d.BookingId == b.Id));
        }

        private string GetRoomImageUrl(int roomId)
        {
            var roomImage = _context.RoomImages
                .Where(ri => ri.RoomId == roomId)
                .OrderByDescending(ri => ri.IsDefault)
                .FirstOrDefault();

            return roomImage?.Path ?? "/img/no_image.jpg";
        }

        public IActionResult Details(int id)
        {
            var room = _context.Rooms
                .Include(r => r.RoomImages)
                .FirstOrDefault(r => r.Id == id);

            if (room == null)
                return NotFound();

            // Lấy danh sách đường dẫn ảnh cho carousel
            var imageUrls = room.RoomImages != null && room.RoomImages.Any()
                ? room.RoomImages.Select(img => img.Path).ToList()
                : new List<string> { "/img/no_image.jpg" };

            ViewBag.ImageUrls = imageUrls;
            return View(room);
        }
        [HttpPost]
        public async Task<IActionResult> Create(int stars, string comment)
        {
            if (ModelState.IsValid)
            {

                var customer = await _userManager.GetUserAsync(HttpContext.User);
                var feedbacks = _context.Feedbacks.FirstOrDefault(i => i.CusId == customer.Id);
                if (feedbacks == null)
                {
                    feedbacks = new Feedback
                    {
                        Stars = stars,
                        Comment = comment,
                        CusId = customer.Id,
                        CreateDate = DateTime.Now,
                        Status = true
                    };
                    _context.Feedbacks.Add(feedbacks);
                }
                feedbacks.Comment = comment;
                feedbacks.Stars = stars;
                feedbacks.EditDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View();
        }
        public IActionResult HideFeedBack(string id)
        {
            var fb = _context.Feedbacks.Find(id);
            if (fb != null) fb.Status = false;
            _context.Feedbacks.Update(fb);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Search()
        {
            // Lấy danh sách loại phòng từ database
            var roomTypes = _context.Rooms
                .Select(r => r.Type)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            ViewBag.RoomTypes = roomTypes;

            // Lấy danh sách tầng
            var floors = _context.Rooms
                .Select(r => r.FloorNumber)
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            ViewBag.Floors = floors;

            return View();
        }

        // Action để xử lý kết quả tìm kiếm
        [HttpGet]
        public async Task<IActionResult> SearchRooms(
            DateTime? CheckIn,
            DateTime? CheckOut,
            string? RoomType,
            int? Guests,
            decimal? MinPrice,
            decimal? MaxPrice,
            int? FloorNumber,
            string? q)
        {
            var rooms = _context.Rooms.AsQueryable();

            // Tìm kiếm theo từ khóa
            if (!string.IsNullOrEmpty(q))
            {
                q = q.ToLower();
                rooms = rooms.Where(r => r.Name.ToLower().Contains(q) ||
                                        r.Type.ToLower().Contains(q) ||
                                        (!string.IsNullOrEmpty(r.Description) && r.Description.ToLower().Contains(q)));
            }

            // Lọc theo loại phòng
            if (!string.IsNullOrEmpty(RoomType))
            {
                rooms = rooms.Where(r => r.Type.ToLower().Contains(RoomType.ToLower()));
            }

            // Lọc theo số khách (sử dụng MAP là capacity)
            if (Guests.HasValue)
            {
                rooms = rooms.Where(r => r.MAP >= Guests.Value);
            }

            // Lọc theo tầng
            if (FloorNumber.HasValue)
            {
                rooms = rooms.Where(r => r.FloorNumber == FloorNumber.Value);
            }

            // Lọc theo khoảng giá
            if (MinPrice.HasValue)
            {
                rooms = rooms.Where(r => r.Price >= (double)MinPrice.Value);
            }

            if (MaxPrice.HasValue)
            {
                rooms = rooms.Where(r => r.Price <= (double)MaxPrice.Value);
            }

            var roomList = await rooms.ToListAsync();

            // Kiểm tra tính khả dụng cho từng phòng
            var checkInDate = CheckIn ?? DateTime.Now;
            var checkOutDate = CheckOut ?? DateTime.Now.AddDays(1);

            // Tạo ViewModel
            var roomViewModels = roomList.Select(room => new RoomSearchViewModel
            {
                Id = room.Id,
                Name = room.Name,
                Type = room.Type,
                Price = room.Price,
                Capacity = room.MAP,
                Description = room.Description,
                FloorNumber = room.FloorNumber,
                Introduce = room.Introduce,
                DAP = room.DAP,
                MAP = room.MAP,
                ImageUrl = GetRoomImageUrl(room.Id),
                IsAvailable = RoomIsAvailable(room.Id, checkInDate, checkOutDate),
                ImageUrls = room.RoomImages != null && room.RoomImages.Any()
                    ? room.RoomImages.Select(img => img.Path).ToList()
                    : new List<string>(),
                Extension = room.Extension
            }).ToList();

            // Lọc chỉ hiển thị phòng có sẵn nếu có ngày checkin/checkout
            if (CheckIn.HasValue && CheckOut.HasValue)
            {
                roomViewModels = roomViewModels.Where(r => r.IsAvailable).ToList();
            }

            ViewBag.SearchParams = new
            {
                CheckIn = CheckIn?.ToString("yyyy-MM-dd"),
                CheckOut = CheckOut?.ToString("yyyy-MM-dd"),
                RoomType,
                Guests,
                MinPrice,
                MaxPrice,
                FloorNumber,
                Query = q,
                ResultCount = roomViewModels.Count
            };

            return View("SearchResults", roomViewModels);
        }

        private DetailedRoomStatusViewModel CreateDetailedRoomViewModel(Room room, DateTime checkInDate, DateTime checkOutDate)
        {
            var viewModel = new DetailedRoomStatusViewModel
            {
                Room = room,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                IsAvailable = RoomIsAvailable(room.Id, DateTime.Now, DateTime.Now)
            };

            if (!viewModel.IsAvailable)
            {
                // Lấy thông tin booking chi tiết cho phòng này trong khoảng thời gian
                var booking = _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.BookingDetails)
                    .Where(b => (b.Status == 0 || b.Status == 1 || b.Status == 2) &&
                               b.BookingDetails.Any(bd => bd.RoomId == room.Id) &&
                               ((b.CheckinDate.Date <= DateTime.Today && b.CheckoutDate >= DateTime.Today)))
                    .FirstOrDefault();

                if (booking != null)
                {
                    viewModel.CustomerName = booking.Customer?.FullName;
                    viewModel.CustomerPhone = booking.Customer?.PhoneNumber;
                    viewModel.CustomerEmail = booking.Customer?.Email;
                    viewModel.BookingDate = booking.CreateDate;
                    viewModel.BookedCheckInDate = booking.CheckinDate;
                    viewModel.BookedCheckOutDate = booking.CheckoutDate;
                    viewModel.BookingId = booking.Id;

                    var bookingDetail = booking.BookingDetails.FirstOrDefault(bd => bd.RoomId == room.Id);
                    if (bookingDetail != null)
                    {
                        viewModel.TotalAmount = bookingDetail.Price * ((booking.CheckoutDate - booking.CheckinDate).Days);
                        viewModel.DepositAmount = viewModel.TotalAmount * 0.3; // 30% deposit
                    }
                    viewModel.BookingStatus = booking.Status switch
                    {
                        0 => "Deposited",
                        1 => "CancelRequested",
                        2 => "CheckedIn",
                        3 => "CheckedOut",
                        4 => "Cancelled",
                        _ => "Pending"
                    };

                    viewModel.PaymentStatus = booking.Status switch
                    {
                        0 => "Đã cọc",
                        1 => "Đang chờ hoàn tiền",
                        2 => "Đã thanh toán",
                        3 => "Đã thanh toán",
                        4 => "Đã hoàn tiền",
                        _ => "Chưa thanh toán"
                    };

                    viewModel.IsDeposited = booking.Status >= 1;
                }
            }

            return viewModel;
        }
    }
}
