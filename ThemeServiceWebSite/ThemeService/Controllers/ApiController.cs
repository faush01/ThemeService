using Microsoft.AspNetCore.Mvc;
using ThemeService.Data;
using ThemeService.Models;

namespace ThemeService.Controllers
{
    public class ApiController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IConfiguration _config;

        public ApiController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _config = configuration;
        }

        public IActionResult Index()
        {
            Store store = new Store(_config);

            ThemeQueryOptions options = new ThemeQueryOptions();

            options.CpData = Request.Query["data"].ToString().ToLower().Trim() == "true";

            // ids
            string ids = Request.Query["id"].ToString();
            foreach (string id in ids.Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                if(int.TryParse(id, out int id_value))
                {
                    options.Id.Add(id_value);
                }
            }

            // Imdb
            string imdb = Request.Query["imdb"].ToString();
            foreach (string id in imdb.Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                options.Imdb.Add(id.Trim());
            }

            List<ThemeData> theme_list = store.GetThemeDataList(options);

            return Json(theme_list);
        }

        public IActionResult Info()
        {

            return View();
        }
    }
}
