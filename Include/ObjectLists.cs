using System.Collections.Concurrent;

namespace OBED.Include
{
    static class ObjectLists
    {
        public static List<Buffet> Buffets { get; private set; } = [];
        public static List<Canteen> Canteens { get; private set; } = [];
        public static List<Grocery> Groceries { get; private set; } = [];
		public static ConcurrentDictionary<long, Person> Persons { get; private set; } = [];
		public static List<Report> Reports { get; private set; } = [];
        public static List<Complaint> Complaints { get; private set; } = [];

        /// <summary>
        /// Добавляет к общей базе новый лист точек или учёток.
        /// Для Person: дубликаты UserID игнорируются без ошибки.
        /// </summary>
        /// <param name="values">Лист с новыми точками/учётками.</param>
        /// <exception cref="ArgumentException">Ошибки.</exception>
        public static void AddRangeList<T>(List<T> values)
        {
			ArgumentNullException.ThrowIfNull(values);

			switch (values)
			{
				case (List<Buffet> buffets):
					{
						foreach (var buffet in buffets.AsEnumerable().Reverse())
							Buffets.Add(buffet);
						break;
					}
				case (List<Canteen> canteens):
					{
						foreach (var canteen in canteens.AsEnumerable().Reverse())
							Canteens.Add(canteen);
						break;
					}
				case (List<Grocery> groceries):
					{
						foreach (var grocery in groceries.AsEnumerable().Reverse())
							Groceries.Add(grocery);
						break;
					}
				case (List<Person> persons):
					{
						foreach(var person in persons.AsEnumerable().Reverse())
							Persons.TryAdd(person.UserID, person);
						break;
					}
				case (List<Report> reports):
					{
                        foreach (var report in reports.AsEnumerable().Reverse())
                            Reports.Add(report);
                        break;
                    }
                case (List<Complaint> complaints):
                    {
                        foreach (var complaint in complaints.AsEnumerable().Reverse())
                            Complaints.Add(complaint);
                        break;
                    }
                default:
					throw new ArgumentException($"Тип списка не поддерживается: {typeof(T).Name}", nameof(values));
			}
		}
	}
}
