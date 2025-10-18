using System.Collections.Concurrent;

namespace OBED.Include
{
    class ObjectLists
    {
        public static List<Buffet> Buffets { get; private set; } = [];
        public static List<Canteen> Canteens { get; private set; } = [];
        public static List<Grocery> Groceries { get; private set; } = [];
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
						Buffets.AddRange(buffets);
						break;
					}
				case (List<Canteen> canteens):
					{
						Canteens.AddRange(canteens);
						break;
					}
				case (List<Grocery> groceries):
					{
						Groceries.AddRange(groceries);
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
