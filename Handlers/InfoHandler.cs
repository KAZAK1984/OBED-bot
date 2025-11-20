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
				if (parsedArgs.Places.Count == 0 || parsedArgs.Index == null || parsedArgs.SelectorPage == null)
					throw new Exception($"Не удалось обработать запрос \"{msg.Text}\": не удалось обнаружить тип и/или айди точки питания и/или параметры прошлых меню");
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

				await SendResponseAsync(DateTime.Now, user.UserID, $"ERR: {ex.Message} - {msg.Text}");
				return;
			}

			BasePlace place = parsedArgs.Places[parsedArgs.Index.Value];
			await Sender.EditOrSend(new(msg, $"""
				Название: {place.Name}
				Средний рейтинг: {(place.Reviews.Count != 0 ? $"{place.Reviews.Average(r => r.Rating)}⭐" : "-")}
				Всего отзывов: {place.Reviews.Count}
				Последний текстовый отзыв: {(place.Reviews.Where(x => x.Comment != null).Any() ? $"{place.Reviews.Where(x => x.Comment != null).Last().Rating}⭐️| {place.Reviews.Where(x => x.Comment != null).Last().Comment}" : "Отзывы с комментариями не найдены")}
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

			await SendResponseAsync(DateTime.Now, user.UserID, $"SUC: {msg.Text}");
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}
}
