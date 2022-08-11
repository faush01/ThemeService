using System.Security.Claims;
using ThemeService.Models;

namespace ThemeService.Utils
{
    public static class UserDetails
    {
        public static GitHubUser GetUserInfo(ClaimsPrincipal user)
        {
            GitHubUser user_info = new GitHubUser();

            if (user.Identity.IsAuthenticated)
            {
                user_info.IsAuthenticated = true;
                user_info.UserName = user.FindFirst(c => c.Type == ClaimTypes.Name)?.Value;
                user_info.LoginId = user.FindFirst(c => c.Type == "urn:github:login")?.Value;
                user_info.HomePage = user.FindFirst(c => c.Type == "urn:github:url")?.Value;
                user_info.AvatarUrl = user.FindFirst(c => c.Type == "urn:github:avatar")?.Value;
            }

            return user_info;
        }
    }
}
