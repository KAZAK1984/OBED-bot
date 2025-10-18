namespace OBED.Include
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

	abstract class BasePlace(string name, string? description = null, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null)
	{
		public string Name { get; private set; } = name;
		public string? Description { get; private set; } = description;

		public List<Review> Reviews { get; private set; } = reviews ?? [];
		public List<Product> Menu { get; private set; } = menu ?? [];
		public List<string> Tegs { get; private set; } = tegs ?? []; // TODO: Возможное изменение типа на enum
																	 // TODO: public List<T> photos []
		private static readonly object reviewLock = new();

		// TODO: Загрузка с бд/файла
		//abstract public void Load(string file);
		//abstract public void Save(string file);
		public virtual bool AddReview(Review review)
		{
			ArgumentNullException.ThrowIfNull(review);

			if (!Reviews.Any(x => x.UserID == review.UserID))
			{
				Reviews.Add(review);
				return true;
			}
			return false;
		}
		public virtual bool AddReview(long userID, int rating, string? comment)
		{
			lock (reviewLock)
			{
				if (!Reviews.Any(x => x.UserID == userID))
				{
					Reviews.Add(new Review(userID, rating, comment));
					return true;
				}
				return false;
			}
		}
		public virtual bool DeleteReview(long userID)
		{
			var reviewToRemove = Reviews.FirstOrDefault(x => x.UserID == userID);

			lock (reviewLock)
			{
				if (reviewToRemove != null)
				{
					Reviews.Remove(reviewToRemove);
					return true;
				}
				return false;
			}
		}
		public virtual Review? GetReview(long userID) => Reviews.FirstOrDefault(x => x.UserID == userID);
	}
}
