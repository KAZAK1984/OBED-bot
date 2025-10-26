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
			х[а-яa-z]*[уё][а-яa-z]*[йиве]|										# хуёвый, хуй, хули и т.д.
			б[а-яa-z]*[л][а-яa-z]*[я]|											# бля, блять, блядина и т.д.  
			п[а-яa-z]*[и][а-яa-z]*[зд]|											# пизд, пизда, пиздец и т.д.
			п[а-яa-z]*[и][а-яa-z]*[дт]|											# пидр, пидор, пидораска и т.д.
			ш[а-яa-z]*[л][а-яa-z]*[ю][a-яa-z]*[хш]|								# шлюхи, шлюха, шлюший и т.д.
			ш[а-яa-z]*[а][а-яa-z]*[л][a-яa-z]*[а][a-яa-z]*[в]|					# шалава, шалавы, шалав и т.д.
			[иеё][а-яa-z]*[б]|													# еб, ебать, поибота и т.д.
			г[а-яa-z]*[ао][а-яa-z]*[н][а-яa-z]*[д]|								# гандон, гондонский, гондон и т.д.
			з[а-яa-z]*[а][а-яa-z]*[л][а-яa-z]*[у]|								# залупа, залупаться, залупы и т.д.
			п[а-яa-z]*[е][а-яa-z]*[н][а-яa-z]*[и][а-яa-z]*[с]|					# пенис, пенисы, пениса и т.д.
			в[а-яa-z]*[а][а-яa-z]*[г][а-яa-z]*[и][а-яa-z]*[н]|					# вагина, вагины, вагинальный и т.д.
			к[а-яa-z]*[л][а-яa-z]*[и][а-яa-z]*[т][а-яa-z]*[о]|					# клитор, клитора, клитеру и т.д.
			д[а-яa-z]*[р][а-яa-z]*[о][а-яa-z]*[ч]|								# дроч, дрочить, дрочила и т.д.
			м[а-яa-z]*[о][а-яa-z]*[ш][а-яa-z]*[он]|								# мошонка, мошня, мошоночный и т.д.
			е[а-яa-z]*[л][а-яa-z]*[д]|											# елда, елды, елду и т.д.
			т[а-яa-z]*[р][а-яa-z]*[а][а-яa-z]*[х]|								# трах, трахну, отрахали и т.д.
			ж[а-яa-z]*[и][а-яa-z]*[д]|											# жид, жиды, жиду и т.д.
			х[а-яa-z]*[о][а-яa-z]*[х][а-яa-z]*[ол]|								# хохол, хохлы, хохлятский и т.д.
			м[а-яa-z]*[оа][а-яa-z]*[сз][а-яa-z]*[к][а-яa-z]*[ао][а-яa-z]*[л]	# москаль, москали, мазкаль и т.д.
		", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, "ru-RU")]
		private static partial Regex swearRegex ();
	}
}
