using System;

namespace ThemeService.Models
{
	public class ThemeData
	{
		public int id { get; set; }
		public string imdb { get; set; }
		public string themoviedb { get; set; }
		public string thetvdb { get; set; }
		public int? season { get; set; }
		public int? episode { get; set; }
		public int? extract_length { get; set; }
		public string series_name { get; set; }
		public string added_by { get; set; }
		public DateTime? added_date { get; set; }
		public DateTime? edit_date { set; get; }
		public int? theme_cp_data_size { get; set; }
		public string theme_cp_data_md5 { get; set; }
		public string theme_cp_data { get; set; }
	}
}
