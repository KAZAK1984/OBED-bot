using OBED.Include;
using OBED.Services;
using OBED.TelegramBot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OBED.Handlers
{
	public class MenuHandler : ICommandHandler, IResponseSender
	{
		private static readonly string byGramTag = "100 грамм";
		private static readonly string byPortionTag = "порцию";

		public bool CanHandle(string messageText) => messageText.StartsWith("/menu");
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

				await SendResponseAsync(DateTime.Now, user.UserID, $"ERR: {ex.Message}\n{message.Text}");
				return;
			}

			int page = parsedArgs.Page ?? 0;
			int pageElement = page * 10;

			BasePlace place = parsedArgs.Places[parsedArgs.Index.Value];
			List<Product>? sortedProduct = null;
			ProductType? productType = parsedArgs.ProductSortType;
			if (productType != null)
				sortedProduct = [.. place.Menu.Where(p => p.Type == productType)];
			int productCounter = sortedProduct != null ? sortedProduct.Count : 0;

			await Sender.EditOrSend(new(message, $"""
				Название: {place.Name}
				Всего позиций в меню: {productCounter}
				{(productType != null ? $"Режим сортировки: {productType}\n" : "")}
				{(productCounter > pageElement	 ? $"{sortedProduct![pageElement].Name} | {sortedProduct![pageElement].Price.value} за {(sortedProduct![pageElement].Price.perGram ? byGramTag : byPortionTag)}" : $"{(productType == null ? $"Меню \"{place.Name}\" не обнаружено" : $"Позиций по тегу \"{productType}\" не обнаружено")}")}
				{(productCounter > ++pageElement ? $"{sortedProduct![pageElement].Name} | {sortedProduct![pageElement].Price.value} за {(sortedProduct![pageElement].Price.perGram ? byGramTag : byPortionTag)}" : "")}
				{(productCounter > ++pageElement ? $"{sortedProduct![pageElement].Name} | {sortedProduct![pageElement].Price.value} за {(sortedProduct![pageElement].Price.perGram ? byGramTag : byPortionTag)}" : "")}
				{(productCounter > ++pageElement ? $"{sortedProduct![pageElement].Name} | {sortedProduct![pageElement].Price.value} за {(sortedProduct![pageElement].Price.perGram ? byGramTag : byPortionTag)}" : "")}
				{(productCounter > ++pageElement ? $"{sortedProduct![pageElement].Name} | {sortedProduct![pageElement].Price.value} за {(sortedProduct![pageElement].Price.perGram ? byGramTag : byPortionTag)}" : "")}
				{(productCounter > ++pageElement ? $"{sortedProduct![pageElement].Name} | {sortedProduct![pageElement].Price.value} за {(sortedProduct![pageElement].Price.perGram ? byGramTag : byPortionTag)}" : "")}
				{(productCounter > ++pageElement ? $"{sortedProduct![pageElement].Name} | {sortedProduct![pageElement].Price.value} за {(sortedProduct![pageElement].Price.perGram ? byGramTag : byPortionTag)}" : "")}
				{(productCounter > ++pageElement ? $"{sortedProduct![pageElement].Name} | {sortedProduct![pageElement].Price.value} за {(sortedProduct![pageElement].Price.perGram ? byGramTag : byPortionTag)}" : "")}
				{(productCounter > ++pageElement ? $"{sortedProduct![pageElement].Name} | {sortedProduct![pageElement].Price.value} за {(sortedProduct![pageElement].Price.perGram ? byGramTag : byPortionTag)}" : "")}
				{(productCounter > ++pageElement ? $"{sortedProduct![pageElement].Name} | {sortedProduct![pageElement].Price.value} за {(sortedProduct![pageElement].Price.perGram ? byGramTag : byPortionTag)}" : "")}
				""", new InlineKeyboardButton[][]
				{
					[(productType == null ? "" : "Без сортировки", $"/menu BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{parsedArgs.Page} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}")],

					[
						(productType == ProductType.MainDish  ? "" : "Блюда",	$"/menu BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page} PT:{nameof(ProductType.MainDish)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
						(productType == ProductType.SideDish  ? "" : "Гарниры", $"/menu BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page} PT:{nameof(ProductType.SideDish)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
						(productType == ProductType.Drink     ? "" : "Напитки", $"/menu BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page} PT:{nameof(ProductType.Drink)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
						(productType == ProductType.Appetizer ? "" : "Закуски", $"/menu BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page} PT:{nameof(ProductType.Appetizer)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
					],

					[
						((page != 0) ? "◀️" : "", $"/menu BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page - 1} PT:{nameof(parsedArgs.ProductSortType)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
						("Назад", $"/info BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}"),
						(productCounter > (pageElement + 1) ? "▶️" : "", $"/menu BP:{nameof(parsedArgs.Places)} IN:{parsedArgs.Index} PG:{page + 1} PT:{nameof(parsedArgs.ProductSortType)} SP:{parsedArgs.SelectorPage} BN:{parsedArgs.BildingNumber.ToString() ?? "-"}")
					]
				}, Telegram.Bot.Types.Enums.ParseMode.Html));

			await SendResponseAsync(DateTime.Now, user.UserID, $"SUC: {message.Text}");
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}
}
