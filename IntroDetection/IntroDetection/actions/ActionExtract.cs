
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace IntroDetection
{
    public class ActionExtract
    {
        private Config config;
        public ActionExtract(Config con)
        {
            config = con;
        }

		public byte[] ExtractChromaprintData(string input_file, TimeSpan ts_start, TimeSpan ts_end)
		{
			if (string.IsNullOrEmpty(input_file))
			{
				Console.WriteLine("Input filename is empty");
				return new byte[0];
			}

			FileInfo input_file_info = new FileInfo(input_file);
			if (!input_file_info.Exists)
			{
				Console.WriteLine("Input file does not exist : " + input_file);
				return new byte[0];
			}

			Stopwatch sw = new Stopwatch();
			sw.Start();
			string ffmpeg_path = config.Get("ffmpeg_path");

			TimeSpan ts_duration = ts_end - ts_start;

			List<string> command_params = new List<string>();
			if (ts_start.TotalSeconds != 0)
			{
				command_params.Add(string.Format("-ss {0}", ts_start));
			}

			//string output_file = @"C:\Development\IntroSkip\ffmpeg\temp.bin";

			command_params.Add(string.Format("-t {0}", ts_duration));
			command_params.Add("-i \"" + input_file + "\"");
			command_params.Add("-ac 1");
			command_params.Add("-acodec pcm_s16le");
			command_params.Add("-ar 16000");
			command_params.Add("-c:v nul");
			command_params.Add("-f chromaprint");
			command_params.Add("-fp_format raw");
			command_params.Add("-");
			//command_params.Add("-y");
			//command_params.Add("\"" + output_file + "\"");

			string param_string = string.Join(" ", command_params);
			Console.WriteLine(ffmpeg_path + " " + param_string);

			ProcessStartInfo start_info = new ProcessStartInfo(ffmpeg_path, param_string);
			start_info.RedirectStandardOutput = true;
			start_info.RedirectStandardError = false;
			start_info.UseShellExecute = false;
			start_info.CreateNoWindow = true;

			List<byte> output = new List<byte>();
			byte[] chroma_bytes = new byte[0];
			int return_code = -1;
			using (Process process = new Process() { StartInfo = start_info })
			{
				process.Start();
				//process.WaitForExit(1000 * 60 * 3);

				FileStream baseStream = process.StandardOutput.BaseStream as FileStream;
				using (MemoryStream ms = new MemoryStream())
				{
					int last_read = 0;
					byte[] buffer = new byte[4096];
					do
					{
						last_read = baseStream.Read(buffer, 0, buffer.Length);
						ms.Write(buffer, 0, last_read);
					} while (last_read > 0);

					chroma_bytes = ms.ToArray();
				}
				
				return_code = process.ExitCode;
			}

			/*
			using (FileStream fs = new FileStream(output_file, FileMode.Open))
            {
				using (MemoryStream ms = new MemoryStream())
				{
					int last_read = 0;
					byte[] buffer = new byte[4096];
					do
					{
						last_read = fs.Read(buffer, 0, buffer.Length);
						ms.Write(buffer, 0, last_read);
					} while (last_read > 0);

					chroma_bytes = ms.ToArray();
				}
			}
			*/

			sw.Stop();
			Console.WriteLine("Exit Code : " + return_code + " time : " + sw.ElapsedMilliseconds);

			return chroma_bytes;
		}

	}
}
