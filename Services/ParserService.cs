using OBED.Include;
using System.IO;

namespace OBED.Services
{
	class ParserService
	{
		//public static void ParseCommand(string command, out List<BasePlace>? places, out int? index, out int? page, 
		//    out int? sortedPage, out ReviewSort? reviewSortType, out ProductType? productSortType)
		//{

		//}
		public static void ParseCommand(string command, out List<BasePlace>? places, out int? page)
		{
			places = null;
			page = null;

			foreach(var arg in command.Split(' ', StringSplitOptions.RemoveEmptyEntries))
			{
				switch (arg[0])
				{
					case 'L':
						{
							places = GetPlaces(arg[1..]);
							break;
						}
					case 'P':
						{
							page = GetNum(arg[1..]);
							break;
						}
					default:
						{
							continue;
						}
				}
			}
		}
		public static void ParseCommand(string command, out List<BasePlace>? places, out int? index, out int? page, out int? sortedPage)
		{
			places = null;
			index = null;
			page = null;
			sortedPage = null;

			foreach (var arg in command.Split(' ', StringSplitOptions.RemoveEmptyEntries))
			{
				switch (arg[0])
				{
					case 'L':
						{
							places = GetPlaces(arg[1..]);
							break;
						}
					case 'I':
						{
							index = GetNum(arg[1..]);
							break;
						}
					case 'P':
						{
							page = GetNum(arg[1..]);
							break;
						}
					case 'S':
						{
							sortedPage = GetNum(arg[1..]);
							break;
						}
					default:
						{
							continue;
						}
				}
			}
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
				"MainDish" => ProductType.MainDish,
				"SideDish" => ProductType.SideDish,
				"Drink" => ProductType.Drink,
				"Appetizer" => ProductType.Appetizer,
				_ => null
			};
		}
	}
}
