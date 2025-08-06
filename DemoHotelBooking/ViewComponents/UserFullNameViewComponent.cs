using DemoHotelBooking.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DemoHotelBooking.ViewComponents
{
    public class UserFullNameViewComponent : ViewComponent
    {
        private readonly UserManager<AppUser> _userManager;

        public UserFullNameViewComponent(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            return View(user);
        }
    }
}
