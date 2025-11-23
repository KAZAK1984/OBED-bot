using OBED.Common;
using OBED.Services;
using OBED.TelegramBot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OBED.Handlers
{
	public class InfoHandler : ICommandHandler, IResponseSender
	{
		public bool CanHandle(string messageText) => messageText.StartsWith("/info");
		public async Task HandleAsync(Message message)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(message.Text);
			var user = await RegistrationService.TryGetOrRegisterUser(message);

			ParserService.ParsedArgs parsedArgs;
			try
			{
				parsedArgs = ParserService.ParseCommand(message.Text);
				if (parsedArgs.Places.Count == 0 || parsedArgs.Index == null || parsedArgs.SelectorPage == null)
					throw new InvalidDataException($"Не удалось обработать запрос \"{message.Text}\": не удалось обнаружить тип и/или айди точки питания и/или параметры прошлых меню");
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

				await SendResponseAsync(DateTime.Now, user.UserID, $"ERR: {ex.Message} - {message.Text}");
				return;
			}

			BasePlace place = parsedArgs.Places[parsedArgs.Index.Value];
			await Sender.EditOrSend(new(message, $"""
				Название: {place.Name}
				Средний рейтинг: {(place.Reviews.Count != 0 ? $"{place.Reviews.Average(r => r.Rating)}⭐" : "-")}
				Всего отзывов: {place.Reviews.Count}
				Последний текстовый отзыв: {(place.Reviews.Any(x => x.Comment != null) ? $"{place.Reviews.Last(x => x.Comment != null).Rating}⭐️| {place.Reviews.Last(x => x.Comment != null).Comment}" : "Отзывы с комментариями не найдены")}
				""", new InlineKeyboardButton[][]
				{
					[("Меню", $"/menu BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}")],

					[
						("Оставить отзыв", $"/sendReview BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber}"),
						("Отзывы", $"/reviews BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}")
					],

					[((user.Role == RoleType.Administrator && place.Reviews.Count != 0) ? "Панель удаления" : "", $"/admin BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}")],

					[("Назад", $"/selector BP:{nameof(parsedArgs.Places)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}")]
				}, Telegram.Bot.Types.Enums.ParseMode.Html));

			await SendResponseAsync(DateTime.Now, user.UserID, $"SUC: {message.Text}");
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}
}
