using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroDetection
{
    public class EmbyData
    {
        private HttpClient? http_client = null;
        private Config? config = null;

        public EmbyData(HttpClient cli, Config con)
        {
            http_client = cli;
            config = con;
        }

        public async Task<EmbyResult?> GetSeriesList()
        {
            string series_url = "/emby/Items" +
                "?IncludeItemTypes=Series" +
                "&Recursive=true" +
                "&Fields=ProviderIds" +
                "&EnableImages=false";
            string api_key = "&api_key=" + config.Get("emby_api_key");
            string request_url = config.Get("emby_server") + series_url + api_key;
            Console.WriteLine("Request : " + request_url);
            HttpResponseMessage response = await http_client.GetAsync(request_url);

            int rc = (int)response.StatusCode;
            EmbyResult? series_data = null;
            if (rc == 200)
            {
                string response_body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Read Data : " + response_body.Length);
                series_data = JsonConvert.DeserializeObject<EmbyResult>(response_body);
                Console.WriteLine("Series count : " + series_data.Items.Count);

                //Stream contentStream = await response.Content.ReadAsStreamAsync();
                //StreamReader streamReader = new StreamReader(contentStream);
                //JsonTextReader jsonReader = new JsonTextReader(streamReader);
                //JsonSerializer serializer = new JsonSerializer();
                //EmbyClient.Result? json_data = serializer.Deserialize<EmbyClient.Result>(jsonReader);
                //Console.WriteLine(json_data.TotalRecordCount);
            }
            else
            {
                Console.WriteLine("Invalid Responce : " + rc);
            }

            return series_data;
        }

        public async Task<EmbyResult?> GetEpisodeList(long series_id)
        {
            string episode_url = "/emby/Items" +
                "?IncludeItemTypes=Episode" +
                "&Recursive=true" +
                "&Fields=Path,Chapters" +
                "&EnableImages=false" +
                "&ParentId=" + series_id;

            string api_key = "&api_key=" + config.Get("emby_api_key");
            string request_url = config.Get("emby_server") + episode_url + api_key;
            Console.WriteLine("Request : " + request_url);
            HttpResponseMessage response = await http_client.GetAsync(request_url);

            int rc = (int)response.StatusCode;
            EmbyResult? eposide_data = null;
            if (rc == 200)
            {
                string response_body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Read Data : " + response_body.Length);
                eposide_data = JsonConvert.DeserializeObject<EmbyResult>(response_body);
                Console.WriteLine("Episode count : " + eposide_data.Items.Count);
            }
            else
            {
                Console.WriteLine("Invalid Responce : " + rc);
            }

            return eposide_data;
        }
    }
}
