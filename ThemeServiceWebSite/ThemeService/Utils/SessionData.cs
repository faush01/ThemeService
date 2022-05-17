using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ThemeService.Models;

namespace ThemeService.Utils
{
    public static class SessionData
    {
        public static UserInfo GetActiveUser(HttpContext context)
        {
            UserInfo user_info = null;
            string object_data = context.Session.GetString("user_info");
            if (!string.IsNullOrEmpty(object_data))
            {
                user_info = JsonConvert.DeserializeObject<UserInfo>(object_data);
            }
            return user_info;
        }
    }
}
