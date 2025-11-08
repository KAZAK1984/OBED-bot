using System.Text.RegularExpressions;

namespace OBED.Include
{
	static partial class AutoMod
	{
		public static string BoldCandidateCensor(string str)
		{
			Regex regexStr = SwearRegex();
			Regex allowedStr = AllowedWordsRegex();

			if (regexStr.Matches(str).Count == 0)
				return str;

			var splitStr = str.Split(' ');
			for (int i = 0; i < splitStr.Length; ++i)
			{
				if (allowedStr.IsMatch(splitStr[i]))
					continue;
				if (regexStr.IsMatch(splitStr[i]))
				{
					splitStr[i] = regexStr.Replace(
						splitStr[i],
						m => $"<u><b>{m.Value}</b></u>");
				}
			}

			return string.Join(' ', splitStr);
		}
		public static string AddCensor(string str)
		{
			Regex regexStr = SwearRegex();
			Regex allowedStr = AllowedWordsRegex();

			if (regexStr.Matches(str).Count == 0)
				return str;

			var splitStr = str.Split(' ');
			for (int i = 0; i < splitStr.Length; ++i)
			{
				if (allowedStr.IsMatch(splitStr[i]))
					continue;
				if (regexStr.IsMatch(splitStr[i]))
					splitStr[i] = "<b>[-]</b>";
			}

			return string.Join(' ', splitStr);
		}

		[GeneratedRegex(@"
			х[а-яa-z]*[уё][а-яa-z]*[йиве]|										# хуёвый, хуй, хули и т.д.
			б[а-яa-z]*[л][а-яa-z]*[я]|											# бля, блять, блядина и т.д.  
			п[а-яa-z]*[и][а-яa-z]*[здтс]|										# пизд, пидор, пиздец и т.д.
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
			м[а-яa-z]*[оа][а-яa-z]*[сз][а-яa-z]*[к][а-яa-z]*[ао][а-яa-z]*[л]|	# москаль, москали, мазкаль и т.д.
			в[а-яa-z]*[у][а-яa-z]*[л][а-яa-z]*[в]|								# вульва, вулва, вульву и т.д.
			с[а-яa-z]*[п][а-яa-z]*[е][а-яa-z]*[р][а-яa-z]*[м]|					# сперма, сперму, спермаглот и т.д.
			у[а-яa-z]*[зл][а-яa-z]*[б][а-яa-z]*[е][а-яa-z]|						# узбек, узбеки, улугбек и т.д.
			т[а-яa-z]*[а][а-яa-z]*[дт][а-яa-z]*[ж][а-яa-z]|						# таджик, татжик, таджичка и т.д.
			к[а-яa-z]*[ыи][а-яa-z]*[р][а-яa-z]*[гк][а-яa-z][ыи][а-яa-z]			# кыргыз, киргизы, гыргызка и т.д.
		", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, "ru-RU")]
		private static partial Regex SwearRegex();
	}
	static partial class AutoMod
	{
		[GeneratedRegex(@"
			хлеб|небо|себя|тебе|тебя|европа|европейский|небеса|облако|снеба|
			писатель|письмо|писать|писал|пишу|написано|список|описание|записка|выписал|
			гандбол|гандхары|
			узбекский|узбекистан|узбекская|узбекской|
			таджикский|таджикистан|таджикская|таджикской|
			кыргызстан|киргизский|кыргызский|киргизия|
			пришлю|оштукатурен|послал|прислал|ошлю|шлюпка|шлюз|шлюпок|
			дротик|подросток|дрова|дрофа|дровокол|
			мошкара|мошка|мошение
		", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, "ru-RU")]
		private static partial Regex AllowedWordsRegex();
	}
}
