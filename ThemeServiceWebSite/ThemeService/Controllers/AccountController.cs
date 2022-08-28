using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ThemeService.Controllers
{
    public class AccountController : Controller
    {

        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            if(string.IsNullOrEmpty(returnUrl) && Request.Headers.ContainsKey("Referer"))
            {
                returnUrl = Request.Headers["Referer"];
            }
            if(string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "/";
            }
            return Challenge(new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        public IActionResult Logout()
        {
            string returnUrl = "/";
            return SignOut(new AuthenticationProperties { RedirectUri = returnUrl },
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

    }
}
