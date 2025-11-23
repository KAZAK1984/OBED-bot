using OBED.Common;
using OBED.Services;
using OBED.TelegramBot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OBED.Handlers
{
	public class SelectorHandler : ICommandHandler, IResponseSender
	{
		private static readonly string commandTag = "/selector";
		public bool CanHandle(string messageText) => messageText.StartsWith(commandTag);
		public async Task HandleAsync(Message message)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(message.Text);
			var user = await RegistrationService.TryGetOrRegisterUser(message);

			ParserService.ParsedArgs parsedArgs;
			try
			{
				parsedArgs = ParserService.ParseCommand(message.Text);
			}
			catch (InvalidDataException ex)
			{
				await Sender.EditOrSend(new(message, $"""
					Ошибка при обработке команды:
					{ex.Message}
					""", new InlineKeyboardButton[][]
					{
						[("Назад", "/start")]
					}, Telegram.Bot.Types.Enums.ParseMode.Html));

				await SendResponseAsync(DateTime.Now, user.UserID, $"ERR: {ex.Message}\n{message.Text}");
				return;
			}

			if (parsedArgs.Places.Count == 0)
			{
				await Sender.EditOrSend(new(message, "Выберите тип точки", new InlineKeyboardButton[][]
					{
						[("Столовые", "/selector BP:canteens")],
						[("Буфеты", "/selector BP:buffets")],
						[("Внешние магазины", "/selector BP:groceries")],
						[("Назад", "/start")]
					}));

				await SendResponseAsync(DateTime.Now, user.UserID, $"SUC: {message.Text}");
				return;
			}

			int page = parsedArgs.SelectorPage ?? 0;
			int pageElement = page * 5;
			List<BasePlace> sortedPlaces = [.. parsedArgs.Places.OrderByDescending(p => p.Reviews.Average(r => r.Rating))];

			bool canBeSorted = false, nowSorted = false;
			if (parsedArgs.Places[0] is ILocatedUni)
			{
				canBeSorted = true;
				if (parsedArgs.BildingNumber != null)
				{
					nowSorted = true;
					foreach (var place in sortedPlaces)
					{
						if (((ILocatedUni)place).BuildingNumber != parsedArgs.BildingNumber)
							sortedPlaces.Remove(place);
					}
				}
			}

			Dictionary<int, int> indexPairs = [];
			for (int i = 0; i < sortedPlaces.Count; ++i)
				indexPairs.Add(i, parsedArgs.Places.IndexOf(sortedPlaces[i]));

			int placesCounter = sortedPlaces.Count;
			await Sender.EditOrSend(new(message, "Выбор точки", new InlineKeyboardButton[][]
				{
					[($"{(canBeSorted ? (nowSorted ? "Отключить сортировку по корпусу" : "Включить сортировку по корпусу") : "")}", nowSorted ? $"/selector BP:{nameof(parsedArgs.Places)}" : $"/buildingSelector {nameof(parsedArgs.Places)}")],

					[($"{((placesCounter != 0)            ? sortedPlaces[pageElement].Name : "")}",
					$"{((indexPairs.Count - 1) >= pageElement ? $"/info BP:{nameof(parsedArgs.Places)} IN:{indexPairs[pageElement]} SP:{page} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}" : commandTag)}")],

					[($"{((placesCounter > ++pageElement) ? sortedPlaces[pageElement].Name : "")}",
					$"{((indexPairs.Count - 1) >= pageElement ? $"/info BP:{nameof(parsedArgs.Places)} IN:{indexPairs[pageElement]} SP:{page} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}" : commandTag)}")],

					[($"{((placesCounter > ++pageElement) ? sortedPlaces[pageElement].Name : "")}",
					$"{((indexPairs.Count - 1) >= pageElement ? $"/info BP:{nameof(parsedArgs.Places)} IN:{indexPairs[pageElement]} SP:{page} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}" : commandTag)}")],

					[($"{((placesCounter > ++pageElement) ? sortedPlaces[pageElement].Name : "")}",
					$"{((indexPairs.Count - 1) >= pageElement ? $"/info BP:{nameof(parsedArgs.Places)} IN:{indexPairs[pageElement]} SP:{page} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}" : commandTag)}")],

					[($"{((placesCounter > ++pageElement) ? sortedPlaces[pageElement].Name : "")}",
					$"{((indexPairs.Count - 1) >= pageElement ? $"/info BP:{nameof(parsedArgs.Places)} IN:{indexPairs[pageElement]} SP:{page} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}" : commandTag)}")],

					[
						($"{((page != 0) ? "◀️" : "")}", $"/placeSelector BP:{nameof(parsedArgs.Places)} PG:{page} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
						("Назад", commandTag),
						($"{(placesCounter > pageElement ? "▶️" : "")}", $"/placeSelector BP:{nameof(parsedArgs.Places)} PG:{page + 1} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}")
					]
				}));

			await SendResponseAsync(DateTime.Now, user.UserID, $"SUC: {message.Text}");
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}

	public class BuildingSelectorHandler : ICommandHandler, IResponseSender
	{
		public bool CanHandle(string messageText) => messageText.StartsWith("/buildingSelector");
		public async Task HandleAsync(Message message)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(message.Text);
			var user = await RegistrationService.TryGetOrRegisterUser(message);

			ParserService.ParsedArgs parsedArgs;
			try
			{
				parsedArgs = ParserService.ParseCommand(message.Text);
			}
			catch (InvalidDataException ex)
			{
				await Sender.EditOrSend(new(message, $"""
					Ошибка при обработке команды:
					{ex.Message}
					""", new InlineKeyboardButton[][]
					{
						[("Назад", "/start")]
					}, Telegram.Bot.Types.Enums.ParseMode.Html));
				await SendResponseAsync(DateTime.Now, user.UserID, $"{ex.Message}\n{message.Text}");
				return;
			}

			await Sender.EditOrSend(new(message, "Выбор точки", new InlineKeyboardButton[][]
				{
					[("1", $"/selector BP:{parsedArgs.Places} BN:1"), ("2", $"/selector BP:{parsedArgs.Places} BN:2"), ("3", $"/selector BP:{parsedArgs.Places} BN:3")],
					[("4", $"/selector BP:{parsedArgs.Places} BN:4"), ("5", $"/selector BP:{parsedArgs.Places} BN:5"), ("6", $"/selector BP:{parsedArgs.Places} BN:6")],
					[("ИАТУ", $"/selector BP:{parsedArgs.Places} BN:0"), ("На территории кампуса", $"/selector BP:{parsedArgs.Places} BN:7")],
					[("Назад", "/selector")]
				}));

			await SendResponseAsync(DateTime.Now, user.UserID, $"SUC: {message.Text}");
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}
}
