using System.Data;
using System.Data.SqlClient;
using System.Reflection.Metadata.Ecma335;
using ThemeService.Models;

namespace ThemeService.Data
{
    public class Store
    {
        private IConfiguration _config;

        public Store(IConfiguration configuration)
        {
            _config = configuration;
        }

        private string GetConnectionString()
        {
            string db_server = _config.GetValue<string>("DatabaseInfo:Server");
            string db_name = _config.GetValue<string>("DatabaseInfo:Database");
            string db_user = _config.GetValue<string>("DatabaseInfo:User");
            string db_pwd = _config.GetValue<string>("DatabaseInfo:Password");

            string fmt_str = "Server = {0}; Database = {1}; User ID = {2}; Password = {3}; " +
                "Trusted_Connection = False; Encrypt = True; Connection Timeout = 300;";
            string ms_conn_str = string.Format(fmt_str, db_server, db_name, db_user, db_pwd);

            return ms_conn_str;
        }

        public bool AddUser(UserInfo new_user)
        {
            string sql = "INSERT INTO users (username, password) VALUES (@username, @password)";
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();
                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    command.Parameters.AddWithValue("username", new_user.username);
                    command.Parameters.AddWithValue("password", new_user.password);
                    command.ExecuteNonQuery();
                }
            }

            return true;
        }

        public void UpdateLastLoginDate(UserInfo user)
        {
            string sql = "UPDATE users SET lastlogin = GETDATE() WHERE username = @username";
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();
                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    command.Parameters.AddWithValue("username", user.username);
                    command.ExecuteNonQuery();
                }
            }
        }

        public UserInfo GetUserInfo(string username)
        {
            string sql = "SELECT id, username, password, lastlogin, access FROM users WHERE username = @username";
            UserInfo user = null;
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();
                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    command.Parameters.AddWithValue("username", username);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            user = new UserInfo();
                            user.id = reader.GetInt32(0);
                            user.username = reader.GetString(1);
                            user.password = reader.GetString(2);
                            if (!reader.IsDBNull(3))
                            {
                                user.last_login = reader.GetDateTime(3);
                            }
                            user.access = reader.GetInt32(4);
                        }
                    }
                }
            }
            return user;
        }

        public List<ThemeData> GetThemeDataList()
        {
            List<ThemeData> theme_data = new List<ThemeData>();
            string sql = "SELECT id, imdb, themoviedb, thetvdb, season, episode, description, added_by, added_date, theme_cp_data_size, theme_cp_data_md5 FROM theme_data;";
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();
                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ThemeData data = new ThemeData();
                            data.id = reader.GetInt32(0);
                            data.imdb = reader.GetString(1);
                            data.thetvdb = reader.GetString(2);
                            data.themoviedb = reader.GetString(3);
                            data.season = reader.GetInt32(4);
                            data.episode = reader.GetInt32(5);
                            data.description = reader.GetString(6);
                            data.added_by = reader.GetString(7);
                            data.added_date = reader.GetDateTime(8);
                            data.theme_cp_data_size = reader.GetInt32(9);
                            data.theme_cp_data_md5 = reader.GetString(10);
                            theme_data.Add(data);
                            //image_data = (byte[])reader[1];
                        }
                    }
                }
            }
            return theme_data;
        }

        public ThemeData GetThemeData(int id)
		{
            string sql = "SELECT id, imdb, themoviedb, thetvdb, season, episode, description, added_by, " +
                "added_date, theme_cp_data_size, theme_cp_data_md5, theme_cp_data FROM theme_data WHERE id = " + id;
            ThemeData theme_data = null;
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();
                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            theme_data = new ThemeData();

                            theme_data.id = reader.GetInt32(0);
                            theme_data.imdb = reader.GetString(1);
                            theme_data.themoviedb = reader.GetString(2);
                            theme_data.thetvdb = reader.GetString(3);

                            theme_data.season = reader.GetInt32(4);
                            theme_data.episode = reader.GetInt32(5);

                            theme_data.description = reader.GetString(6);
                            theme_data.added_by = reader.GetString(7);
                            theme_data.added_date = reader.GetDateTime(8);
                            theme_data.theme_cp_data_size = reader.GetInt32(9);
                            theme_data.theme_cp_data_md5 = reader.GetString(10);
                            theme_data.theme_cp_data = reader.GetString(11);

                            //if (!reader.IsDBNull(3))
                            //{
                            //    user.last_login = reader.GetDateTime(3);
                            //}
                        }
                    }
                }
            }
            return theme_data;
        }

        public void DeleteTheme(int id)
        {
            string sql = "DELETE FROM theme_data WHERE id = " + id;
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();
                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public int SaveThemeData(ThemeData theme_data)
        {
            string sql = "INSERT INTO theme_data " +
                "(imdb, themoviedb, thetvdb, season, episode, description, added_by, theme_cp_data_size, theme_cp_data_md5, theme_cp_data) " +
                "VALUES(@imdb, @themoviedb, @thetvdb, @season, @episode, @description, @added_by, @theme_cp_data_size, @theme_cp_data_md5, @theme_cp_data);";

            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();
                 
                string search_sql = "SELECT id FROM theme_data WHERE theme_cp_data_md5 = '" + theme_data.theme_cp_data_md5 + "'";

                using (SqlCommand command = new SqlCommand(search_sql, sql_conn))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            return reader.GetInt32(0);
                        }
                    }
                }

                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    command.Parameters.AddWithValue("imdb", theme_data.imdb);
                    command.Parameters.AddWithValue("themoviedb", theme_data.themoviedb);
                    command.Parameters.AddWithValue("thetvdb", theme_data.thetvdb);
                    command.Parameters.AddWithValue("season", theme_data.season);
                    command.Parameters.AddWithValue("episode", theme_data.episode);
                    command.Parameters.AddWithValue("description", theme_data.description);
                    command.Parameters.AddWithValue("added_by", theme_data.added_by);
                    command.Parameters.AddWithValue("theme_cp_data_size", theme_data.theme_cp_data_size);
                    command.Parameters.AddWithValue("theme_cp_data_md5", theme_data.theme_cp_data_md5);
                    command.Parameters.AddWithValue("theme_cp_data", theme_data.theme_cp_data);
                    command.ExecuteNonQuery();
                }

                using (SqlCommand command = new SqlCommand(search_sql, sql_conn))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            return reader.GetInt32(0);
                        }
                    }
                }
            }

            return -1;
        }
    }
}
