using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroDetection
{
    public class Config
    {
        private Dictionary<string, string> config_data = new Dictionary<string, string>();

        public Config(string config_file)
        {
            string[] lines = System.IO.File.ReadAllLines(config_file);
            foreach(string line in lines)
            {
                int index = line.IndexOf("=");
                if(!line.StartsWith("#") && index != -1)
                {
                    string key = line.Substring(0, index);
                    string value = line.Substring(index + 1);
                    config_data[key] = value.Trim();
                }
            }
        }

        public string Get(string key)
        {
            if(config_data.ContainsKey(key))
            {
                return config_data[key];
            }
            else
            {
                Console.WriteLine("Config key not found : " + key);
                return "";
            }
        }
    }
}
