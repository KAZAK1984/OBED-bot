using System.Collections.Concurrent;

namespace OBED.Include
{
    class ObjectLists
    {
        public static List<Buffet> Buffets { get; private set; } = [];
        public static List<Canteen> Canteens { get; private set; } = [];
        public static List<Grocery> Groceries { get; private set; } = [];
		public static ConcurrentDictionary<long, Person> Persons { get; } = [];

        public static void AddList<T>(List<T> values)
        {
			switch (values)
			{
				case List<Buffet> buffets:
					{
						Buffets = buffets;
						break;
					}
				case List<Canteen> canteens:
					{
						Canteens = canteens;
						break;
					}
				case List<Grocery> groceries:
					{
						Groceries = groceries;
						break;
					}
				default:
					{
						throw new ArgumentException("Попытка присвоить неизвестный тип", nameof(values));
					}
			}
		}
	}
}
