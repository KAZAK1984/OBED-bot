using OBED.Include;
using OBED.Services;
using OBED.TelegramBot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OBED.Handlers
{
	public class ReviewsHandler : ICommandHandler, IResponseSender
	{
		public bool CanHandle(string messageText) => messageText.StartsWith("/reviews");
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

			int page = parsedArgs.Page ?? 0;
			int pageElement = page * 5;

			BasePlace place = parsedArgs.Places[parsedArgs.Index.Value];

			if (parsedArgs.ReviewSortType == null)
				parsedArgs = parsedArgs with { ReviewSortType = ReviewSort.NewDate };

			List<Review>? sortedReviews = parsedArgs.ReviewSortType switch
			{
				ReviewSort.Upper => [.. place.Reviews.OrderByDescending(r => r.Rating)],
				ReviewSort.Lower => [.. place.Reviews.OrderBy(r => r.Rating)],
				ReviewSort.OldDate => [.. place.Reviews.OrderBy(r => r.Date)],
				ReviewSort.NewDate => [.. place.Reviews.OrderByDescending(r => r.Date)],
				_ => place.Reviews
			};

			await Sender.EditOrSend(new(message, $"""
				Название: {place.Name}
				Всего отзывов: {sortedReviews.Count}
				Всего отзывов с комментариями: {sortedReviews.Count(r => !string.IsNullOrWhiteSpace(r.Comment))}
				Режим сортировки: {parsedArgs.ReviewSortType}

				{(sortedReviews.Count > pageElement   ? $"{sortedReviews[pageElement].Rating}⭐ | {sortedReviews[pageElement].Comment}" : $"Отзывы с комментариями на \"{place.Name}\" не обнаружены")}
				{(sortedReviews.Count > ++pageElement ? $"{sortedReviews[pageElement].Rating}⭐ | {sortedReviews[pageElement].Comment}" : "")}
				{(sortedReviews.Count > ++pageElement ? $"{sortedReviews[pageElement].Rating}⭐ | {sortedReviews[pageElement].Comment}" : "")}
				{(sortedReviews.Count > ++pageElement ? $"{sortedReviews[pageElement].Rating}⭐ | {sortedReviews[pageElement].Comment}" : "")}
				{(sortedReviews.Count > ++pageElement ? $"{sortedReviews[pageElement].Rating}⭐ | {sortedReviews[pageElement].Comment}" : "")}
				""", new InlineKeyboardButton[][]
				{
					[
						(parsedArgs.ReviewSortType == ReviewSort.Upper   ? "" : "Оценка ↑", $"/reviews BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page} RT:{nameof(ProductType.MainDish)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
						(parsedArgs.ReviewSortType == ReviewSort.Lower   ? "" : "Оценка ↓", $"/reviews BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page} RT:{nameof(ProductType.SideDish)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
						(parsedArgs.ReviewSortType == ReviewSort.OldDate ? "" : "Новые",    $"/reviews BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page} RT:{nameof(ProductType.Drink)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
						(parsedArgs.ReviewSortType == ReviewSort.NewDate ? "" : "Старые",   $"/reviews BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page} RT:{nameof(ProductType.Appetizer)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
					],

					[
						((page != 0) ? "◀️" : "", $"/reviews BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page - 1} RT:{nameof(parsedArgs.ReviewSortType)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
						("Назад", $"/info BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
						(sortedReviews.Count > (pageElement + 1) ? "▶️" : "", $"/reviews BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page + 1} RT:{nameof(parsedArgs.ReviewSortType)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}")
					]

				}, Telegram.Bot.Types.Enums.ParseMode.Html));

			await SendResponseAsync(DateTime.Now, user.UserID, $"SUC: {message.Text}");
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}
}
