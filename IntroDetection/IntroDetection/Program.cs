using IntroDetection;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class Program
{
    public static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();   
    }

    private static async Task MainAsync(string[] args)
    {
        string config_file = @"config.txt";
        if (args.Length > 0)
        {
            config_file = args[0];
        }
        Console.WriteLine("Config file : " + config_file);

        HttpClient http_client = new HttpClient();
        Config config = new Config(config_file);

        ActionProcess process_episodes = new ActionProcess(http_client, config);
        await process_episodes.ProcessEpisodes();
    }

}
