using OBED.Include;
using OBED.Services;
using OBED.TelegramBot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OBED.Handlers
{
	public class SelectorHandler : ICommandHandler, IResponseSender
	{
		public bool CanHandle(string command) => command.StartsWith("/selector");
		public async Task HandleAsync(Message msg)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(msg.Text);

			var user = await RegistrationService.TryGetOrRegisterUser(msg);
			var parsedArgs = ParserService.ParseCommand(msg.Text);
			List<BasePlace> sortedPlaces = [.. parsedArgs.Places.OrderByDescending(p => p.Reviews.Average(r => r.Rating))];

            (bool first, bool second) checker = (false, false);
			if (parsedArgs.Raw.Count == 1 && sortedPlaces.FirstOrDefault() is ILocatedUni)
			{
				checker.first = true;
				if (int.TryParse(parsedArgs.Raw[0], out int num))
				{ 
					checker.second = true;
                    foreach (var place in sortedPlaces)
					{
						if (place is ILocatedUni locatedUni && locatedUni.BuildingNumber != num)
							sortedPlaces.Remove(place);
					}
				}
			}

			int placesCounter = sortedPlaces.Count;
			Dictionary<int, int> indexPairs = [];
			for (int i = 0; i < placesCounter; ++i)
				indexPairs.Add(i, parsedArgs.Places.IndexOf(sortedPlaces[i]));

			await SendResponseAsync(DateTime.Now, user.UserID, msg.Text);
		}

		public async Task SendResponseAsync(Message msg, (bool first, bool second) checker, DateTime date, long userId, string text)
		{
			await Sender.EditOrSend(new(msg, "Выбор точки", new InlineKeyboardButton[][]
			{
				[($"{((checker.first && !checker.second) ? "Сортировка по корпусу" : (checker.second ? "Отключить сортировку" : ""))}", (args[0] == '-') ? $"/buildingNumberSelector {args[1..]}" : $"/placeSelector -{args[1]}")],
				[($"{((placesCounter != 0) ? sortedPlaces[nowCounter].Name : "")}", $"{((indexPairs.Count - 1) >= nowCounter ? $"/info {args[..2]}{indexPairs[nowCounter]}_{page}" : "/places")}")],
				[($"{((placesCounter > ++nowCounter) ? sortedPlaces[nowCounter].Name : "")}", $"{((indexPairs.Count - 1) >= nowCounter ? $"/info {args[..2]}{indexPairs[nowCounter]}_{page}" : "/places")}")],
				[($"{((placesCounter > ++nowCounter) ? sortedPlaces[nowCounter].Name : "")}", $"{((indexPairs.Count - 1) >= nowCounter ? $"/info {args[..2]}{indexPairs[nowCounter]}_{page}" : "/places")}")],
				[($"{((placesCounter > ++nowCounter) ? sortedPlaces[nowCounter].Name : "")}", $"{((indexPairs.Count - 1) >= nowCounter ? $"/info {args[..2]}{indexPairs[nowCounter]}_{page}" : "/places")}")],
				[($"{((placesCounter > ++nowCounter) ? sortedPlaces[nowCounter].Name : "")}", $"{((indexPairs.Count - 1) >= nowCounter ? $"/info {args[..2]}{indexPairs[nowCounter]}_{page}" : "/places")}")],
				[($"{((page != 0) ? "◀️" : "")}", $"/placeSelector {args[..2]}{page - 1}"), ("Назад","/places"), ($"{(placesCounter > nowCounter ? "▶️" : "")}", $"/placeSelector {args[..2]}{page + 1}")]
			}));
			Console.WriteLine($"{date} | {userId} | {text}");
		}
	}
}
