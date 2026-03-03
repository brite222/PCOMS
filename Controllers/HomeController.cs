using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PCOMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name!);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    if (roles.Contains("Client"))
                    {
                        return RedirectToAction("Dashboard", "ClientPortal");
                    }
                    else if (roles.Contains("Admin") || roles.Contains("ProjectManager"))
                    {
                        return RedirectToAction("Index", "Clients");
                    }
                    else if (roles.Contains("Developer"))
                    {
                        return RedirectToAction("MyProjects", "Projects");
                    }
                }
            }

            return RedirectToAction("Login", "Account");
        }
    }
}