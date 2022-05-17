using System;

namespace ThemeService.Models
{
    public class UserInfo
    {
        public int id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public DateTime last_login { get; set; }
        public int access { get; set; }
    }
}
