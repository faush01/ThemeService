using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroDetection
{
    public class ActionDetect
    {
        private Config config;

        public ActionDetect(Config con)
        {
            config = con;
        }

		public bool FindBestOffset(byte[] episode_cop_buytes, byte[] theme_cp_bytes, EmbyItem item)
        {
			List<uint> episode_cp_uints = BytesToInts(episode_cop_buytes);
			List<uint> theme_cp_uints = BytesToInts(theme_cp_bytes);

			if(episode_cp_uints.Count == 0 || theme_cp_uints.Count == 0 || theme_cp_uints.Count > episode_cp_uints.Count)
            {
				Console.WriteLine("Error with cp data : episode[" + episode_cp_uints.Count + "] theme[" + theme_cp_uints.Count + "]");
				return false;
            }

			List<uint> distances = GetDistances(episode_cp_uints, theme_cp_uints);

			int? best_start_offset = GetBestOffset(distances);

			if(best_start_offset == null)
            {
				Console.WriteLine("Theme not found!");
				return false;
            }

			// based on testing it looks like it is about 8.06 ints per second
			// based on the options used in the ffmpeg audio mixing and cp muxing
			// TODO: this need further investigation
			double ints_per_sec = 8.06;

			int theme_start = (int)(best_start_offset / ints_per_sec);
			TimeSpan ts_start = new TimeSpan(0, 0, theme_start);
			
			int theme_end = theme_start + (int)(theme_cp_uints.Count / ints_per_sec);
			TimeSpan ts_end = new TimeSpan(0, 0, theme_end);

			Console.WriteLine("Theme At : " + ts_start + " - " + ts_end);

			item.theme_start = ts_start;
			item.theme_end = ts_end;
			return true;
		}

		private int? GetBestOffset(List<uint> distances)
        {
			uint sum_distances = 0;
			uint min_dist = uint.MaxValue;
			int? min_offset = null;
			for (int x = 0; x < distances.Count; x++)
			{
				sum_distances += distances[x];
				if (distances[x] < min_dist)
				{
					min_dist = distances[x];
					min_offset = x;
				}
			}

			double average_distance = sum_distances / distances.Count;
			uint distance_threshold = (uint)(average_distance * 0.5);  // TODO: find a good threshold

			Console.WriteLine("Min Distance        : " + min_dist);
			Console.WriteLine("Average Distance    : " + average_distance);
			Console.WriteLine("Distance Threshold  : " + distance_threshold);
			Console.WriteLine("Min Distance Offset : " + min_offset);

			if(min_dist > distance_threshold)
            {
				Console.WriteLine("Min distance  was not below average distance threshold!");
				return null;
            }

			return min_offset;
		}

		private List<uint> GetDistances(List<uint> episode_cp_data, List<uint> theme_cp_data)
        {
			List<uint> distances = new List<uint>();

			int last_offset = (episode_cp_data.Count - theme_cp_data.Count) + 1;
			for (int offset = 0; offset < last_offset; offset++)
			{
				uint total_distance = 0;
				for (int x = 0; x < theme_cp_data.Count; x++)
				{
					uint left = episode_cp_data[x + offset];
					uint right = theme_cp_data[x];
					uint this_score = GetHammingDist(left, right);
					total_distance += this_score;
				}
				distances.Add(total_distance);
			}

			return distances;
		}

		private List<uint> BytesToInts(byte[] cp_byte_data)
		{
			List<uint> cp_data = new List<uint>();
			if(cp_byte_data.Length == 0 || cp_byte_data.Length % 4 != 0)
            {
				Console.WriteLine("Error : CP Theme data is not correct length : " + cp_byte_data.Length);
				return cp_data;
			}
			using (MemoryStream ms = new MemoryStream(cp_byte_data))
			{
				using (BinaryReader binaryReader = new BinaryReader(ms))
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

		private uint GetHammingDist(uint left, uint right)
		{
			// https://stackoverflow.com/questions/1024904/calculating-hamming-weight-efficiently-in-matlab
			// http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetNaive
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
	}
}
