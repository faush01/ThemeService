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
                    "added_date",
                    "added_by",
                    "edit_date",
                    "imdb",
                    "themoviedb",
                    "thetvdb",
                    "series_name",
                    "season",
                    "episode",
                    "extract_length",
                    "theme_cp_data_size",
                    "theme_cp_data_md5"//,
                    //"theme_cp_data"
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
                if(name.IndexOf("*") > -1)
                {
                    name = name.Replace("*", "%");
                    where_clause.Add("series_name LIKE '" + name + "'");
                }
                else 
                {
                    where_clause.Add("series_name = '" + name + "'");
                }
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
                "extract_length=@extract_length, " +
                "series_name=@series_name, " +
                "edit_date=GETDATE() " +
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
    }
}
