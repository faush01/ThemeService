using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
            string theme_source = config.Get("theme_server");
            if(theme_source.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("Loading theme list from API : " + theme_source);
                return await LoadListFromApi();
            }
            else
            {
                Console.WriteLine("Loading theme list from file : " + theme_source);
                return LoadListFromFile(theme_source);
            }
        }

        private async Task<List<ThemeInfo>> LoadListFromApi()
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
            string theme_source = config.Get("theme_server");
            if (theme_source.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("Loading theme data from API : " + theme_source);
                return await GetThemeDataFromApi(ids);
            }
            else
            {
                Console.WriteLine("Loading theme data from file : " + theme_source);
                return LoadListFromFile(theme_source);
            }
        }

        public async Task<List<ThemeInfo>> GetThemeDataFromApi(HashSet<int?> ids)
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

        private List<ThemeInfo> LoadListFromFile(string file_name)
        {
            List<ThemeInfo> themes = new List<ThemeInfo>();
            using(FileStream fs = new FileStream(file_name, FileMode.Open))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    string line = sr.ReadLine();
                    int line_id = 0;
                    while(line != null)
                    {
                        string[] token = line.Split("\t");

                        ThemeInfo info = new ThemeInfo();
                        info.id = line_id;
                        info.imdb = token[0];
                        info.themoviedb = token[1];
                        info.thetvdb = token[2];
                        info.season = int.Parse(token[3]);
                        info.episode = int.Parse(token[4]);
                        info.extract_length = int.Parse(token[5]);
                        info.description = token[6];
                        info.theme_cp_data = token[7];

                        themes.Add(info);
                        line_id++;
                        line = sr.ReadLine();
                    }
                }
            }

            return themes;
        }
    }
}
