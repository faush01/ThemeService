using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
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

        private int? GetInt(SqlDataReader reader, int index)
        {
            int? val = null;
            if(!reader.IsDBNull(index))
            {
                val = reader.GetInt32(index);
            }
            return val;
        }

        private string GetString(SqlDataReader reader, int index)
        {
            string val = null;
            if (!reader.IsDBNull(index))
            {
                val = reader.GetString(index);
            }
            return val;
        }

        private object GetParamValue(object item)
        {
            if(item == null)
            {
                return System.DBNull.Value;
            }
            return item;
        }

        private string ListToParamString(List<string> list)
        {
            List<string> new_list = new List<string>();
            foreach (string item in list)
            {
                string new_item = item.Replace("'", "''");
                new_list.Add("'" + new_item + "'");
            }
            return string.Join(",", new_list);
        }

        public List<ThemeData> GetThemeDataList(ThemeQueryOptions options)
        {
            List<ThemeData> theme_data_list = new List<ThemeData>();

            // build select
            List<string> fields = new List<string>()
                {
                    "id",
                    "hidden",
                    "added_date",
                    "added_by",
                    "edit_date",
                    "edit_by",
                    "imdb",
                    "themoviedb",
                    "thetvdb",
                    "series_name",
                    "season",
                    "episode",
                    "extract_length",
                    "theme_cp_data_size",
                    "theme_cp_data_md5",
                    "verify_count", // dynamic field
                    "verify_users" // dynamic field
                };

            if(options.CpData)
            {
                fields.Add("theme_cp_data");
            }

            // build where
            List<string> where_clause = new List<string>();
            if(!string.IsNullOrEmpty(options.SerieName))
            {
                string name = options.SerieName;
                name = name.Replace("'", "''");
                name = name.Replace("*", "%");
                where_clause.Add("series_name LIKE '%" + name + "%'");
            }
            if(options.Id.Count > 0)
            {
                where_clause.Add("id IN (" + ListToParamString(options.Id) + ")");
            }
            if(options.Imdb.Count > 0)
            {
                where_clause.Add("imdb IN (" + ListToParamString(options.Imdb) + ")");
            }
            if (options.ThemovieDb.Count > 0)
            {
                where_clause.Add("themoviedb IN (" + ListToParamString(options.ThemovieDb) + ")");
            }
            if (options.TheTvDb.Count > 0)
            {
                where_clause.Add("thetvdb IN (" + ListToParamString(options.TheTvDb) + ")");
            }
            if (options.Md5.Count > 0)
            {
                where_clause.Add("theme_cp_data_md5 IN (" + ListToParamString(options.Md5) + ")");
            }
            if (options.AddedBy.Count > 0)
            {
                where_clause.Add("added_by IN (" + ListToParamString(options.AddedBy) + ")");
            }
            if(options.verify_min != null)
            {
                where_clause.Add("verify_count >= " + options.verify_min.Value);
            }
            // hidden or not
            if(options.hidden == false)
            {
                where_clause.Add("hidden = 0");
            }
            else if (options.hidden == true)
            {
                where_clause.Add("hidden = 1");
            }

            // this is to allow fast lookup of field indexes
            Dictionary<string, int> filed_ids = new Dictionary<string, int>();
            for (int index = 0; index < fields.Count; index++)
            {
                filed_ids.Add(fields[index], index);
            }

            string sql = "SELECT";

            if(options.Limit != 0)
            {
                sql += " TOP " + options.Limit;
            }
            
            sql += " " + string.Join(",", fields);

            sql += " FROM theme_data";

            // add in user verification
            sql += " LEFT OUTER JOIN (";
            sql += "SELECT item_id, COUNT(DISTINCT(verified_by)) AS verify_count, STRING_AGG(verified_by, ',') AS verify_users ";
            sql += "FROM theme_verify GROUP BY item_id";
            sql += ") AS verification ON verification.item_id = theme_data.id";

            // add the where clause
            if (where_clause.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", where_clause);
            }

            sql += " ORDER BY " + options.OrderBy + " " + options.OrderByDirection;

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
                            data.hidden = reader.GetBoolean(filed_ids["hidden"]);

                            data.imdb = GetString(reader, filed_ids["imdb"]);
                            data.thetvdb = GetString(reader, filed_ids["thetvdb"]);
                            data.themoviedb = GetString(reader, filed_ids["themoviedb"]);

                            data.season = GetInt(reader, filed_ids["season"]);
                            data.episode = GetInt(reader, filed_ids["episode"]);
                            data.extract_length = GetInt(reader, filed_ids["extract_length"]); 
                            data.series_name = GetString(reader, filed_ids["series_name"]);

                            data.added_by = GetString(reader, filed_ids["added_by"]);
                            data.added_date = reader.GetDateTime(filed_ids["added_date"]);
                            data.edit_date = reader.GetDateTime(filed_ids["edit_date"]);
                            data.edit_by = GetString(reader, filed_ids["edit_by"]);

                            data.verify_count = GetInt(reader, filed_ids["verify_count"]);
                            data.verify_users = GetString(reader, filed_ids["verify_users"]);

                            data.theme_cp_data_size = GetInt(reader, filed_ids["theme_cp_data_size"]);
                            data.theme_cp_data_md5 = GetString(reader, filed_ids["theme_cp_data_md5"]);

                            if (options.CpData)
                            {
                                data.theme_cp_data = GetString(reader, filed_ids["theme_cp_data"]);
                            }

                            theme_data_list.Add(data);
                        }
                    }
                }
            }
            return theme_data_list;
        }

        public void HideTheme(int id, string login_id)
        {
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();

                string sql_theme_data = "UPDATE theme_data SET hidden=1, edit_by=@edit_by, edit_date=GETDATE() WHERE id = @id";
                using (SqlCommand command = new SqlCommand(sql_theme_data, sql_conn))
                {
                    command.Parameters.AddWithValue("id", GetParamValue(id));
                    command.Parameters.AddWithValue("edit_by", GetParamValue(login_id));
                    command.ExecuteNonQuery();
                }

                /*
                string sql_theme_data = "DELETE FROM theme_data WHERE id = @id";
                using (SqlCommand command = new SqlCommand(sql_theme_data, sql_conn))
                {
                    command.Parameters.AddWithValue("id", GetParamValue(id));
                    command.ExecuteNonQuery();
                }

                string sql_theme_verify = "DELETE FROM theme_verify WHERE item_id = @item_id";
                using (SqlCommand command = new SqlCommand(sql_theme_verify, sql_conn))
                {
                    command.Parameters.AddWithValue("item_id", GetParamValue(id));
                    command.ExecuteNonQuery();
                }
                */
            }
        }

        public List<ThemeData> GetThemeHistory(int id)
        {
            string sql = "SELECT " +
                "item_id, " +
                "edit_date, " +
                "edit_by, " + 
                "hidden, " + 
                "imdb, " + 
                "themoviedb, " +
                "thetvdb, " + 
                "series_name, " + 
                "season, " + 
                "episode, " + 
                "extract_length " +
                "FROM theme_history " +
                "WHERE item_id=@item_id " +
                "ORDER BY edit_date DESC";

            List<ThemeData> history_data = new List<ThemeData>();
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();

                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    command.Parameters.AddWithValue("item_id", GetParamValue(id));

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ThemeData data = new ThemeData();

                            data.id = reader.GetInt32(0);
                            data.edit_date = reader.GetDateTime(1);
                            data.edit_by = GetString(reader, 2);

                            data.hidden = reader.GetBoolean(3);

                            data.imdb = GetString(reader, 4);
                            data.themoviedb = GetString(reader, 5);
                            data.thetvdb = GetString(reader, 6);

                            data.series_name = GetString(reader, 7);

                            data.season = GetInt(reader, 8);
                            data.episode = GetInt(reader, 9);
                            data.extract_length = GetInt(reader, 10);

                            history_data.Add(data);
                        }
                    }
                }
            }
            return history_data;
        }

        public void LogHistory(ThemeData theme_data)
        {
            string sql = "INSERT INTO theme_history (item_id, edit_date, edit_by, hidden, imdb, themoviedb, thetvdb, series_name, season, episode, extract_length) " +
                "VALUES (@item_id, @edit_date, @edit_by, @hidden, @imdb, @themoviedb, @thetvdb, @series_name, @season, @episode, @extract_length);";

            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();
                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    command.Parameters.AddWithValue("item_id", GetParamValue(theme_data.id));
                    command.Parameters.AddWithValue("edit_date", GetParamValue(theme_data.edit_date));
                    command.Parameters.AddWithValue("edit_by", GetParamValue(theme_data.edit_by));
                    command.Parameters.AddWithValue("hidden", GetParamValue(theme_data.hidden));

                    command.Parameters.AddWithValue("imdb", GetParamValue(theme_data.imdb));
                    command.Parameters.AddWithValue("themoviedb", GetParamValue(theme_data.themoviedb));
                    command.Parameters.AddWithValue("thetvdb", GetParamValue(theme_data.thetvdb));

                    command.Parameters.AddWithValue("series_name", GetParamValue(theme_data.series_name));
                    command.Parameters.AddWithValue("season", GetParamValue(theme_data.season));
                    command.Parameters.AddWithValue("episode", GetParamValue(theme_data.episode));
                    command.Parameters.AddWithValue("extract_length", GetParamValue(theme_data.extract_length));

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
                "extract_length=@extract_length, " +
                "series_name=@series_name, " +
                "edit_date=GETDATE(), " +
                "edit_by=@edit_by " +
                "WHERE id=@id";
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();
                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    command.Parameters.AddWithValue("id", theme_data.id);
                    command.Parameters.AddWithValue("imdb", GetParamValue(theme_data.imdb));
                    command.Parameters.AddWithValue("themoviedb", GetParamValue(theme_data.themoviedb));
                    command.Parameters.AddWithValue("thetvdb", GetParamValue(theme_data.thetvdb));
                    command.Parameters.AddWithValue("season", GetParamValue(theme_data.season));
                    command.Parameters.AddWithValue("extract_length", GetParamValue(theme_data.extract_length));
                    command.Parameters.AddWithValue("episode", GetParamValue(theme_data.episode));
                    command.Parameters.AddWithValue("series_name", GetParamValue(theme_data.series_name));
                    command.Parameters.AddWithValue("edit_by", GetParamValue(theme_data.edit_by));
                    command.ExecuteNonQuery();
                }
            }
        }

        public int SaveThemeData(ThemeData theme_data)
        {
            string sql = "INSERT INTO theme_data " +
                "(imdb, themoviedb, thetvdb, season, episode, extract_length, series_name, added_by, theme_cp_data_size, theme_cp_data_md5, theme_cp_data) " +
                "VALUES(@imdb, @themoviedb, @thetvdb, @season, @episode, @extract_length, @series_name, @added_by, @theme_cp_data_size, @theme_cp_data_md5, @theme_cp_data);";

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
                    command.Parameters.AddWithValue("imdb", GetParamValue(theme_data.imdb));
                    command.Parameters.AddWithValue("themoviedb", GetParamValue(theme_data.themoviedb));
                    command.Parameters.AddWithValue("thetvdb", GetParamValue(theme_data.thetvdb));
                    command.Parameters.AddWithValue("season", GetParamValue(theme_data.season));
                    command.Parameters.AddWithValue("episode", GetParamValue(theme_data.episode));
                    command.Parameters.AddWithValue("extract_length", GetParamValue(theme_data.extract_length));
                    command.Parameters.AddWithValue("series_name", GetParamValue(theme_data.series_name));
                    command.Parameters.AddWithValue("added_by", GetParamValue(theme_data.added_by));
                    command.Parameters.AddWithValue("theme_cp_data_size", GetParamValue(theme_data.theme_cp_data_size));
                    command.Parameters.AddWithValue("theme_cp_data_md5", GetParamValue(theme_data.theme_cp_data_md5));
                    command.Parameters.AddWithValue("theme_cp_data", GetParamValue(theme_data.theme_cp_data));
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

        public void RemoveVerification(int item_id, string user_id)
        {
            
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();

                string sql = "DELETE FROM theme_verify WHERE item_id = @item_id AND verified_by = @verified_by";
                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    command.Parameters.AddWithValue("item_id", GetParamValue(item_id));
                    command.Parameters.AddWithValue("verified_by", GetParamValue(user_id));
                    command.ExecuteNonQuery();
                }
            }
        }

        public int AddVerification(int item_id, string user_id)
        {
            using (SqlConnection sql_conn = new SqlConnection(GetConnectionString()))
            {
                sql_conn.Open();

                string search_sql = "SELECT id FROM theme_verify WHERE item_id = @item_id AND verified_by = @verified_by";

                using (SqlCommand command = new SqlCommand(search_sql, sql_conn))
                {
                    command.Parameters.AddWithValue("item_id", GetParamValue(item_id));
                    command.Parameters.AddWithValue("verified_by", GetParamValue(user_id));

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            return reader.GetInt32(0);
                        }
                    }
                }

                string sql = "INSERT INTO theme_verify (item_id, verified_by) VALUES(@item_id, @verified_by);";
                using (SqlCommand command = new SqlCommand(sql, sql_conn))
                {
                    command.Parameters.AddWithValue("item_id", GetParamValue(item_id));
                    command.Parameters.AddWithValue("verified_by", GetParamValue(user_id));
                    command.ExecuteNonQuery();
                }

                using (SqlCommand command = new SqlCommand(search_sql, sql_conn))
                {
                    command.Parameters.AddWithValue("item_id", GetParamValue(item_id));
                    command.Parameters.AddWithValue("verified_by", GetParamValue(user_id));

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            return reader.GetInt32(0);
                        }
                    }
                }

                return -1;
            }
        }
    }
}
