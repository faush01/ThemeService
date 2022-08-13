using System.Collections.Generic;

namespace ThemeService.Data
{
    public class ThemeQueryOptions
    {
        public bool CpData { get; set; } = false;
        public List<string> Id { get; set; } = new List<string>();
        public List<string> Imdb { get; set; } = new List<string>();
        public List<string> ThemovieDb { get; set; } = new List<string>();
        public List<string> TheTvDb { get; set; } = new List<string>();
        public List<string> Season { get; set; } = new List<string>();
        public List<string> Episode { get; set; } = new List<string>();
        public string AddedBy { get; set; }
        public string SerieName { get; set; }
    }
}
