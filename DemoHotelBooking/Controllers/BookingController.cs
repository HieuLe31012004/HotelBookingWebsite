using DemoHotelBooking.Models;
using DemoHotelBooking.Models.Momo;
using DemoHotelBooking.Models.Order;
using DemoHotelBooking.Services;
using DemoHotelBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;


namespace DemoHotelBooking.Controllers
{
    public class BookingController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;
        private readonly ILogger<BookingController> _logger;

        private BookingViewModel currentBooking;
        private AppUser currentUser;
        public BookingController(IMomoService momoService, AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<IdentityRole> roleManager, IVnPayService service, ILogger<BookingController> logger)
        {
            _vnPayService = service;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _momoService = momoService;
            _logger = logger;
        }
        private Task<AppUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        //Lấy thông tin đặt phòng từ session
        private BookingViewModel GetBookingFromSession()
        {
            try
            {
                var bookingJson = HttpContext.Session.GetString("CurrentBooking");
                if (string.IsNullOrEmpty(bookingJson))
                {
                    return new BookingViewModel
                    {
                        CheckinDate = DateTime.Now,
                        CheckoutDate = DateTime.Now.AddDays(1),
                        SelectedRooms = new List<Room>(),
                        AvailbleRooms = new List<Room>()
                    };
                }

                var booking = JsonConvert.DeserializeObject<BookingViewModel>(bookingJson);

                // Đảm bảo các properties không null
                if (booking == null)
                {
                    return new BookingViewModel
                    {
                        CheckinDate = DateTime.Now,
                        CheckoutDate = DateTime.Now.AddDays(1),
                        SelectedRooms = new List<Room>(),
                        AvailbleRooms = new List<Room>()
                    };
                }

                // Đảm bảo các list không null
                if (booking.SelectedRooms == null)
                    booking.SelectedRooms = new List<Room>();
                if (booking.AvailbleRooms == null)
                    booking.AvailbleRooms = new List<Room>();
                if (booking.Customer == null)
                {
                    AppUser currentUser = _userManager.GetUserAsync(User).GetAwaiter().GetResult();
                    booking.Customer = currentUser;
                }
                return booking;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting booking from session");
                return new BookingViewModel
                {
                    CheckinDate = DateTime.Now,
                    CheckoutDate = DateTime.Now.AddDays(1),
                    SelectedRooms = new List<Room>(),
                    AvailbleRooms = new List<Room>()
                };
            }
        }
        //Lưu thông tin đặt phòng vào session
        private void SaveBookingToSession(BookingViewModel booking)
        {
            var bookingJson = JsonConvert.SerializeObject(booking);
            HttpContext.Session.SetString("CurrentBooking", bookingJson);
        }

        //Đặt phòng
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Booking(int? id)
        {
            try
            {
                currentBooking = GetBookingFromSession();

                // Đảm bảo currentBooking không null
                if (currentBooking == null)
                {
                    return RedirectToAction("Rooms", "Room");
                }

                UpDateAvailbleRooms();
                currentUser = await GetCurrentUserAsync();

                if (currentUser != null)
                {
                    currentBooking.Phone = currentUser.PhoneNumber;
                    currentBooking.Name = currentUser.FullName;
                }

                if (id != null)
                {
                    var room = _context.Rooms.FirstOrDefault(r => r.Id == id);
                    if (room != null && !currentBooking.SelectedRooms.Any(r => r.Id == room.Id))
                    {
                        currentBooking.SelectedRooms.Add(room);
                    }
                }

                // Đảm bảo pricing được tính toán
                UpdateBookingPricing(currentBooking, User.Identity.IsAuthenticated);

                ViewData["availbleRooms"] = currentBooking.AvailbleRooms ?? new List<Room>();
                ViewData["bookingRooms"] = currentBooking.SelectedRooms ?? new List<Room>();
                ViewData["currentBooking"] = currentBooking;
                SaveBookingToSession(currentBooking);
                return View(currentBooking);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in Booking GET method");
                TempData["Error"] = "Có lỗi xảy ra khi tải trang đặt phòng. Vui lòng thử lại.";
                return RedirectToAction("Rooms", "Room");
            }
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Booking(BookingViewModel model)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                // Collect all validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                ViewBag.Error = string.Join("; ", errors);

                // Return to booking view with current data
                currentBooking = GetBookingFromSession();
                if (currentBooking != null)
                {
                    UpDateAvailbleRooms();
                    ViewData["availbleRooms"] = currentBooking.AvailbleRooms ?? new List<Room>();
                    ViewData["bookingRooms"] = currentBooking.SelectedRooms ?? new List<Room>();
                    ViewData["currentBooking"] = currentBooking;
                }
                else
                {
                    ViewData["availbleRooms"] = new List<Room>();
                    ViewData["bookingRooms"] = new List<Room>();
                }

                return View(model);
            }

            TimeSpan stayDuration = model.CheckoutDate - model.CheckinDate;
            // Tính số ngày chính xác - nếu quá 24h thì tính thêm 1 ngày
            int numberOfDays = (int)Math.Ceiling(stayDuration.TotalDays);
            if (numberOfDays < 1) numberOfDays = 1;
            var user = _context.Users.FirstOrDefault(i => i.PhoneNumber == model.Phone);
            //Kiểm tra đã đăng ký chưa
            if (user == null)
            {
                if (!await CreateUnRegisterUser(model.Phone, model.Name))
                    return View(currentBooking); //lưu tài khoản loại chưa đăng ký
                user = await _userManager.FindByNameAsync(model.Phone);
            }

            //if (!user.IsRegisted)
            //{
            //    user.FullName = model.Name;
            //    _context.Users.Add(user);
            //}            

            currentBooking = GetBookingFromSession();

            // Kiểm tra null trước khi sử dụng
            if (currentBooking == null)
            {
                ViewBag.Error = "Phiên đặt phòng đã hết hạn. Vui lòng bắt đầu lại!";
                return RedirectToAction("Rooms", "Room");
            }

            if (currentBooking.SelectedRooms == null || currentBooking.SelectedRooms.Count == 0)
            {
                ViewBag.Error = "Chưa chọn phòng!!!";
                UpDateAvailbleRooms();
                ViewData["availbleRooms"] = currentBooking.AvailbleRooms ?? new List<Room>();
                ViewData["bookingRooms"] = currentBooking.SelectedRooms ?? new List<Room>();
                return View(model);
            }
            if (model.CheckinDate < DateTime.Now)
            {
                ViewBag.Error = "Không thể nhận/trả phòng ở thời điểm này!!!";
                ViewData["availbleRooms"] = currentBooking.AvailbleRooms;
                ViewData["bookingRooms"] = currentBooking.SelectedRooms;
                return View(model);
            }
            if (model.CheckinDate >= model.CheckoutDate)
            {
                ViewBag.Error = "Ngày trả phòng phải sau ngày nhận phòng!";
                ViewData["availbleRooms"] = currentBooking.AvailbleRooms;
                ViewData["bookingRooms"] = currentBooking.SelectedRooms;
                return View(model);
            }

            // Kiểm tra thời gian đặt tối thiểu (ít nhất 3 giờ)
            TimeSpan minimumStay = TimeSpan.FromHours(3);
            if (stayDuration < minimumStay)
            {
                ViewBag.Error = "Thời gian đặt phòng tối thiểu là 3 giờ!";
                ViewData["availbleRooms"] = currentBooking.AvailbleRooms;
                ViewData["bookingRooms"] = currentBooking.SelectedRooms;
                return View(model);
            }

            // Kiểm tra tính khả dụng của các phòng đã chọn
            foreach (var room in currentBooking.SelectedRooms)
            {
                if (!RoomIsAvailable(room.Id, model.CheckinDate, model.CheckoutDate))
                {
                    ViewBag.Error = $"Phòng {room.Name} không khả dụng trong thời gian đã chọn!";
                    ViewData["availbleRooms"] = currentBooking.AvailbleRooms;
                    ViewData["bookingRooms"] = currentBooking.SelectedRooms;
                    return View(model);
                }
            }

            currentBooking.CheckinDate = model.CheckinDate;
            currentBooking.CheckoutDate = model.CheckoutDate;
            currentBooking.Name = model.Name;
            currentBooking.Phone = model.Phone;

            // Tính toán chi tiết giá cả
            currentBooking.NumberOfRooms = currentBooking.SelectedRooms.Count;
            currentBooking.NumberOfNights = numberOfDays;

            // Giá gốc (số phòng × giá phòng × số ngày)
            currentBooking.BasePrice = currentBooking.SelectedRooms.Sum(i => i.Price) * numberOfDays;

            // Ưu đãi thành viên (2% cho thành viên đã đăng ký)
            if (User.Identity.IsAuthenticated)
            {
                currentBooking.MemberDiscount = currentBooking.BasePrice * 0.02; // 2% giảm giá
            }
            else
            {
                currentBooking.MemberDiscount = 0;
            }

            // Giá sau giảm giá
            double priceAfterDiscount = currentBooking.BasePrice - currentBooking.MemberDiscount;

            // VAT 8%
            currentBooking.VAT = priceAfterDiscount * 0.08;

            // Giá cuối cùng
            currentBooking.FinalPrice = priceAfterDiscount + currentBooking.VAT;

            // Tính tiền cọc: 20% tổng tiền cuối cùng
            currentBooking.Deposit = currentBooking.FinalPrice * 0.2;
            currentBooking.Amount = currentBooking.FinalPrice;
            currentBooking.Customer = user;
            SaveBookingToSession(currentBooking);
            var vnPayModel = new VnPaymentRequestModel()
            {
                Amount = (double)currentBooking.Deposit,
                CreateDate = DateTime.Now,
                Description = $"{model.Phone}-{model.Name}",
                FullName = model.Name,
                BookingId = new Random().Next(1, 1000)
            };

            try
            {
                var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, vnPayModel, "booking");

                if (!string.IsNullOrEmpty(paymentUrl))
                {
                    _logger?.LogInformation("VnPay payment URL created successfully for booking");
                    return Redirect(paymentUrl);
                }
                else
                {
                    ViewBag.Error = "Không thể tạo liên kết thanh toán. Vui lòng thử lại sau.";
                    _logger?.LogError("VnPay payment URL is null or empty");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra khi tạo thanh toán. Vui lòng thử lại sau.";
                _logger?.LogError(ex, "Error creating VnPay payment");
            }

            // Nếu có lỗi, trở lại trang booking
            UpDateAvailbleRooms();
            ViewData["availbleRooms"] = currentBooking.AvailbleRooms ?? new List<Room>();
            ViewData["bookingRooms"] = currentBooking.SelectedRooms ?? new List<Room>();
            ViewData["currentBooking"] = currentBooking;
            return View(model);
        }
        //Chọn phòng
        [Authorize]
        public IActionResult AddRoom(int Id)
        {
            currentBooking = GetBookingFromSession();
            var room = _context.Rooms.Find(Id);

            // Đảm bảo SelectedRooms không null
            if (currentBooking.SelectedRooms == null)
                currentBooking.SelectedRooms = new List<Room>();

            // Kiểm tra xem phòng đã được chọn chưa
            if (room != null && !currentBooking.SelectedRooms.Any(i => i.Id == Id))
            {
                // Kiểm tra tính khả dụng của phòng
                if (RoomIsAvailable(Id, currentBooking.CheckinDate, currentBooking.CheckoutDate))
                {
                    currentBooking.SelectedRooms.Add(room);

                    // Cập nhật lại giá cả
                    UpdateBookingPricing(currentBooking, User.Identity.IsAuthenticated);
                }
                else
                {
                    // Có thể thêm thông báo lỗi ở đây
                    TempData["RoomError"] = $"Phòng {room.Name} không khả dụng trong thời gian đã chọn.";
                }
            }

            ViewData["availbleRooms"] = currentBooking.AvailbleRooms;
            ViewData["bookingRooms"] = currentBooking.SelectedRooms;
            ViewData["currentBooking"] = currentBooking;
            SaveBookingToSession(currentBooking);
            return PartialView("BookingRooms", currentBooking.SelectedRooms);
        }
        //Bỏ chọn phòng
        [Authorize]
        public IActionResult RemoveRoom(int id)
        {
            currentBooking = GetBookingFromSession();

            // Đảm bảo SelectedRooms không null
            if (currentBooking.SelectedRooms == null)
                currentBooking.SelectedRooms = new List<Room>();

            var room = currentBooking.SelectedRooms.FirstOrDefault(i => i.Id == id);
            if (room == null)
                return NotFound();
            currentBooking.SelectedRooms.Remove(room);

            // Cập nhật lại giá cả
            UpdateBookingPricing(currentBooking, User.Identity.IsAuthenticated);

            SaveBookingToSession(currentBooking);
            ViewData["availbleRooms"] = currentBooking.AvailbleRooms;
            ViewData["bookingRooms"] = currentBooking.SelectedRooms;
            ViewData["currentBooking"] = currentBooking;
            return PartialView("BookingRooms", currentBooking.SelectedRooms);
        }
        public bool RoomIsAvailable(int roomId, DateTime startDate, DateTime endDate)
        {
            return !_context.Bookings
                .Where(b =>
                    (b.Status == DemoHotelBooking.Models.BookingStatus.Deposited ||
                     b.Status == DemoHotelBooking.Models.BookingStatus.CancelRequested ||
                     b.Status == DemoHotelBooking.Models.BookingStatus.CheckedIn) &&
                    b.CheckinDate < endDate &&
                    b.CheckoutDate > startDate)
                .Any(b => _context.BookingDetails.Any(d => d.RoomId == roomId && d.BookingId == b.Id));
        }

        [HttpPost]
        public IActionResult UpdateTime(DateTime start, DateTime end)
        {
            currentBooking = GetBookingFromSession();
            currentBooking.CheckinDate = start;
            currentBooking.CheckoutDate = end;

            // Cập nhật lại giá cả khi thay đổi thời gian
            UpdateBookingPricing(currentBooking, User.Identity.IsAuthenticated);

            UpDateAvailbleRooms();

            // Lưu lại session với thông tin mới
            SaveBookingToSession(currentBooking);

            ViewData["availbleRooms"] = currentBooking.AvailbleRooms;
            ViewData["bookingRooms"] = currentBooking.SelectedRooms;
            ViewData["currentBooking"] = currentBooking;

            return PartialView("ListRoomAvailble", currentBooking.AvailbleRooms);
        }
        //cập nhật phòng trống
        public void UpDateAvailbleRooms()
        {
            // Đảm bảo currentBooking không null
            if (currentBooking == null)
            {
                currentBooking = GetBookingFromSession();
            }

            var rooms = _context.Rooms
                .Include(x => x.RoomImages)
                .Select(r => new Room
                {
                    Id = r.Id,
                    Name = r.Name,
                    Type = r.Type,
                    FloorNumber = r.FloorNumber,
                    Price = r.Price,
                    Introduce = r.Introduce,
                    Description = r.Description,
                    DAP = r.DAP,
                    MAP = r.MAP,
                    Extension = r.Extension,
                    RoomNumber = r.RoomNumber,
                    RoomType = r.RoomType,
                    Capacity = r.Capacity,
                    Status = r.Status,
                    RoomImages = r.RoomImages.Select(img => new RoomImage
                    {
                        RoomId = img.RoomId,
                        Path = img.Path
                    }).ToList()
                })
                .ToList();
            //var rooms = _context.Rooms.Include(x=>x.RoomImages).ToList();

            // Đảm bảo AvailbleRooms không null
            if (currentBooking.AvailbleRooms == null)
                currentBooking.AvailbleRooms = new List<Room>();

            currentBooking.AvailbleRooms.Clear();
            foreach (var room in rooms)
            {
                if (RoomIsAvailable(room.Id, DateTime.Now, DateTime.Now))
                {
                    currentBooking.AvailbleRooms.Add(room);
                }
            }
            SaveBookingToSession(currentBooking);
        }
        //tạo tài khoản cho người chưa đăng ký
        public async Task<bool> CreateUnRegisterUser(string Phone, string FullName)
        {
            bool flag = _context.Users.Any(i => i.PhoneNumber == Phone);
            if (flag) return false;

            var user = new AppUser
            {
                UserName = Phone,
                FullName = FullName,
                IsRegisted = false,
                PhoneNumber = Phone
            };
            var result = await _userManager.CreateAsync(user, "Abcd@1234");
            if (result.Succeeded)
            {
                // Gán vai trò "Customer" cho người dùng
                await _userManager.AddToRoleAsync(user, "Customer");

                return true;
            }
            return false;
        }


        //kết quả trả về của VnPay
        public async Task<IActionResult> PaymentCallBack()
        {
            try
            {
                // Lấy thông tin từ query string của VnPay để xác thực và cập nhật trạng thái đơn hàng
                var response = _vnPayService.PaymentExecute(HttpContext.Request.Query);

                if (response == null)
                {
                    TempData["Error"] = "Không thể xử lý kết quả thanh toán từ VnPay";
                    return RedirectToAction("PaymentFail");
                }

                if (!response.Success)
                {
                    TempData["Error"] = $"Thanh toán thất bại: {response.VnPayResponseCode}";
                    return RedirectToAction("PaymentFail");
                }

                //lấy thông tin đặt phòng từ viewmodel
                currentBooking = GetBookingFromSession();
                if (currentBooking == null || currentBooking.Customer == null)
                {
                    HttpContext.Session.Remove("CurrentBooking");
                    TempData["Error"] = "Không tìm thấy thông tin đặt phòng";
                    return RedirectToAction("PaymentFail");
                }

                // Log payment success
                _logger.LogInformation("VnPay payment successful for booking. OrderId: {OrderId}, TransactionId: {TransactionId}",
                    response.OrderId, response.TransactionId);

                //lấy thông tin khách hàng
                // tạo mới đơn đặt phòng
                var booking = new Booking
                {
                    CreateDate = DateTime.Now,
                    CheckinDate = currentBooking.CheckinDate,
                    CheckoutDate = currentBooking.CheckoutDate,
                    Deposit = (double)currentBooking.Deposit,
                    TotalPrice = (decimal)currentBooking.FinalPrice,
                    CusID = currentBooking.Customer.Id,
                    Status = DemoHotelBooking.Models.BookingStatus.Deposited // Đã đặt cọc
                };
                //lưu vào DB
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();


                List<BookingDetail> bookings = new List<BookingDetail>();
                //thêm và lưu danh sách phòng đã chọn
                foreach (var room in currentBooking.SelectedRooms)
                {
                    var detail = new BookingDetail
                    {
                        BookingId = booking.Id,
                        RoomId = room.Id,
                        Price = room.Price
                    };
                    bookings.Add(detail);
                    _context.BookingDetails.Add(detail);
                }
                await _context.SaveChangesAsync();


                var selectedRoomIds = currentBooking.SelectedRooms.Select(sr => sr.Id).ToList();

                var updateRooms = _context.Rooms
                    .Where(r => selectedRoomIds.Contains(r.Id))
                    .ToList();

                foreach (var room in updateRooms)
                {
                    if (booking.CheckinDate.Date == DateTime.Today)
                    {
                        room.Status = "Reserved";
                    }
                }

                _context.UpdateRange(updateRooms);
                await _context.SaveChangesAsync();

                //xóa viewmodel
                HttpContext.Session.Remove("CurrentBooking");

                TempData["BookingId"] = booking.Id;
                TempData["Success"] = "Đặt phòng thành công! Vui lòng đến khách sạn để nhận phòng.";

                return RedirectToAction("PaymentSuccess");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VnPay payment callback");
                TempData["Error"] = "Có lỗi xảy ra khi xử lý đặt phòng: " + ex.Message;
                return RedirectToAction("PaymentFail");
            }
        }
        public IActionResult PaymentSuccess()
        {
            return View();
        }
        public IActionResult PaymentFail()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> History()
        {
            var cus = await GetCurrentUserAsync();
            if (cus == null) return RedirectToAction("Login", "Account");

            var bks = _context.Bookings
                .Where(i => i.CusID == cus.Id)
                .Include(i => i.Customer)
                .OrderByDescending(i => i.CreateDate)
                .ToList();

            var models = new List<BookingView>();
            foreach (var booking in bks)
            {
                // Lấy danh sách phòng đã đặt
                var rooms = _context.BookingDetails
                    .Where(bd => bd.BookingId == booking.Id)
                    .Include(bd => bd.Room)
                    .ToList();

                // Lấy thông tin hóa đơn (nếu có)
                var invoice = _context.Invoices
                    .FirstOrDefault(i => i.BookingId == booking.Id);

                var bookingView = new BookingView
                {
                    Booking = booking,
                    Rooms = rooms
                };

                models.Add(bookingView);
            }
            return View(models);
        }
        [Authorize]
        public async Task<IActionResult> BookingDetails(int id)
        {
            var cus = await GetCurrentUserAsync();
            if (cus == null) return RedirectToAction("Login", "Account");

            var bkdt = _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefault(b => b.Id == id && b.CusID == cus.Id);

            if (bkdt == null)
            {
                TempData["Error"] = "Không tìm thấy đặt phòng hoặc bạn không có quyền xem đặt phòng này.";
                return RedirectToAction("History");
            }

            var dt = _context.BookingDetails
                .Where(i => i.BookingId == id)
                .Include(i => i.Room)
                .ToList();

            var invoice = _context.Invoices
                .FirstOrDefault(i => i.BookingId == id);

            var model = new BookingView
            {
                Booking = bkdt,
                Rooms = dt
            };
            return View(model);
        }
        [Authorize]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var cus = await GetCurrentUserAsync();
            if (cus == null) return RedirectToAction("Login", "Account");
            var bk = await _context.Bookings.Include(x => x.BookingDetails).SingleOrDefaultAsync(x => x.Id == id);
            if (bk == null) return NotFound();

            // Kiểm tra quyền sở hữu booking
            if (bk.CusID != cus.Id)
            {
                return Forbid();
            }

            // Chỉ cho phép hủy nếu chưa nhận phòng (Status 1 hoặc 2)
            if (bk.Status != 0)
            {
                TempData["Error"] = "Không thể hủy đặt phòng ở trạng thái hiện tại.";
                return RedirectToAction("BookingDetails", new { id = id });
            }

            bk.Status = 1; // Đã hủy
            _context.Bookings.Update(bk);

            // Cập nhật trạng thái phòng liên quan
            var bookingDetails = bk.BookingDetails.Select(x => x.RoomId).ToList();
            var rooms = _context.Rooms
                .Where(r => bookingDetails.Contains(r.Id))
                .ToList();
            RoomStatusHelper.UpdateAllRoomStatuses(rooms, _context.Bookings.ToList());

            _context.SaveChanges();

            TempData["Success"] = "Đã hủy đặt phòng thành công. Bạn có thể liên hệ để được hoàn cọc.";
            return RedirectToAction("BookingDetails", new { id = id });
        }
        [Authorize]
        public async Task<IActionResult> BookingStatus(int id)
        {
            var cus = await GetCurrentUserAsync();
            if (cus == null) return RedirectToAction("Login", "Account");

            var booking = _context.Bookings.Include(b => b.Customer).FirstOrDefault(b => b.Id == id && b.CusID == cus.Id);
            if (booking == null) return NotFound();

            var rooms = _context.BookingDetails.Where(bd => bd.BookingId == id).Include(bd => bd.Room).ToList();
            var invoice = _context.Invoices.FirstOrDefault(i => i.BookingId == id);

            var model = new BookingView
            {
                Booking = booking,
                Rooms = rooms
            };

            // Thêm thông tin thanh toán
            ViewBag.Invoice = invoice;
            ViewBag.CanCancel = (booking.Status == 0 || booking.Status == 1) && booking.CheckinDate > DateTime.Now;
            ViewBag.PaymentInfo = GetPaymentStatusInfo(booking, invoice);

            return View(model);
        }

        private string GetPaymentStatusInfo(Booking booking, Invoice invoice)
        {
            if (invoice == null)
            {
                return "Chưa có hóa đơn";
            }

            return invoice.Status switch
            {
                0 => "Chưa thanh toán",
                1 => "Đã check-in",
                2 => "Đã check-out",
                3 => "Đã thanh toán đầy đủ",
                _ => "Trạng thái không xác định"
            };
        }

        // Method để cập nhật lại giá cả khi thêm/bớt phòng
        private void UpdateBookingPricing(BookingViewModel booking, bool isAuthenticated)
        {
            // Đảm bảo SelectedRooms không null
            if (booking.SelectedRooms == null)
            {
                booking.SelectedRooms = new List<Room>();
            }

            if (booking.SelectedRooms.Any())
            {
                // Tính số ngày
                TimeSpan stayDuration = booking.CheckoutDate - booking.CheckinDate;
                int numberOfDays = (int)Math.Ceiling(stayDuration.TotalDays);
                if (numberOfDays < 1) numberOfDays = 1;

                // Cập nhật thông tin cơ bản
                booking.NumberOfRooms = booking.SelectedRooms.Count;
                booking.NumberOfNights = numberOfDays;

                // Giá gốc (số phòng × giá phòng × số ngày)
                booking.BasePrice = booking.SelectedRooms.Sum(i => i.Price) * numberOfDays;

                // Ưu đãi thành viên (2% cho thành viên đã đăng ký)
                if (isAuthenticated)
                {
                    booking.MemberDiscount = booking.BasePrice * 0.02; // 2% giảm giá
                }
                else
                {
                    booking.MemberDiscount = 0;
                }

                // Giá sau giảm giá
                double priceAfterDiscount = booking.BasePrice - booking.MemberDiscount;

                // VAT 8%
                booking.VAT = priceAfterDiscount * 0.08;

                // Giá cuối cùng
                booking.FinalPrice = priceAfterDiscount + booking.VAT;

                // Tính tiền cọc: 20% tổng tiền cuối cùng
                booking.Deposit = booking.FinalPrice * 0.2;
                booking.Amount = booking.FinalPrice;
            }
            else
            {
                // Reset về 0 nếu không có phòng nào
                booking.BasePrice = 0;
                booking.MemberDiscount = 0;
                booking.VAT = 0;
                booking.FinalPrice = 0;
                booking.Deposit = 0;
                booking.Amount = 0;
                booking.NumberOfRooms = 0;
            }
        }

        // Action để refresh phần selected rooms với pricing mới
        [HttpPost]
        public IActionResult RefreshSelectedRooms()
        {
            currentBooking = GetBookingFromSession();

            // Cập nhật lại giá cả với thời gian hiện tại
            UpdateBookingPricing(currentBooking, User.Identity.IsAuthenticated);

            ViewData["availbleRooms"] = currentBooking.AvailbleRooms;
            ViewData["bookingRooms"] = currentBooking.SelectedRooms;
            ViewData["currentBooking"] = currentBooking;

            SaveBookingToSession(currentBooking);

            return PartialView("BookingRooms", currentBooking.SelectedRooms);
        }

        // Redirect guest users to login page with a message
        [HttpGet]
        public IActionResult BookingRequiresLogin(int? roomId)
        {
            TempData["Info"] = "Vui lòng đăng nhập để đặt phòng. Nếu chưa có tài khoản, hãy đăng ký ngay!";
            TempData["RoomId"] = roomId;
            return RedirectToAction("Login", "Account");
        }

        // Debug action for VnPay testing (previously Momo)
        public IActionResult VnPayDebug()
        {
            // Hiển thị thông tin cấu hình VnPay
            ViewBag.VnPayUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            ViewBag.TmnCode = "1RK2YH4I";
            ViewBag.Version = "2.1.0";
            ViewBag.Command = "pay";
            ViewBag.ReturnUrl = "http://localhost:5000/Booking/PaymentCallBack";
            ViewBag.HasHashSecret = !string.IsNullOrEmpty("097SQMMUKWTJQ317XG3JGD4V1NLHT6UB");

            // Thông tin debug cho developer
            ViewBag.DebugInfo = new
            {
                IsLocalhost = Request.Host.Host.Contains("localhost"),
                CurrentUrl = $"{Request.Scheme}://{Request.Host}",
                PaymentGateway = "VnPay"
            };

            return View();
        }

        // Test VnPay payment action
        [HttpPost]
        public IActionResult TestVnPayPayment(double amount)
        {
            try
            {
                _logger.LogInformation("Testing VnPay payment with amount: {Amount}", amount);

                // Validate amount
                if (amount <= 0)
                {
                    TempData["Error"] = "Số tiền phải lớn hơn 0";
                    return RedirectToAction("VnPayDebug");
                }

                if (amount < 1000)
                {
                    TempData["Error"] = "Số tiền tối thiểu là 1,000 VND";
                    return RedirectToAction("VnPayDebug");
                }

                if (amount > 50000000)
                {
                    TempData["Error"] = "Số tiền tối đa là 50,000,000 VND";
                    return RedirectToAction("VnPayDebug");
                }

                // Create test payment request
                var vnPayModel = new VnPaymentRequestModel
                {
                    FullName = "Test User",
                    Description = $"Test payment {amount:N0} VND",
                    Amount = amount,
                    CreateDate = DateTime.Now,
                    BookingId = new Random().Next(1000, 9999)
                };

                var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, vnPayModel, "test");

                if (!string.IsNullOrEmpty(paymentUrl))
                {
                    _logger.LogInformation("VnPay payment URL created successfully");
                    TempData["Success"] = "Payment URL tạo thành công!";
                    return Redirect(paymentUrl);
                }
                else
                {
                    _logger.LogError("VnPay payment URL creation failed");
                    TempData["Error"] = "Tạo payment URL thất bại";
                    return RedirectToAction("VnPayDebug");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in TestVnPayPayment with amount: {Amount}", amount);
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("VnPayDebug");
            }
        }
    }
}