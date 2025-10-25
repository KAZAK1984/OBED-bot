using System.Text;
using System.Text.RegularExpressions;

namespace OBED.Include
{
	partial class AutoMod
	{
		public static string BoldCandidateCensor(string str)
		{
			Regex regexStr = swearRegex();

			if (regexStr.Matches(str).Count == 0)
				return str;

			var strBd = new StringBuilder(str);
			foreach (Match match in regexStr.Matches(str))
				strBd.Replace(match.Value, $"<u><b>{match.Value}</b></u>");

			return strBd.ToString();
		}
		public static string AddCensor(string checkedStr)
		{
			Regex regexStr = swearRegex();

			if (regexStr.Matches(checkedStr).Count == 0)
				return checkedStr;

			var splitStr = checkedStr.Split(' ');
			foreach(Match match in regexStr.Matches(checkedStr))
			{
				for (int i = 0; i < splitStr.Length; ++i)
				{
					if (splitStr[i].Contains(match.Value))
						splitStr[i] = "<b>[цензура]</b>";
				}
			}

			return string.Join(' ', splitStr);
		}

		[GeneratedRegex(@"
			х[а-яa-z]*[уё][а-яa-z]*(й|и|в)|				# хуёвый, хуй, хули и т.д.
			б[а-яa-z]*[л][а-яa-z]*[я]|					# бля, блять, блядина и т.д.  
			п[а-яa-z]*[и][а-яa-z]*[зд]|					# пизд, пизда, пиздец и т.д.
			п[а-яa-z]*[и][а-яa-z]*[дт]|					# пидр, пидор, пидораска и т.д.
			ш[а-яa-z]*[л][а-яa-z]*[ю][a-яa-z]*(х|ш)|	# шлюхи, шлюха, шлюший и т.д.
			[её][а-яa-z]*б|								# еб, ебать, ёб и т.д.
			г[а-яa-z]*[ао][а-яa-z]*[н][a-яa-z]*[д]		# гандон, гондонский, гондон и т.д.
			з[а-яa-z]*[а][а-яa-z]*[л][a-яa-z]*[у]		# залупа, залупаться, залупы и т.д.
		", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, "ru-RU")]
		private static partial Regex swearRegex ();
	}
}
