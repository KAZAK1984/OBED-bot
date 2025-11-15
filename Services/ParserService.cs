using OBED.Include;

namespace OBED.Services
{
	class ParserService
	{
		public sealed record ParsedArgs
		(
			List<string> Raw,
			List<BasePlace> Places,
			int? Page = null,
			int? Index = null,
			int? SortedPage = null,
			ReviewSort? ReviewSortType = null,
			ProductType? ProductSortType = null
		);

		public static ParsedArgs ParseCommand(string command)
		{
			var parsedArgs = new ParsedArgs(Places: [], Raw: []);
			var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1);
			if (!args.Any())
				return parsedArgs;

			foreach (var arg in args)
			{
				var token = arg;
				if (token.Length > 2 && token[2] == ':')
					token = token.Remove(2, 1); // Убираем двоеточие после кода аргумента, если есть, нужно для повышения читаемости кода при записи команды

				if (token.Length < 3)
					throw new InvalidDataException($"Обнаружен некорректный аргумент {token} в запросе: {command}");
				var prefix = token[..2];
				var payload = token[2..];

				switch (prefix)
				{		
					case "BP":	// List<BasePlace>
						{
							if (parsedArgs.Places!.Count != 0)
								throw new InvalidDataException($"Обнаружена попытка повторного заполения <BasePlace> в запросе: {command}");
							parsedArgs.Places.AddRange(GetPlaces(payload) ?? throw new InvalidDataException($"Обнаружена попытка запроса к несуществующему типу {payload} в запросе: {command}"));
							break;
						}
					case "PG":	// Page
						{
							if (parsedArgs.Page != null)
								throw new InvalidDataException($"Обнаружена попытка повторного заполения <Page> в запросе: {command}");
							var pageNum = GetNum(payload) ?? throw new InvalidDataException($"Обнаружена попытка при обработке <Page> {payload} в запросе: {command}");
							parsedArgs = parsedArgs with { Page = pageNum };
							break;
						}
					case "IN":	// Index
						{
							if (parsedArgs.Index != null)
								throw new InvalidDataException($"Обнаружена попытка повторного заполения <Index> в запросе: {command}");
							var indexNum = GetNum(payload) ?? throw new InvalidDataException($"Обнаружена попытка при обработке <Index> {payload} в запросе: {command}");
							parsedArgs = parsedArgs with { Index = indexNum };
							break;
						}
					case "SP":	// SortedPage
						{
							if (parsedArgs.SortedPage != null)
								throw new InvalidDataException($"Обнаружена попытка повторного заполения <SortedPage> в запросе: {command}");
							var sPageNum = GetNum(payload) ?? throw new InvalidDataException($"Обнаружена попытка при обработке <SortedPage> {payload} в запросе: {command}");
							parsedArgs = parsedArgs with { SortedPage = sPageNum };
							break;
						}
					case "RT":	// ReviewSortType
						{
							if (parsedArgs.ReviewSortType != null)
								throw new InvalidDataException($"Обнаружена попытка повторного заполения <ReviewSortType> в запросе: {command}");
							var sortType = GetReviewSortType(payload) ?? throw new InvalidDataException($"Обнаружена попытка запроса к несуществующему типу {payload} в запросе: {command}");
							parsedArgs = parsedArgs with { ReviewSortType = sortType };
							break;
						}
					case "PT":	// ProductSortType
						{
							if (parsedArgs.ProductSortType != null)
								throw new InvalidDataException($"Обнаружена попытка повторного заполения <ProductType> в запросе: {command}");
							var sortType = GetProductSortType(payload) ?? throw new InvalidDataException($"Обнаружена попытка запроса к несуществующему типу {payload} в запросе: {command}");
							parsedArgs = parsedArgs with { ProductSortType = sortType };
							break;
						}
					default:    // Raw
						{       // Обозначайте кастомные передаваемые типы строго маленькими буквами, чтобы не возникало конфликтов с основными типами
							if (token.Length > 1)
								parsedArgs.Raw!.Add(token); // Передаём аргумент как есть
							break;
						}
				}
			}

			return parsedArgs;
		}

		public static List<BasePlace>? GetPlaces(string type)
		{
			return type.Trim().ToLower() switch
			{
				"canteens" => [.. ObjectLists.Canteens.Cast<BasePlace>()],
				"buffets" => [.. ObjectLists.Buffets.Cast<BasePlace>()],
				"groceries" => [.. ObjectLists.Groceries.Cast<BasePlace>()],
				_ => null
			};
		}
		public static int? GetNum(string numStr)
		{
			if (int.TryParse(numStr, out int num))
				return num;

			return null;
		}
		public static ReviewSort? GetReviewSortType(string sortStr)
		{
			return sortStr.Trim().ToLower() switch
			{
				"upper" => ReviewSort.Upper,
				"lower" => ReviewSort.Lower,
				"olddate" => ReviewSort.OldDate,
				"newdate" => ReviewSort.NewDate,
				_ => null
			};
		}
		public static ProductType? GetProductSortType(string sortStr)
		{
			return sortStr.ToLower().Trim() switch
			{
				"mainDish" => ProductType.MainDish,
				"sideDish" => ProductType.SideDish,
				"drink" => ProductType.Drink,
				"appetizer" => ProductType.Appetizer,
				_ => null
			};
		}	
	}
}
