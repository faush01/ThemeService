using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroDetection
{
    public class ActionStore
    {
        Config config;

        public ActionStore(Config conf)
        {
            config = conf;
        }

        public EmbyItem LoadEpisodeData(EmbyItem item)
        {
            string item_path = GetItemPath(item, false);

            if(item_path == null)
            {
                return null;
            }

            FileInfo fileInfo = new FileInfo(item_path);
            if (!fileInfo.Exists)
            {
                return null;
            }

            string item_data = File.ReadAllText(item_path);
            EmbyItem new_emby_item = JsonConvert.DeserializeObject<EmbyItem>(item_data);

            return new_emby_item;
        }

        public void SaveEpisodeData(EmbyItem item)
        {
            string item_path = GetItemPath(item, true);
            string item_data = JsonConvert.SerializeObject(item, Formatting.Indented);
            File.WriteAllText(item_path, item_data);
        }

        private string GetItemPath(EmbyItem item, bool create)
        {
            string store_base = config.Get("store_path");
            string item_path = Path.Combine(store_base, string.Format(@"{0:D3}", item.SeriesId)); 
            DirectoryInfo item_path_info = new DirectoryInfo(item_path);
            if(!item_path_info.Exists)
            {
                if (create)
                {
                    item_path_info.Create();
                }
                else
                {
                    return null;
                }
            }

            string fmt = @"{0:D3}-{1:D3}.json";
            string episode_name = string.Format(fmt, item.ParentIndexNumber, item.IndexNumber);
            string item_full_path = Path.Combine(item_path, episode_name);

            Console.WriteLine("Item Store Path : " + item_full_path);
            return item_full_path;
        }

    }
}
