using OBED.Include;
using OBED.Services;
using OBED.TelegramBot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OBED.Handlers
{
	public class InfoHandler : ICommandHandler, IResponseSender
	{
		public bool CanHandle(string command) => command.StartsWith("/info");
		public async Task HandleAsync(Message msg)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(msg.Text);
			var user = await RegistrationService.TryGetOrRegisterUser(msg);

			ParserService.ParsedArgs parsedArgs;
			try
			{
				parsedArgs = ParserService.ParseCommand(msg.Text);
				if (parsedArgs.Places.Count == 0 || parsedArgs.Index == null)
					throw new Exception($"Не удалось обработать запрос \"{msg.Text}\": не удалось обработать тип и/или айди точки питания");
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

			int? sortNum = null;
			if (parsedArgs.Raw.Where(s => s.StartsWith("st")).Count() == 1 && int.TryParse(parsedArgs.Raw.Where(s => s.StartsWith("st")).First(), out var requestedNum))
				sortNum = requestedNum;

			BasePlace place = parsedArgs.Places[parsedArgs.Index.Value];
			await Sender.EditOrSend(new(msg, $"""
				Название: {place.Name}
				Средний рейтинг: {(place.Reviews.Count != 0 ? $"{place.Reviews.Average(r => r.Rating)}⭐" : "-")}
				Всего отзывов: {place.Reviews.Count}
				Последний текстовый отзыв: {(place.Reviews.Where(x => x.Comment != null).Any() ? $"{place.Reviews.Where(x => x.Comment != null).Last().Rating}⭐️| {place.Reviews.Where(x => x.Comment != null).Last().Comment}" : "Отзывы с комментариями не найдены")}
				""", new InlineKeyboardButton[][]
				{
					[("Меню", $"/menu BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} mp:{parsedArgs.Page}{(sortNum != null ? $" st:{sortNum}" : "")}")],
					[("Оставить отзыв", $"/sendReview BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} mp:{parsedArgs.Page}{(sortNum != null ? $" st:{sortNum}" : "")}"), ("Отзывы", $"/reviews BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} mp:{parsedArgs.Page}{(sortNum != null ? $" st:{sortNum}" : "")}")],
					[((user.Role == RoleType.Administrator && place.Reviews.Count != 0) ? "Панель удаления" : "", $"/admin BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} mp:{parsedArgs.Page}{(sortNum != null ? $" st:{sortNum}" : "")}")],
					[("Назад", $"/selector BP:{nameof(parsedArgs.Places)} PG:{parsedArgs.Page}{(sortNum != null ? $" st:{sortNum}" : "")}")]
				}, Telegram.Bot.Types.Enums.ParseMode.Html));

			await SendResponseAsync(DateTime.Now, user.UserID, msg.Text);
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}
}
