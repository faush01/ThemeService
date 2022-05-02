namespace ThemeService.Data
{
    public class ThemeQueryOptions
    {
        public bool CpData { get; set; } = false;
        public List<int> Id { get; set; } = new List<int>();
        public List<string> Imdb { get; set; } = new List<string>();
        public List<string> ThemovieDb { get; set; } = new List<string>();
        public List<string> TheTvDb { get; set; } = new List<string>();
        public List<int> Season { get; set; } = new List<int>();
        public List<int> Episode { get; set; } = new List<int>();
        public string AddedBy { get; set; }

    }
}
