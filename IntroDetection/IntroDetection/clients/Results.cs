using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroDetection
{
    public class ThemeInfo
    {
        public int? id { get; set; }
        public string? imdb { get; set; }
        public string? themoviedb { get; set; }
        public string? thetvdb { get; set; }
        public int season { get; set; }
        public int episode { get; set; }
        public int extract_length { get; set; }
        public string? description { get; set; }
        public string? added_by { get; set; }
        public DateTime added_date { get; set; }
        public int theme_cp_data_size { get; set; }
        public string? theme_cp_data_md5 { get; set; }
        public string? theme_cp_data { get; set; }
    }

    public class EmbyResult
    {
        public List<EmbyItem>? Items { set; get; }
        public int TotalRecordCount { set; get; }
    }

    public class EmbyItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public Dictionary<string, string>? ProviderIds { set; get; }
        public string? Path { get; set; }
        public int? SeriesId { get; set; }
        public string? SeriesName { get; set; }
        public int? IndexNumber { get; set; }
        public int? ParentIndexNumber { get; set; }
        public long? RunTimeTicks { get; set; }
        public List<Chapter>? Chapters { get; set; }
        public int? theme_cp_id { get; set; }
        public TimeSpan? theme_start { get; set; }
        public TimeSpan? theme_end { get; set; }

    }

    public class Chapter
    { 
        public long? StartPositionTicks { get; set; }
        public string? Name { get; set; }
    }

}
