
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace IntroDetection
{
    public class ActionProcess
    {
        private Config config;
        private HttpClient http_client;

        public ActionProcess(HttpClient cli, Config con)
        {
            config = con;
            http_client = cli;
        }

        public async Task ProcessEpisodes()
        {
            List<EmbyItem> matched_episodes = await GetMatchedEpisodes();

            // now load the actual chromaprint data for all the IDs we need
            HashSet<int?> theme_cp_ids = new HashSet<int?>();
            foreach(EmbyItem item in matched_episodes)
            {
                if(item.theme_cp_id != null)
                {
                    theme_cp_ids.Add(item.theme_cp_id);
                }
            }
            Dictionary<int, ThemeInfo> theme_cp_data = await LoadCpData(theme_cp_ids);

            foreach(EmbyItem item in matched_episodes)
            {
                if(item.theme_cp_id != null)
                {
                    ThemeInfo ti = theme_cp_data[item.theme_cp_id.Value];
                    ProcessEpisode(item, ti);
                }
            }
        }

        public void ProcessEpisode(EmbyItem item, ThemeInfo ti)
        {
            ActionStore item_store = new ActionStore(config);
            // check to see if we have theme start/end data already for this episode

            // do check
            EmbyItem existing_item = item_store.LoadEpisodeData(item);
            if(existing_item != null)
            {
                Console.WriteLine("Episode already processed");
                return;
            }

            // 1 - extract the chromaprint data from the episode
            // 2 - find the offset

            //string input_file = @"C:\Data\media\StarTrek - Discovery - S03E07.mkv";

            string input_file = item.Path;

            // do file path replament
            string replace_text = config.Get("lib_path_replace");
            string[] replace_parts = replace_text.Split('>', StringSplitOptions.RemoveEmptyEntries);
            if(replace_parts.Length == 2)
            {
                input_file = input_file.Replace(replace_parts[0].Replace("\"", ""), replace_parts[1].Replace("\"", ""));
            }
            
            // use the extract length in minutes from the theme service
            int extract_length = 10;
            if(ti.extract_length > 0 && ti.extract_length < 30)
            {
                extract_length = ti.extract_length;
            }
            TimeSpan time_start = new TimeSpan(0, 0, 0);
            TimeSpan time_end = new TimeSpan(0, extract_length, 0);

            // extract chromaprint from episode file
            ActionExtract extract = new ActionExtract(config);
            byte[] episode_cp_data = extract.ExtractChromaprintData(input_file, time_start, time_end);
            Console.WriteLine("Episode cp data length : " + episode_cp_data.Length);

            //get raw byte[] of chromaprint data for intro
            byte[] theme_cp_data = Convert.FromBase64String(ti.theme_cp_data);
            Console.WriteLine("Theme cp data length : " + theme_cp_data.Length);

            // detection the theme in the episode
            ActionDetect detect = new ActionDetect(config);
            bool found = detect.FindBestOffset(episode_cp_data, theme_cp_data, item);
            if(!found)
            {
                Console.WriteLine("Theme not found");
            }
            else
            {
                Console.WriteLine("Found theme, saving data");
                // save theme data
                item_store.SaveEpisodeData(item);
            }
        }

        public async Task<Dictionary<int, ThemeInfo>> LoadCpData(HashSet<int?> theme_ids)
        {
            Dictionary<int, ThemeInfo> theme_info_data = new Dictionary<int, ThemeInfo>();

            ThemeData theme_data = new ThemeData(http_client, config);
            List<ThemeInfo> theme_cp_data = await theme_data.GetThemeData(theme_ids);
            foreach(ThemeInfo theme in theme_cp_data)
            {
                if (theme.id != null)
                {
                    theme_info_data.Add(theme.id.Value, theme);
                }
            }

            return theme_info_data;
        }

        private async Task<List<EmbyItem>> GetMatchedEpisodes()
        {
            // get all the emby data and theme service data and try to match theme intros with episodes

            EmbyData emby_data = new EmbyData(http_client, config);
            EmbyResult series_list = await emby_data.GetSeriesList();

            ThemeData theme_data = new ThemeData(http_client, config);
            List<ThemeInfo> theme_info = await theme_data.GetThemeList();

            Dictionary<string, List<ThemeInfo>> theme_dict = new Dictionary<string, List<ThemeInfo>>();
            foreach (ThemeInfo theme in theme_info)
            {
                if (theme_dict.ContainsKey(theme.imdb))
                {
                    theme_dict[theme.imdb].Add(theme);
                }
                else
                {
                    List<ThemeInfo> themes = new List<ThemeInfo>();
                    themes.Add(theme);
                    theme_dict[theme.imdb] = themes;
                }
            }

            List<EmbyItem> episodes = new List<EmbyItem>();

            // match episodes to chomaprint data, at the moment this uses imbd
            // TODO: should add themovie db and others mathces also
            foreach (EmbyItem series in series_list.Items)
            {
                string imdb = "";
                if (series.ProviderIds.ContainsKey("Imdb"))
                {
                    imdb = series.ProviderIds["Imdb"];
                }

                if (!string.IsNullOrEmpty(imdb) && theme_dict.ContainsKey(imdb))
                {
                    List<ThemeInfo> series_themes = theme_dict[imdb];
                    Console.WriteLine("Getting episodes for series : " + series.Name);
                    EmbyResult episode_list = await emby_data.GetEpisodeList(series.Id);

                    // TODO: for now just match on the first global match and override and season matches
                    // this means season level matches will be prefered
                    // find a good way of picking the best cp if there are multiple
                    Console.WriteLine("Finding best chromaprint theme data for each episode");
                    foreach (EmbyItem episode in episode_list.Items)
                    {
                        // find first season match
                        foreach(ThemeInfo theme in series_themes)
                        {
                            if (theme.season == episode.ParentIndexNumber)
                            {
                                episode.theme_cp_id = theme.id;
                                break;
                            }
                        }
                        // no season theme so just use the first global one
                        if(episode.theme_cp_id == null && series_themes.Count > 0)
                        {
                            episode.theme_cp_id = series_themes[0].id;
                        }

                        if(episode.theme_cp_id != null)
                        {
                            episodes.Add(episode);
                            //Console.WriteLine(episode.Id + " : " + episode.ParentIndexNumber + "-" + episode.IndexNumber + " : " + episode.theme_cp_id);
                        }
                    }
                }
            }

            return episodes;
        }
    }
}
