using System.Text;

string theme_cp_path = @"C:\Development\IntroSkip\ffmpeg\discovery_theme_01.bin";
List<uint> theme_cp_data = LoadData(theme_cp_path);
Console.WriteLine("Theme CP Lenth: " + theme_cp_data.Count);

string episode_cp_path = @"C:\Development\IntroSkip\ffmpeg\discovery_15min.bin";
List<uint> episode_cp_data = LoadData(episode_cp_path);
Console.WriteLine("Episode CP Lenth: " + episode_cp_data.Count);

List<uint> distances = new List<uint>();

int last_offset = (episode_cp_data.Count - theme_cp_data.Count) + 1;
for(int offset = 0; offset < last_offset; offset++)
{
	uint total_distance = 0;
	for(int x = 0; x < theme_cp_data.Count; x++)
    {
		uint left = episode_cp_data[x + offset];
		uint right = theme_cp_data[x];
		uint this_score = GetHammingDist(left, right);
		total_distance += this_score;
	}
	distances.Add(total_distance);
}

Console.WriteLine(distances.Count);
WriteResult(distances, @"C:\Development\IntroSkip\ffmpeg\distances.tsv");

uint sum_distances = 0;
uint min_dist = uint.MaxValue;
int min_offset = -1;
for(int x = 0; x < distances.Count; x++)
{
	sum_distances += distances[x];
	if (distances[x] < min_dist)
    {
		min_dist = distances[x];
		min_offset = x;
	}
}
double average_distance = sum_distances / distances.Count;
Console.WriteLine("Min Distance        : " + min_dist);
Console.WriteLine("Average Distance    : " + average_distance);
Console.WriteLine("Min Distance Offset : " + min_offset);

// based on testing it looks like it is about 8.06 ints per second based on the options I used in the ffmpeg audio flattening
int theme_start = (int)(min_offset / 8.06);
TimeSpan ts = new TimeSpan(0, 0, theme_start);
Console.WriteLine("Theme At : " + ts);

// https://stackoverflow.com/questions/1024904/calculating-hamming-weight-efficiently-in-matlab
// http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetNaive
uint GetHammingDist(uint left, uint right)
{
	//w = bitand( bitshift(w, -1), uint32(1431655765)) + bitand(w, uint32(1431655765));
	//w = bitand(bitshift(w, -2), uint32(858993459)) + bitand(w, uint32(858993459));
	//w = bitand(bitshift(w, -4), uint32(252645135)) + bitand(w, uint32(252645135));
	//w = bitand(bitshift(w, -8), uint32(16711935)) + bitand(w, uint32(16711935));
	//w = bitand(bitshift(w, -16), uint32(65535)) + bitand(w, uint32(65535));

	uint distance = left ^ right;
	distance = ((distance >> 1) & 1431655765U) + (distance & 1431655765U);
	distance = ((distance >> 2) & 858993459U) + (distance & 858993459U);
	distance = ((distance >> 4) & 252645135U) + (distance & 252645135U);
	distance = ((distance >> 8) & 16711935U) + (distance & 16711935U);
	distance = ((distance >> 16) & 65535U) + (distance & 65535U);
	return distance;
}

void WriteResult(List<uint> distances, string path)
{
	using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
	{
		foreach (uint dist in distances)
		{
			fileStream.Write(Encoding.UTF8.GetBytes(dist.ToString() + "\n"));
		}
	}
}

List<uint> LoadData(string cp_path)
{
	List<uint> cp_data = new List<uint>();
	using (FileStream fileStream = new FileStream(cp_path, FileMode.Open, FileAccess.Read, FileShare.Read))
	{
		using (BinaryReader binaryReader = new BinaryReader(fileStream))
		{
			int num = (int)binaryReader.BaseStream.Length / 4;
			for (int i = 0; i < num; i++)
			{
				cp_data.Add(binaryReader.ReadUInt32());
			}
		}
	}
	return cp_data;
}

