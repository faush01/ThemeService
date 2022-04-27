using System.Text;

namespace ThemeService.Utils
{
	public static class StringUtils
	{
		public static string WrapString(string value, int cols)
		{
			if(value == null)
			{
				return "";
			}
			StringBuilder sb = new StringBuilder();
			for(int x = 0; x < value.Length; x += cols)
			{
				if((x + cols) > value.Length)
				{
					sb.Append(value.Substring(x));
				}
				else
				{
					sb.Append(value.Substring(x, cols) + "\n");
				}
			}
			return sb.ToString();
		}
	}
}
