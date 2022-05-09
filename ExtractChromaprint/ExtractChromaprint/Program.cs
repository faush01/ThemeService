using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;

//TimeSpan time_start = new TimeSpan(0, 7, 47);
//TimeSpan time_end = new TimeSpan(0, 9, 16);

TimeSpan time_start = new TimeSpan(0, 0, 0);
TimeSpan time_end = new TimeSpan(0, 15, 0);

byte[] chromaprint_bytes = GetChromaprintData(time_start, time_end);
Console.WriteLine("Chromaprint Data Length : " + chromaprint_bytes.Length);

using (FileStream s = new FileStream(@"C:\Development\IntroSkip\extract_03_full.bin", FileMode.Create))
{
	s.Write(chromaprint_bytes, 0, chromaprint_bytes.Length);
}

byte[] GetChromaprintData(TimeSpan ts_start, TimeSpan ts_end)
{
	string ffmpeg_path = @"C:\Install\ffmpeg\ffmpeg-5.0.1-full_build\bin\ffmpeg";
	string input_file = @"C:\Data\media\StarTrek - Discovery - S03E07.mkv";

	TimeSpan ts_duration = ts_end - ts_start;

	List<string> command_params = new List<string>();
	if(ts_start.TotalSeconds != 0)
    {
		command_params.Add(string.Format("-ss {0}", ts_start));
    }

	command_params.Add(string.Format("-t {0}", ts_duration));
	command_params.Add("-i \"" + input_file + "\"");
	command_params.Add("-ac 1");
	command_params.Add("-acodec pcm_s16le");
	command_params.Add("-ar 16000");
	command_params.Add("-c:v nul");
	command_params.Add("-f chromaprint");
	command_params.Add("-fp_format raw");
	command_params.Add("-");
	//command_params.Add("\"" + output_file + "\"");

	string param_string = string.Join(" ", command_params);
	Console.WriteLine(ffmpeg_path + " " + param_string);

	ProcessStartInfo start_info = new ProcessStartInfo(ffmpeg_path, param_string);
	start_info.RedirectStandardOutput = true;
	start_info.RedirectStandardError = false;
	start_info.UseShellExecute = false;
	start_info.CreateNoWindow = true;

	byte[] chroma_bytes = new byte[0];
	int return_code = -1;
	using (Process process = new Process(){StartInfo = start_info})
	{
		process.Start();
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
	Console.WriteLine("Exit Code : " + return_code);

	return chroma_bytes;
}