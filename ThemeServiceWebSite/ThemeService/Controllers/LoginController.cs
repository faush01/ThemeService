using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using ThemeService.Data;
using ThemeService.Models;

namespace ThemeService.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IConfiguration _config;

        public LoginController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _config = configuration;
        }

        public IActionResult Index()
        {
            string message = HttpContext.Session.GetString("loggin_failed");
            HttpContext.Session.Remove("loggin_failed");
            ViewData["message"] = message;
            return View();
        }

        private string HashPassword(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(password);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToHexString(hashBytes);
            }
        }

        public IActionResult Process(IFormCollection form_data)
        {
            string username = form_data["username"];
            string password = form_data["password"];

            Store store = new Store(_config);

            UserInfo user = store.GetUserInfo(username);

            if (user != null)
            {
                string pwd_hash = HashPassword(password);
                
                if(pwd_hash != user.password)
                {
                    HttpContext.Session.SetString("loggin_failed", "Username Password does not match");
                    return RedirectToAction("Index", "Login");
                }

                // set last logged in date
                store.UpdateLastLoginDate(user);

                HttpContext.Session.SetString("user_info", JsonConvert.SerializeObject(user));
                string referer = HttpContext.Session.GetString("login_referer");
                if (!string.IsNullOrEmpty(referer))
                {
                    HttpContext.Session.Remove("login_referer");
                    return Redirect(referer);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                HttpContext.Session.SetString("loggin_failed", "Username not found");
                return RedirectToAction("Index", "Login");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("user_info");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult CreateUser()
        {
            return View();
        }

        private bool CheckUserName(string username)
        {
            string acceptable = "abcdefghijklmnopqrstuvwxyz0123456789_"; ;
            foreach(char c in username)
            {
                if(!acceptable.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }

        public IActionResult Create(IFormCollection form_data)
        {
            Store store = new Store(_config);

            string username = form_data["username"];
            string password01 = form_data["password01"];
            string password02 = form_data["password02"];

            List<string> messages = new List<string>();

            // check username
            if(string.IsNullOrEmpty(username))
            {
                messages.Add("Username is empty");
            }

            if(!CheckUserName(username))
            {
                messages.Add("Username not valid (usernames can only contain lowercase letters, numbers and _)");
            }

            if(string.IsNullOrEmpty(password01) || string.IsNullOrEmpty(password02))
            {
                messages.Add("Passwords can not be empty");
            }

            if(password01 != password02)
            {
                messages.Add("Passwords do not match");
            }

            if (messages.Count == 0)
            {
                UserInfo user_info = store.GetUserInfo(username);
                if (user_info != null)
                {
                    messages.Add("User already exists");
                }
            }

            if (messages.Count == 0) // try to create
            {
                // do creation checking
                UserInfo new_user = new UserInfo();
                new_user.username = username;
                new_user.password = HashPassword(password01);
                store.AddUser(new_user);
                messages.Add("User " + username + " created");
            }

            ViewData["messages"] = messages;

            return View();
        }
    }
}
