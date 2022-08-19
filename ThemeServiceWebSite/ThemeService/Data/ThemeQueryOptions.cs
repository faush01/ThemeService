using System.Collections.Generic;

namespace ThemeService.Data
{
    public enum OrderBy {
        added_date = 0,
        edit_date = 1,
        series_name = 2
    }

    public enum OrderByDirection
    {
        DESC = 0,
        ASC = 1
    }

    public class ThemeQueryOptions
    {
        public bool CpData { get; set; } = false;
        public List<string> Id { get; set; } = new List<string>();
        public List<string> Imdb { get; set; } = new List<string>();
        public List<string> ThemovieDb { get; set; } = new List<string>();
        public List<string> TheTvDb { get; set; } = new List<string>();
        public List<string> Season { get; set; } = new List<string>();
        public List<string> Episode { get; set; } = new List<string>();
        public List<string> AddedBy { get; set; } = new List<string>();
        public List<string> Md5 { get; set; } = new List<string>();
        public string SerieName { get; set; }
        public int? verify_min { get; set; }
        public int Limit { get; set; } = 10;
        public OrderBy OrderBy { get; set; } = OrderBy.added_date;
        public OrderByDirection OrderByDirection { get; set; } = OrderByDirection.DESC;
    }
}
