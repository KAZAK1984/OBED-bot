using System.Collections.Concurrent;

namespace OBED.Include
{
    class ObjectLists
    {
        public static ConcurrentBag<Buffet> Buffets { get; private set; } = [];
        public static ConcurrentBag<Canteen> Canteens { get; private set; } = [];
        public static ConcurrentBag<Grocery> Groceries { get; private set; } = [];
		public static ConcurrentDictionary<long, Person> Persons { get; private set; } = [];

		/// <summary>
		/// Добавляет к общей базе новые точки или учётки.
		/// Для Person: дубликаты UserID игнорируются без ошибки.
		/// </summary>
		/// <param name="values">Лист с новыми точками/учётками.</param>
		/// <exception cref="ArgumentException">Ошибки.</exception>
		public static void AddRangeList<T>(List<T> values)
        {
			switch (values)
			{
				case (List<Buffet> buffets):
					{
						foreach (var buffet in buffets)
							Buffets.Add(buffet);
						break;
					}
				case (List<Canteen> canteens):
					{
						foreach (var canteen in canteens)
							Canteens.Add(canteen);
						break;
					}
				case (List<Grocery> groceries):
					{
						foreach (var grocery in groceries)
							Groceries.Add(grocery);
						break;
					}
				case (List<Person> persons):
					{
						foreach(var person in persons)
						{
							Persons.TryAdd(person.UserID, person);
						}
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
