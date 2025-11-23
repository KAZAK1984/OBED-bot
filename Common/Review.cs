namespace OBED.Common
{
	/// <summary>
	/// Тип сортировки отзывов.
	/// </summary>
	public enum ReviewSort
	{
		/// <summary>Сортировка по убыванию рейтинга.</summary>
		Upper,
		/// <summary>Сортировка по возрастанию рейтинга.</summary>
		Lower,
		/// <summary>Сортировка от старых к новым.</summary>
		OldDate,
		/// <summary>Сортировка от новых к старым. Ставится по умолчанию.</summary>
		NewDate
	}
	class Review
	{
		public long UserID { get; init; }
		public int Rating { get; private set; }
		public string? Comment { get; private set; }
		public DateTime Date { get; private set; }

		public Review(long userID, int rating, string? comment = null, DateTime? date = null)
		{
			if (userID <= 0)
				throw new ArgumentException("UserID должно быть больше 0", nameof(userID));
			if (rating < 1 || rating > 10)
				throw new ArgumentOutOfRangeException(nameof(rating), "Рейтинг должен быть от 1 до 10");

			UserID = userID;
			Rating = rating;
			Comment = comment;
			Date = date ?? DateTime.Now;
		}
	}
}
