namespace ThemeService.Models
{
    public class GitHubUser
    {
        public bool IsAuthenticated { get; set; } = false;
        public string UserName { get; set; }
        public string LoginId { get; set; }
        public string HomePage { get; set; }
        public string AvatarUrl { get; set; }

    }
}
