using DemoHotelBooking.Models;
using DemoHotelBooking.Models.Order;
using DemoHotelBooking.Services;
using DemoHotelBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DemoHotelBooking.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;
        public InvoiceController(IMomoService momoService, AppDbContext context, UserManager<AppUser> userManager, IVnPayService vnPayService)
        {
            _context = context;
            _userManager = userManager;
            _vnPayService = vnPayService;
            _momoService = momoService;
        }
        //ds datphong
        public IActionResult BookingList()
        {
            //var list = _context.Bookings.ToList();
            var list = _context.Bookings
                .Include(i => i.Customer)
                .ToList();
            var models = new List<BookingView>();
            foreach (var i in list)
            {
                //i.Customer = _context.Users.Find(i.CusID);
                models.Add(new BookingView
                {
                    Booking = i
                });
            }
            return View(models);
        }
        //chi tiet datphong
        public IActionResult BookingDetails(int id)
        {
            var bkdt = _context.Bookings.Find(id);
            var dt = _context.BookingDetails.Where(i => i.BookingId == id).Include(i => i.Room).ToList();
            var model = new BookingView
            {
                Booking = bkdt,
                Rooms = dt
            };
            return View(model);
        }
        public async Task<IActionResult> Checkin(int id)
        {
            var bk = _context.Bookings.Find(id);
            if (bk == null) return NotFound();
            if (!(bk.Status == 1 || bk.Status == 2))
                return RedirectToAction("BookingList");
            var inv = _context.Invoices.FirstOrDefault(i => i.BookingId == id);
            if (inv != null) return NotFound();
            var Receptionist = await _userManager.GetUserAsync(HttpContext.User);
            if (Receptionist == null)
                return RedirectToAction("Login", "Account");
            var bkdt = _context.BookingDetails.Where(i => i.BookingId == id).ToList();
            TimeSpan stayDuration = bk.CheckoutDate - bk.CheckinDate;
            // Tính số ngày chính xác - nếu quá 24h thì tính thêm 1 ngày
            int numberOfDays = (int)Math.Ceiling(stayDuration.TotalDays);
            if (numberOfDays < 1) numberOfDays = 1;
            inv = new Invoice
            {
                BookingId = id,
                CreateDate = DateTime.Now,
                PaymentDate = DateTime.Now,
                PayMethod = 0,
                Status = 1,
                Amount = 0,
                ReceptionistId = Receptionist.Id,
            };
            bk.Status = 4; // Đã nhận phòng - Chờ thanh toán
            _context.Bookings.Update(bk);
            _context.Invoices.Update(inv);
            _context.Invoices.Add(inv);
            _context.SaveChanges();
            foreach (var i in bkdt)
            {
                i.Room = _context.Rooms.Find(i.RoomId);
                // Tính tiền đúng theo số ngày ở (tối thiểu 1 ngày)
                int actualDays = numberOfDays;
                inv.Amount += i.Room.Price * actualDays;
                var ivdt = new InvoiceDetail
                {
                    InvoiceId = inv.Id,
                    RoomId = i.RoomId,
                    Price = i.Room.Price * actualDays, // Lưu tổng giá theo số ngày
                    SubFee = 0
                };
                _context.InvoiceDetails.Add(ivdt);
                _context.SaveChanges();
            }
            var model = new InvoiceViewModel
            {
                Id = inv.Id,
                CreateDate = inv.CreateDate,
                Status = inv.Status,
                Amount = inv.Amount,
                Receptionist = inv.Receptionist,
            };
            model.Rooms = new List<Room>();
            foreach (var i in bkdt)
            {
                i.Room = _context.Rooms.Find(i.RoomId);
                model.Rooms.Add(i.Room);
            }
            return RedirectToAction("InvoiceDetail", new { id = inv.Id, message = "Lập phiếu thành công" });
        }
        [HttpPost]
        public IActionResult UpdateSubFee(int ivid, int rid, int subfee)
        {
            var iv = _context.Invoices.Find(ivid);
            var dt = _context.InvoiceDetails.FirstOrDefault(i => i.RoomId == rid && i.InvoiceId == ivid);
            subfee = subfee < 0 ? 0 : subfee;
            dt.SubFee = subfee;
            _context.InvoiceDetails.Update(dt);
            _context.SaveChanges();
            if (dt != null)
                return RedirectToAction("InvoiceDetail", new { id = ivid });
            return NotFound();
        }
        //////////////////////////////////////////
        //////////////////////////////////////////
        public IActionResult InvoiceDetail(int id, string? message)
        {
            var inv = _context.Invoices.Find(id);
            var bk = _context.Bookings.Find(inv.BookingId);
            inv.Amount = 0;
            inv.Amount = Math.Round(inv.Amount, 0);
            inv.Booking = _context.Bookings.Find(inv.BookingId);
            inv.Receptionist = _context.Users.Find(inv.ReceptionistId);
            inv.Booking.Customer = _context.Users.Find(inv.Booking.CusID);
            var ivv = new InvoiceView();
            var ivdt = _context.InvoiceDetails.Where(i => i.InvoiceId == id).ToList();
            foreach (var i in ivdt)
            {
                i.Room = _context.Rooms.Find(i.RoomId);
                inv.Amount += i.Price; // Price đã bao gồm số ngày
                inv.Amount += i.Price * i.SubFee / 100;
                ivv.SubFee += i.Price * i.SubFee / 100;
            }
            // Không nhân với numberOfDays nữa vì Price đã tính theo số ngày
            inv.Amount = Math.Round(inv.Amount, 0);
            ivv.Invoice = inv;
            ivv.InvoiceDetail = ivdt;
            ivv.Final = inv.Amount - inv.Booking.Deposit;
            _context.Invoices.Update(inv);
            _context.SaveChanges();
            if (!string.IsNullOrEmpty(message))
                ViewBag.Message = message;
            return View(ivv);
        }
        ///////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////
        public IActionResult Checkout(int id)
        {
            var iv = _context.Invoices.Find(id);
            if (iv == null) return NotFound();
            iv.Status = 2;
            _context.Invoices.Update(iv);
            _context.SaveChanges();
            return RedirectToAction("invoicedetail", new { id = id, message = "Trả phòng thành công" });
        }
        [HttpPost]
        public async Task<IActionResult> Checkout(int id, int paymethod)
        {
            var iv = _context.Invoices.Find(id);
            if (iv == null) return NotFound();
            if (paymethod == 0)
            {
                iv.PayMethod = 0;
                iv.Status = 3; // Đã thanh toán
                iv.PaymentDate = DateTime.Now;
                
                // Cập nhật trạng thái booking
                var booking = _context.Bookings.Find(iv.BookingId);
                if (booking != null)
                {
                    booking.Status = 6; // Đã thanh toán đầy đủ
                    _context.Bookings.Update(booking);
                }
                
                _context.Invoices.Update(iv);
                _context.SaveChanges();
                return RedirectToAction("InvoiceDetail", new { id = id, message = "Thanh toán thành công" });
            }
            string redirectUrl;
            iv.Booking = _context.Bookings.Find(iv.BookingId);
            iv.Booking.Customer = _context.Users.Find(iv.Booking.CusID);
            //var vnPayModel = new VnPaymentRequestModel()
            //{
            //    Amount = Math.Round(iv.Amount - iv.Booking.Deposit, 0),
            //    CreateDate = DateTime.Now,
            //    Description = $"Thanh toan hoa don:{id}",
            //    FullName = iv.Booking.Customer.FullName,
            //    BookingId = id
            //};
            //redirectUrl = _vnPayService.CreatePaymentUrl(HttpContext, vnPayModel, "");
            //return Redirect(redirectUrl);
            var order = new OrderInfoModel
            {
                FullName = iv.Booking.Customer.FullName,
                Amount = Math.Round(iv.Amount - iv.Booking.Deposit, 0),
                OrderId = id.ToString(),
                OrderInfo = $"Thanh toan hoa don:{id}",
            };
            var response = await _momoService.CreatePaymentAsync(order, "invoice");
            return Redirect(response.PayUrl);
        }
        /////////Sesstion
        private async Task<InvoiceViewModel> GetInvoiceFromSession(int id)
        {
            var InvoiceJson = HttpContext.Session.GetString("CurrentInvoice");
            InvoiceViewModel model;
            var inv = _context.Invoices.Find(id);
            if (string.IsNullOrEmpty(InvoiceJson))
            {
                model = new InvoiceViewModel
                {
                    Id = id,
                    CreateDate = inv.CreateDate,
                    Status = inv.Status
                };
                model.Booking = _context.Bookings.Find(inv.BookingId);
                model.Booking.Customer = _context.Users.Find(model.Booking.CusID);
                model.Receptionist = await _userManager.GetUserAsync(HttpContext.User);
                var bkdt = _context.BookingDetails.Where(i => i.BookingId == model.Booking.Id);
                model.Rooms = new List<Room>();
                foreach (var i in bkdt)
                {
                    var room = _context.Rooms.Find(i.RoomId);
                    //var rmd = new RoomViewModel
                    //{
                    //    Id=room.Id,
                    //    Type=room.Type,
                    //    Description=room.Description,
                    //    Price=room.Price,
                    //    FloorNumber=room.FloorNumber,
                    //    DAP=room.DAP,
                    //    MAP=room.MAP,
                    //};
                    model.Rooms.Add(room);

                }
                return model;
            }
            model = JsonConvert.DeserializeObject<InvoiceViewModel>(InvoiceJson);
            if (model.Id != id)
            {
                model = new InvoiceViewModel
                {
                    Id = id,
                    CreateDate = inv.CreateDate,
                    Status = inv.Status
                };
                model.Booking = _context.Bookings.Find(inv.BookingId);
                model.Booking.Customer = _context.Users.Find(model.Booking.CusID);
                model.Receptionist = await _userManager.GetUserAsync(HttpContext.User);
                var bkdt = _context.BookingDetails.Where(i => i.BookingId == model.Booking.Id);
                model.Rooms = new List<Room>();
                foreach (var i in bkdt)
                {
                    var room = _context.Rooms.Find(i.RoomId);
                    var rmd = new RoomViewModel
                    {
                        Id = room.Id,
                        Type = room.Type,
                        Description = room.Description,
                        Price = room.Price,
                        FloorNumber = room.FloorNumber,
                        DAP = room.DAP,
                        MAP = room.MAP,
                    };
                    model.Rooms.Add(room);
                }
            }
            return model;
        }

        //Lưu thông tin đặt phong vào session
        private void SaveInvoiceToSession(InvoiceViewModel invoice)
        {
            var InvoiceJson = JsonConvert.SerializeObject(invoice);
            HttpContext.Session.SetString("CurrentInvoice", InvoiceJson);
        }
        //Session////////
        public async Task<IActionResult> PaymentCallBack()
        {

            // Lấy thông tin từ query string của VnPay để xác thực và cập nhật trạng thái đơn hàng
            //var response = _vnPayService.PaymentExecute(Request.Query);
            //int id = int.Parse(response.OrderDescription.Split(':')[1]);
            //if (response == null || response.VnPayResponseCode != "00")
            //{
            //    //thanh toán thất bại
            //    //int idiv = (int)TempData["ivid"];
            //    return RedirectToAction("invoicedetail", new { id = id, message = "Thanh toán thất bại" });
            //}
            var response = _momoService.PaymentExecuteAsync(HttpContext.Request.Query);
            if (response == null)
            {
                //thanh toán thất bại
                return RedirectToAction("PaymentFail");
            }
            //lấy thông tin đặt phòng từ viewmodel
            string[] rs = response.OrderInfo.Split(':');
            int id = int.Parse(rs[rs.Length-1]);
            var iv = _context.Invoices.Find(id);
            iv.Status = 3;
            iv.PaymentDate = DateTime.Now;
            iv.PayMethod = 1;
            _context.Invoices.Update(iv);
            _context.SaveChanges();
            //xóa viewmodel
            return RedirectToAction("invoicedetail", new { id = id, message = "Thanh toán thành công" });
        }

        public IActionResult Print(int id)
        {
            var invoice = _context.Invoices
                .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
                .Include(i => i.Receptionist)
                .FirstOrDefault(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            var invoiceDetails = _context.InvoiceDetails
                .Where(d => d.InvoiceId == id)
                .Include(d => d.Room)
                .ToList();
            int p = invoiceDetails.Sum(i => i.SubFee);
            double sf = 0;
            foreach (var i in invoiceDetails)
            {
                sf += i.Price * i.SubFee / 100;
            }
            var viewModel = new InvoiceView
            {
                Invoice = invoice,
                InvoiceDetail = invoiceDetails,
                SubFee = sf
            };
            viewModel.Final = viewModel.Invoice.Amount - viewModel.Invoice.Booking.Deposit + invoiceDetails.Sum(d => d.Price * d.SubFee / 100);
            return View("Print", viewModel);
        }

        public IActionResult InvoiceList(int? type)
        {
            int status = type ?? 0;
            var list = _context.Invoices
                .Include(i => i.Booking)
                .ThenInclude(x => x.Customer)
                .ToList();
            if (status != 0)
            {
                list = list.Where(i => i.Status == status).ToList();
            }
            var models = new List<InvoiceView>();
            foreach (var item in list)
            {
                models.Add(
                    new InvoiceView
                    {
                        Invoice = item,
                    });
            }
            return View(models);
        }
    }
}
