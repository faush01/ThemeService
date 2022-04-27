using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Cryptography;
using ThemeService.Data;
using ThemeService.Models;

namespace ThemeService.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _config = configuration;
        }

        public IActionResult Index()
        {
            UserInfo user_info = Utils.SessionData.GetActiveUser(HttpContext);
            ViewData["user_info"] = user_info;

            Store store = new Store(_config);
            List<ThemeData> theme_list = store.GetThemeDataList();
            ViewData["theme_list"] = theme_list;

            return View();
        }

        public IActionResult AddTheme()
        {
            UserInfo user_info = Utils.SessionData.GetActiveUser(HttpContext);
            if (user_info == null)
            {
                string referer = Url.Action("AddTheme", "Home");
                HttpContext.Session.SetString("login_referer", referer);
                return RedirectToAction("Index", "Login");
            }
            ViewData["user_info"] = user_info;

            return View();
        }

        public IActionResult ShowItemInfo(int id)
		{
            UserInfo user_info = Utils.SessionData.GetActiveUser(HttpContext);
            ViewData["user_info"] = user_info;

            Store store = new Store(_config);
            ThemeData theme_data = store.GetThemeData(id);
            ViewData["theme_data"] = theme_data;
            return View();
        }

        public IActionResult DeleteTheme(int id)
		{
            UserInfo user_info = Utils.SessionData.GetActiveUser(HttpContext);
            if (user_info == null)
            {
                string referer = Url.Action("AddTheme", "Home");
                HttpContext.Session.SetString("login_referer", referer);
                return RedirectToAction("Index", "Login");
            }

            Store store = new Store(_config);
            ThemeData theme = store.GetThemeData(id);
            if(theme != null)
			{
                if(theme.added_by == user_info.username)
                {
                    store.DeleteTheme(id);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult AddNewTheme(IFormCollection form_data)
		{
            UserInfo user_info = Utils.SessionData.GetActiveUser(HttpContext);
            if (user_info == null)
            {
                string referer = Url.Action("AddTheme", "Home");
                HttpContext.Session.SetString("login_referer", referer);
                return RedirectToAction("Index", "Login");
            }
            ViewData["user_info"] = user_info;

            ThemeData theme = new ThemeData();

            theme.imdb = form_data["imbd"];
            theme.themoviedb = form_data["themoviedb"];
            theme.thetvdb = form_data["thetvdb"];

            theme.imdb = theme.imdb.Trim();
            theme.themoviedb = theme.themoviedb.Trim();
            theme.thetvdb = theme.thetvdb.Trim();

            int season = -1;
            try
            {
                season = int.Parse(form_data["season"]);
            }
            catch (Exception ex) { }
            theme.season = season;
            int episode = -1;
            try
            {
                episode = int.Parse(form_data["episode"]);
            }
            catch(Exception ex) { }
            theme.episode = episode;

            theme.description = form_data["description"];
            theme.added_by = user_info.username;

            byte[] cp_bytes = null;
            if (form_data.Files.Count > 0)
			{
                string filename = form_data.Files[0].FileName;
                MemoryStream stream = new MemoryStream();
                form_data.Files[0].CopyTo(stream);
                cp_bytes = stream.ToArray();
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }

            if (cp_bytes != null)
			{
                theme.theme_cp_data_size = cp_bytes.Length;
                using (MD5 md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(cp_bytes);
                    theme.theme_cp_data_md5 = Convert.ToHexString(hashBytes);
                }
                theme.theme_cp_data = Convert.ToBase64String(cp_bytes);
            }

            // do add theme
            Store store = new Store(_config);
            int id = store.SaveThemeData(theme);

            return RedirectToAction("ShowItemInfo", "Home", new { id = id });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}