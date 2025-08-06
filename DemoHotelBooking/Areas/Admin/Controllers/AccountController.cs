using DemoHotelBooking.Models;
using DemoHotelBooking.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DemoHotelBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> CustomerList()
        {
            var users = _userManager.Users.ToList();
            var customers = new List<AppUser>();
            foreach (var u in users)
            {
                if (await _userManager.IsInRoleAsync(u, "Customer"))
                {
                    customers.Add(u);
                }
            }
            return View(customers);
        }

        public async Task<IActionResult> StaffList()
        {
            var users = _userManager.Users.ToList();
            var staff = new List<AppUser>();
            foreach (var u in users)
            {
                if (await _userManager.IsInRoleAsync(u, "Admin") || await _userManager.IsInRoleAsync(u, "Staff"))
                {
                    staff.Add(u);
                }
            }
            return View(staff);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    FullName = model.FullName,
                    IsRegisted = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Assign default role as Customer
                    await _userManager.AddToRoleAsync(user, "Customer");
                    TempData["Success"] = "Tạo tài khoản thành công";
                    return RedirectToAction("CustomerList");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            return View(user);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Xóa tài khoản thành công";
            }
            else
            {
                TempData["Error"] = "Có lỗi khi xóa tài khoản";
            }

            return RedirectToAction("CustomerList");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy user" });

            // Remove all current roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add new role
            var result = await _userManager.AddToRoleAsync(user, role);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Đã cập nhật role thành công" });
            }

            return Json(new { success = false, message = "Có lỗi khi cập nhật role" });
        }
    }
}
