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

			ParserService.ParsedArgs parsedArgs;
			try
			{
				parsedArgs = ParserService.ParseCommand(msg.Text);
			}
			catch (Exception ex)
			{
				await Sender.EditOrSend(new(msg, $"""
					Ошибка при обработке команды:
					{ex.Message}
					""", new InlineKeyboardButton[][]
					{
						[("Назад", "/start")]
					}, Telegram.Bot.Types.Enums.ParseMode.Html));

				await SendResponseAsync(DateTime.Now, user.UserID, $"{ex.Message}\n{msg.Text}");
				return;
			}

			if (parsedArgs.Places.Count == 0)
			{
				await Sender.EditOrSend(new(msg, "Выберите тип точки", new InlineKeyboardButton[][]
					{
						[("Столовые", "/selector BP:canteens")],
						[("Буфеты", "/selector BP:buffets")],
						[("Внешние магазины", "/selector BP:groceries")],
						[("Назад", "/start")]
					}));

				await SendResponseAsync(DateTime.Now, user.UserID, msg.Text);
				return;
			}

			int page = parsedArgs.Page ?? 0;
			int pageElement = page * 5;
			List<BasePlace> sortedPlaces = [.. parsedArgs.Places.OrderByDescending(p => p.Reviews.Average(r => r.Rating))];

			int requestedNum = 0;
			bool canBeSorted = false, nowSorted = false;
			if (parsedArgs.Places.First() is ILocatedUni)
			{
				canBeSorted = true;
				if (parsedArgs.Raw.Where(s => s.StartsWith("st")).Count() == 1 && int.TryParse(parsedArgs.Raw.Where(s => s.StartsWith("st")).First(), out requestedNum))
				{
					nowSorted = true;
					foreach (var place in sortedPlaces)
					{
						if (((ILocatedUni)place).BuildingNumber != requestedNum)
							sortedPlaces.Remove(place);
					}
				}
			}

			Dictionary<int, int> indexPairs = [];
			for (int i = 0; i < sortedPlaces.Count; ++i)
				indexPairs.Add(i, parsedArgs.Places.IndexOf(sortedPlaces[i]));

			int placesCounter = sortedPlaces.Count;
			await Sender.EditOrSend(new(msg, "Выбор точки", new InlineKeyboardButton[][]
				{
					[($"{(canBeSorted ? (nowSorted ? "Отключить сортировку по корпусу" : "Включить сортировку по корпусу") : "")}", nowSorted ? $"/selector BP:{nameof(parsedArgs.Places)}" : $"/buildingSelector {nameof(parsedArgs.Places)}")],
					[($"{((placesCounter != 0)            ? sortedPlaces[pageElement].Name : "")}", $"{((indexPairs.Count - 1) >= pageElement ? $"/info BP:{nameof(parsedArgs.Places)} IN:{indexPairs[pageElement]} PG:{page}{(nowSorted ? $" st:{requestedNum}" : "")}" : "/selector")}")],
					[($"{((placesCounter > ++pageElement) ? sortedPlaces[pageElement].Name : "")}", $"{((indexPairs.Count - 1) >= pageElement ? $"/info BP:{nameof(parsedArgs.Places)} IN:{indexPairs[pageElement]} PG:{page}{(nowSorted ? $" st:{requestedNum}" : "")}" : "/selector")}")],
					[($"{((placesCounter > ++pageElement) ? sortedPlaces[pageElement].Name : "")}", $"{((indexPairs.Count - 1) >= pageElement ? $"/info BP:{nameof(parsedArgs.Places)} IN:{indexPairs[pageElement]} PG:{page}{(nowSorted ? $" st:{requestedNum}" : "")}" : "/selector")}")],
					[($"{((placesCounter > ++pageElement) ? sortedPlaces[pageElement].Name : "")}", $"{((indexPairs.Count - 1) >= pageElement ? $"/info BP:{nameof(parsedArgs.Places)} IN:{indexPairs[pageElement]} PG:{page}{(nowSorted ? $" st:{requestedNum}" : "")}" : "/selector")}")],
					[($"{((placesCounter > ++pageElement) ? sortedPlaces[pageElement].Name : "")}", $"{((indexPairs.Count - 1) >= pageElement ? $"/info BP:{nameof(parsedArgs.Places)} IN:{indexPairs[pageElement]} PG:{page}{(nowSorted ? $" st:{requestedNum}" : "")}" : "/selector")}")],
					[($"{((page != 0) ? "◀️" : "")}", $"/placeSelector BP:{nameof(parsedArgs.Places)} PG:{page}{(nowSorted ? $" st:{requestedNum}" : "")}"), ("Назад","/selector"), ($"{(placesCounter > pageElement ? "▶️" : "")}", $"/placeSelector BP:{nameof(parsedArgs.Places)} PG:{page + 1}{(nowSorted ? $" st:{requestedNum}" : "")}")]
				}, Telegram.Bot.Types.Enums.ParseMode.Html));

			await SendResponseAsync(DateTime.Now, user.UserID, msg.Text);
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}

	public class BuildingSelectorHandler : ICommandHandler, IResponseSender
	{
		public bool CanHandle(string command) => command.StartsWith("/buildingSelector");
		public async Task HandleAsync(Message msg)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(msg.Text);
			var user = await RegistrationService.TryGetOrRegisterUser(msg);

			ParserService.ParsedArgs parsedArgs;
			try
			{
				parsedArgs = ParserService.ParseCommand(msg.Text);
			}
			catch (Exception ex)
			{
				await Sender.EditOrSend(new(msg, $"""
					Ошибка при обработке команды:
					{ex.Message}
					""", new InlineKeyboardButton[][]
					{
						[("Назад", "/start")]
					}, Telegram.Bot.Types.Enums.ParseMode.Html));
					await SendResponseAsync(DateTime.Now, user.UserID, $"{ex.Message}\n{msg.Text}");
				return;
			}

			await Sender.EditOrSend(new(msg, "Выбор точки", new InlineKeyboardButton[][]
				{
					[("1", $"/selector BP:{parsedArgs.Places} st:1"), ("2", $"/selector BP:{parsedArgs.Places} st:2"), ("3", $"/selector BP:{parsedArgs.Places} st:3")],
					[("4", $"/selector BP:{parsedArgs.Places} st:4"), ("5", $"/selector BP:{parsedArgs.Places} st:5"), ("6", $"/selector BP:{parsedArgs.Places} st:6")],
					[("ИАТУ", $"/selector BP:{parsedArgs.Places} st:0"), ("На территории кампуса", $"/selector BP:{parsedArgs.Places} st:7")],
					[("Назад", "/selector")]
				}));

			await SendResponseAsync(DateTime.Now, user.UserID, msg.Text);
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}
}
