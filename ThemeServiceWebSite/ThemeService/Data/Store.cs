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

        public List<ThemeData> GetThemeDataList(ThemeQueryOptions options)
        {
            List<ThemeData> theme_data_list = new List<ThemeData>();

            // build select
            List<string> fields = new List<string>()
                {
                    "id",
                    "imdb",
                    "themoviedb",
                    "thetvdb",
                    "season",
                    "episode",
                    "description",
                    "added_by",
                    "added_date",
                    "theme_cp_data_size",
                    "theme_cp_data_md5"
                };

            if(options.CpData)
            {
                fields.Add("theme_cp_data");
            }

            // build where
            List<string> where_clause = new List<string>();
            if(options.Id.Count > 0)
            {
                where_clause.Add("id IN (" + string.Join(",", options.Id) + ")");
            }
            if(options.Imdb.Count > 0)
            {
                List<string> imdb_list = new List<string>();
                foreach (string imbd in options.Imdb) // build the string list and escape any ' chars
                {
                    imdb_list.Add("'" + imbd.Replace("'", "''") + "'");
                }
                where_clause.Add("imdb IN (" + string.Join(",", imdb_list) + ")");
            }


            // this is to allow fast lookup of field indexes
            Dictionary<string, int> filed_ids = new Dictionary<string, int>();
            for (int index = 0; index < fields.Count; index++)
            {
                filed_ids.Add(fields[index], index);
            }

            string sql = "SELECT " + string.Join(",", fields) + " ";
            sql += "FROM theme_data";

            // add the where clause
            if(where_clause.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", where_clause);
            }

            // do query
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

                            data.id = reader.GetInt32(filed_ids["id"]);
                            data.imdb = reader.GetString(filed_ids["imdb"]);
                            data.thetvdb = reader.GetString(filed_ids["thetvdb"]);
                            data.themoviedb = reader.GetString(filed_ids["themoviedb"]);
                            data.season = reader.GetInt32(filed_ids["season"]);
                            data.episode = reader.GetInt32(filed_ids["episode"]);
                            data.description = reader.GetString(filed_ids["description"]);
                            data.added_by = reader.GetString(filed_ids["added_by"]);
                            data.added_date = reader.GetDateTime(filed_ids["added_date"]);
                            data.theme_cp_data_size = reader.GetInt32(filed_ids["theme_cp_data_size"]);
                            data.theme_cp_data_md5 = reader.GetString(filed_ids["theme_cp_data_md5"]);

                            if (options.CpData)
                            {
                                data.theme_cp_data = reader.GetString(filed_ids["theme_cp_data"]);
                            }

                            theme_data_list.Add(data);
                        }
                    }
                }
            }
            return theme_data_list;
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

        public void UpdateTheme(ThemeData theme_data)
        {
            string sql = "UPDATE theme_data SET " +
                "imdb=@imdb, " +
                "themoviedb=@themoviedb, " +
                "thetvdb=@thetvdb, " +
                "season=@season, " +
                "episode=@episode, " +
                "description=@description " +
                "WHERE id=@id";
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();
                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    command.Parameters.AddWithValue("id", theme_data.id);
                    command.Parameters.AddWithValue("imdb", theme_data.imdb);
                    command.Parameters.AddWithValue("themoviedb", theme_data.themoviedb);
                    command.Parameters.AddWithValue("thetvdb", theme_data.thetvdb);
                    command.Parameters.AddWithValue("season", theme_data.season);
                    command.Parameters.AddWithValue("episode", theme_data.episode);
                    command.Parameters.AddWithValue("description", theme_data.description);
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
