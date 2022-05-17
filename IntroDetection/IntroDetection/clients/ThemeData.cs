using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IntroDetection
{
    public class ThemeData
    {
        private HttpClient http_client = null;
        private Config config = null;
        
        public ThemeData(HttpClient cli, Config con)
        {
            http_client = cli;
            config = con;
        }

        public async Task<List<ThemeInfo>> GetThemeList()
        {
            string themes_url = "/Api";//?data=true";

            string request_url = config.Get("theme_server") + themes_url;
            Console.WriteLine("Request : " + request_url);
            HttpResponseMessage response = await http_client.GetAsync(request_url);

            int rc = (int)response.StatusCode;
            List<ThemeInfo> theme_data = null;
            if (rc == 200)
            {
                string response_body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Read Data : " + response_body.Length);
                theme_data = JsonConvert.DeserializeObject<List<ThemeInfo>>(response_body);
                Console.WriteLine("Themes count : " + theme_data.Count);
            }
            else
            {
                Console.WriteLine("Invalid Responce : " + rc);
            }

            return theme_data;
        }

        public async Task<List<ThemeInfo>> GetThemeData(HashSet<int?> ids)
        {
            string themes_url = "/Api?data=true&id=" + String.Join(",", ids);

            string request_url = config.Get("theme_server") + themes_url;
            Console.WriteLine("Request : " + request_url);
            HttpResponseMessage response = await http_client.GetAsync(request_url);

            int rc = (int)response.StatusCode;
            List<ThemeInfo> theme_data = null;
            if (rc == 200)
            {
                string response_body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Read Data : " + response_body.Length);
                theme_data = JsonConvert.DeserializeObject<List<ThemeInfo>>(response_body);
                Console.WriteLine("Themes count : " + theme_data.Count);
            }
            else
            {
                Console.WriteLine("Invalid Responce : " + rc);
            }

            return theme_data;
        }
    }
}
