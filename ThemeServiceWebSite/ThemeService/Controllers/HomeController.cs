using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
                    foreach(string token in tokens)
                    {
                        if(!string.IsNullOrEmpty(token))
                        {
                            items.Add(token.Trim());
                        }
                    }
                }
            }
        }

        private string GetJsonString(Dictionary<string, object> data)
        {
            List<Type> num_types = new List<Type>() { typeof(int), typeof(long), typeof(double) }; 
            StringBuilder sb = new StringBuilder(4096);
            int i = 0;
            sb.Append("{\r\n");
            foreach (var x in data)
            {
                string value = "";
                if(x.Value == null)
                {
                    value = "null";
                }
                else if (num_types.Contains(x.Value.GetType()))
                {
                    value = x.Value.ToString();
                }
                else
                {
                    value = "\"" + x.Value.ToString() + "\"";
                }

                sb.Append("\t\"" + x.Key + "\":" + value);
                if (i < data.Count - 1)
                {
                    sb.Append(",");
                }
                sb.Append("\r\n");
                i++;
            }
            sb.Append("}");

            return sb.ToString();
        }

        private string GetItemJson(ThemeData item)
        {
            Dictionary<string, object> theme_data = new Dictionary<string, object>();

            theme_data.Add("tvdb", item.thetvdb);
            theme_data.Add("imdb", item.imdb);
            theme_data.Add("tmdb", item.themoviedb);

            theme_data.Add("series", item.series_name);
            theme_data.Add("season", item.season);
            theme_data.Add("episode", item.episode);

            theme_data.Add("extract", item.extract_length);

            theme_data.Add("cp_data", item.theme_cp_data);
            theme_data.Add("cp_data_length", item.theme_cp_data_size);
            theme_data.Add("cp_data_md5", item.theme_cp_data_md5);

            return GetJsonString(theme_data);
        }

        private byte[] GetItemsZip(List<ThemeData> items)
        {
            using (MemoryStream zip_ms = new MemoryStream())
            {
                using (ZipArchive archive = new ZipArchive(zip_ms, ZipArchiveMode.Create))
                {
                    foreach(ThemeData item in items)
                    {
                        ZipArchiveEntry readmeEntry = archive.CreateEntry(item.theme_cp_data_md5 + ".json");
                        using (StreamWriter writer = new StreamWriter(readmeEntry.Open()))
                        {
                            writer.WriteLine(GetItemJson(item));
                        }
                    }
                }
                return zip_ms.ToArray();
            }
        }

        public IActionResult RemoveVerification(int item_id)
        {
            GitHubUser user_info = Utils.UserDetails.GetUserInfo(User);
            ViewData["user_info"] = user_info;

            if (user_info.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "Account");
            }

            Store store = new Store(_config);
            store.RemoveVerification(item_id, user_info.LoginId);

            return RedirectToAction("ShowItemInfo", "Home", new { id = item_id });
        }

        public IActionResult AddThemeVerification(int item_id)
        {
            GitHubUser user_info = Utils.UserDetails.GetUserInfo(User);
            ViewData["user_info"] = user_info;

            if (user_info.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "Account");
            }

            Store store = new Store(_config);

            ThemeQueryOptions options = new ThemeQueryOptions();
            options.CpData = false;
            options.Id.Add(item_id.ToString());

            List<ThemeData> theme_list = store.GetThemeDataList(options);
            if(theme_list.Count > 0)
            {
                ThemeData item = theme_list[0];
                if (item.added_by != user_info.LoginId)
                {
                    store.AddVerification(item_id, user_info.LoginId);
                }
            }

            return RedirectToAction("ShowItemInfo", "Home", new { id = item_id });
        }

        public IActionResult DownloadItemInfo(int id)
        {
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

            string item_json = GetItemJson(theme_data);
            byte[] bytes = Encoding.UTF8.GetBytes(item_json);
            MemoryStream ms = new MemoryStream(bytes);
            return File(ms, "application/json");//, "chromaprint_intro_items.zip");
        }

        public IActionResult Search(
            string name, 
            string imdb, 
            string tvdb, 
            string tmdb,
            string md5,
            string added,
            string limit,
            string orderby,
            string orderdir,
            string verified,
            string download)
        {
            GitHubUser user_info = Utils.UserDetails.GetUserInfo(User);
            ViewData["user_info"] = user_info;

            bool do_down = !string.IsNullOrEmpty(download) && download.Equals("true", StringComparison.InvariantCultureIgnoreCase);

            Store store = new Store(_config);

            ThemeQueryOptions options = new ThemeQueryOptions();
            options.CpData = do_down;

            options.SerieName = name;

            AddSearchItems(imdb, options.Imdb);
            AddSearchItems(tvdb, options.TheTvDb);
            AddSearchItems(tmdb, options.ThemovieDb);
            AddSearchItems(md5, options.Md5);
            AddSearchItems(added, options.AddedBy);

            options.OrderBy = OrderBy.added_date;
            options.OrderByDirection = OrderByDirection.DESC;

            if(!string.IsNullOrEmpty(limit))
            {
                int int_val = 0;
                int.TryParse(limit, out int_val);
                options.Limit = int_val;
            }

            if(do_down && string.IsNullOrEmpty(limit)) // if doing download and no limit then return all items
            {
                options.Limit = 0;
            }

            if(!string.IsNullOrEmpty(orderby))
            {
                int int_val = 0;
                int.TryParse(orderby, out int_val);
                options.OrderBy = (OrderBy)int_val;
            }

            if (!string.IsNullOrEmpty(orderdir))
            {
                int int_val = 0;
                int.TryParse(orderdir, out int_val);
                options.OrderByDirection = (OrderByDirection)int_val;
            }

            bool only_verified = !string.IsNullOrEmpty(verified) && verified.Equals("true", StringComparison.InvariantCultureIgnoreCase);
            if(only_verified)
            {
                options.verify_min = 1;
            }
            
            List<ThemeData> theme_list = store.GetThemeDataList(options);

            // download or view
            if(do_down)
            {
                byte[] zip_bytes = GetItemsZip(theme_list);
                MemoryStream ms = new MemoryStream(zip_bytes);
                return File(ms, "application/octet-stream", "chromaprint_intro_items.zip");
            }
            else 
            {
                ViewData["series_name"] = name;
                ViewData["imdb_list"] = imdb;
                ViewData["tvdb_list"] = tvdb;
                ViewData["tmdb_list"] = tmdb;
                ViewData["md5"] = md5;
                ViewData["added_by"] = added;
                ViewData["limit"] = limit;
                ViewData["verified"] = verified;
                ViewData["orderby"] = orderby;
                ViewData["orderdir"] = orderdir;

                ViewData["theme_list"] = theme_list;

                return View();
            }
        }

        public IActionResult Index()
        {
            GitHubUser user_info = Utils.UserDetails.GetUserInfo(User);
            ViewData["user_info"] = user_info;

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

            if(theme.extract_length == null)
            {
                messages.Add("Extraction length is a required field");
            }

            if(string.IsNullOrEmpty(theme.imdb))
            {
                messages.Add("Imdb ID is a required field.");
            }

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

            // verify theme cp length vs extraction length
            int extract_len = theme.extract_length ?? 0;
            int cp_data_size = theme.theme_cp_data_size ?? 0;
            // 32 bytes per sec based on testing, extract need to be at least 1.5 times longer then the actual intro
            double cp_len_min_double = ((cp_data_size / 32.0) / 60.0) * 1.5;
            int cp_len_min = (int)Math.Ceiling(cp_len_min_double);
            if (cp_len_min >= extract_len)
            {
                messages.Add("Extraction length is not valid, it should be at least the intro length + the intro start offset.");
            }

            if (messages.Count == 0)
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