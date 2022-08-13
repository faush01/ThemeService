using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
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

        private void AddSearchItems(string input, List<string> items)
        {
            if (!string.IsNullOrEmpty(input))
            {
                input = input.ToLower().Trim();
                string[] tokens = input.Split(",", StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length > 0)
                {
                    items.AddRange(tokens);
                }
            }
        }

        public IActionResult Search(string name, string imdb, string tvdb, string tmdb)
        {
            GitHubUser user_info = Utils.UserDetails.GetUserInfo(User);
            ViewData["user_info"] = user_info;

            Store store = new Store(_config);

            ThemeQueryOptions options = new ThemeQueryOptions();
            options.CpData = false;

            options.SerieName = name;

            AddSearchItems(imdb, options.Imdb);
            AddSearchItems(tvdb, options.TheTvDb);
            AddSearchItems(tmdb, options.ThemovieDb);

            List<ThemeData> theme_list = store.GetThemeDataList(options);

            ViewData["theme_list"] = theme_list;

            return View();
        }

        public IActionResult Index()
        {
            GitHubUser user_info = Utils.UserDetails.GetUserInfo(User);
            ViewData["user_info"] = user_info;

            Store store = new Store(_config);

            ThemeQueryOptions options = new ThemeQueryOptions();
            options.CpData = false;

            List<ThemeData> theme_list = store.GetThemeDataList(options);

            ViewData["theme_list"] = theme_list;

            return View();
        }

        public IActionResult AddTheme()
        {
            GitHubUser user_info = Utils.UserDetails.GetUserInfo(User);
            if (user_info.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("AddTheme", "Home") });
            }
            ViewData["user_info"] = user_info;

            return View();
        }

        public IActionResult ShowItemInfo(int id)
		{
            GitHubUser user_info = Utils.UserDetails.GetUserInfo(User);
            ViewData["user_info"] = user_info;

            ThemeData theme_data = new ThemeData();
            if (id != 0)
            {
                Store store = new Store(_config);

                ThemeQueryOptions options = new ThemeQueryOptions();
                options.CpData = true;
                options.Id.Add(id.ToString());

                List<ThemeData> theme_list = store.GetThemeDataList(options);
                if (theme_list.Count > 0)
                {
                    theme_data = theme_list[0];
                }
            }

            ViewData["theme_data"] = theme_data;
            return View();
        }

        public IActionResult DeleteTheme(int id)
		{
            GitHubUser user_info = Utils.UserDetails.GetUserInfo(User);
            if (user_info.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "Account");
            }

            Store store = new Store(_config);

            ThemeQueryOptions options = new ThemeQueryOptions();
            options.CpData = false;
            options.Id.Add(id.ToString());
            List<ThemeData> theme_list = store.GetThemeDataList(options);

            ThemeData theme = null;
            if(theme_list.Count > 0)
            {
                theme = theme_list[0];
            }

            if(theme != null)
			{
                if(theme.added_by == user_info.LoginId)
                {
                    store.DeleteTheme(id);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult AddNewTheme(IFormCollection form_data)
		{
            GitHubUser user_info = Utils.UserDetails.GetUserInfo(User);
            if (user_info.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewData["user_info"] = user_info;

            List<string> messages = new List<string>();

            ThemeData theme = new ThemeData();

            theme.imdb = TrimString(form_data["imdb"]);
            theme.themoviedb = TrimString(form_data["themoviedb"]);
            theme.thetvdb = TrimString(form_data["thetvdb"]);
            theme.season = GetInt(form_data["season"]);
            theme.episode = GetInt(form_data["episode"]);
            theme.extract_length = GetInt(form_data["extract_length"]);
            theme.series_name = TrimString(form_data["series_name"]);

            theme.series_name = TrimString(form_data["series_name"]);
            theme.added_by = user_info.LoginId;

            string filename = null;
            byte[] cp_bytes = null;
            if (form_data.Files.Count > 0)
			{
                filename = form_data.Files[0].FileName;
                MemoryStream stream = new MemoryStream();
                form_data.Files[0].CopyTo(stream);
                cp_bytes = stream.ToArray();
            }
            else
            {
                messages.Add("No file selected");
            }

            if (cp_bytes != null && cp_bytes.Length != 0)
			{
                if(filename.ToLower().EndsWith(".json"))
                {
                    string cp_base64_string = null;
                    string cp_md5_string = null;
                    string cp_json_string = System.Text.Encoding.UTF8.GetString(cp_bytes);
                    try
                    {
                        using (JsonDocument cp_info_doc = JsonDocument.Parse(cp_json_string))
                        {
                            var root = cp_info_doc.RootElement;
                            cp_base64_string = root.GetProperty("cp_data").GetString();
                            cp_md5_string = root.GetProperty("cp_data_md5").GetString();
                        }
                    }
                    catch(Exception e)
                    {
                        messages.Add("Error parsing JSON file : " + e.Message);
                    }

                    if(cp_base64_string != null && cp_md5_string != null)
                    {
                        try
                        {
                            byte[] cp_bytes_extracted = Convert.FromBase64String(cp_base64_string);
                            theme.theme_cp_data_size = cp_bytes_extracted.Length;
                            using (MD5 md5 = MD5.Create())
                            {
                                byte[] hashBytes = md5.ComputeHash(cp_bytes_extracted);
                                theme.theme_cp_data_md5 = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
                            }
                            theme.theme_cp_data = Convert.ToBase64String(cp_bytes_extracted);

                            if (cp_md5_string.Equals(theme.theme_cp_data_md5, StringComparison.InvariantCultureIgnoreCase) == false)
                            {
                                messages.Add("MD5 of data in JSON file does not match");
                            }
                        }
                        catch(Exception e)
                        {
                            messages.Add("Error processing JSON file data : " + e.Message);
                        }
                    }
                    else
                    {
                        messages.Add("Error extracting base64 data from JSON file");
                    }
                }
                else if (filename.ToLower().EndsWith(".bin"))
                {
                    theme.theme_cp_data_size = cp_bytes.Length;
                    using (MD5 md5 = MD5.Create())
                    {
                        byte[] hashBytes = md5.ComputeHash(cp_bytes);
                        theme.theme_cp_data_md5 = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
                    }
                    theme.theme_cp_data = Convert.ToBase64String(cp_bytes);
                }
                else
                {
                    // file type not recognised
                    messages.Add("File type not recognised");
                }
            }
            else
            {
                messages.Add("File has no usable data");
            }

            if(messages.Count == 0)
            {
                // add theme
                Store store = new Store(_config);
                int id = store.SaveThemeData(theme);
                return RedirectToAction("ShowItemInfo", "Home", new { id = id });
            }
            else
            {
                ViewData["messages"] = messages;
                return View();
            }
        }

        private int? GetInt(string input)
        {
            int? val = null;
            if (input != null)
            {
                int parsed_val = 0;
                if(int.TryParse(input, out parsed_val))
                {
                    val = parsed_val;
                }
            }
            return val;
        }

        private string TrimString(string val)
        {
            if(val == null)
            {
                return null;
            }
            string trimmed = val.Trim();
            if (trimmed == "")
            {
                return null;
            }
            return trimmed;
        }

        public IActionResult UpdateTheme(IFormCollection form_data)
		{
            GitHubUser user_info = Utils.UserDetails.GetUserInfo(User);
            if (user_info.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewData["user_info"] = user_info;

            int? theme_id = GetInt(form_data["id"]);
            if (theme_id == null)
            {
                return RedirectToAction("Index", "Home");
            }

            Store store = new Store(_config);
            ThemeQueryOptions options = new ThemeQueryOptions();
            options.Id.Add(theme_id.Value.ToString());
            List<ThemeData> theme_list = store.GetThemeDataList(options);

            if(theme_list.Count == 0)
			{
                return RedirectToAction("Index", "Home");
            }

            ThemeData theme = theme_list[0];

            if(theme.added_by != user_info.LoginId)
			{
                return RedirectToAction("ShowItemInfo", "Home", new { id = theme.id });
            }

            theme.imdb = TrimString(form_data["imdb"]);
            theme.themoviedb = TrimString(form_data["themoviedb"]);
            theme.thetvdb = TrimString(form_data["thetvdb"]);
            theme.season = GetInt(form_data["season"]);
            theme.episode = GetInt(form_data["episode"]);
            theme.extract_length = GetInt(form_data["extract_length"]);
            theme.series_name = TrimString(form_data["series_name"]);

            store.UpdateTheme(theme);

            return RedirectToAction("ShowItemInfo", "Home", new { id = theme.id });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}