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

		/// <summary>
		/// Initializes a new Review with the specified user ID, rating, optional comment, and optional date.
		/// </summary>
		/// <param name="userID">The identifier of the user who created the review; must be greater than 0.</param>
		/// <param name="rating">The review rating from 1 to 10.</param>
		/// <param name="comment">An optional text comment; may be null.</param>
		/// <param name="date">An optional review date; when null, the current date and time are used.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="userID"/> is less than or equal to 0.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rating"/> is not between 1 and 10.</exception>
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
		/// <summary>
		/// Adds the specified review to the place when there is no existing review from the same user.
		/// </summary>
		/// <param name="review">The review to add.</param>
		/// <returns>`true` if the review was added; `false` if a review from the same user already exists.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="review"/> is null.</exception>
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
		/// <summary>
		/// Adds a new review for the specified user if that user has no existing review.
		/// </summary>
		/// <param name="userID">The identifier of the user; must be greater than 0.</param>
		/// <param name="rating">The review rating; must be between 1 and 10 inclusive.</param>
		/// <param name="comment">Optional review comment.</param>
		/// <returns>`true` if the review was added, `false` if a review by the same user already exists.</returns>
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
		/// <summary>
		/// Removes the first review for the specified user.
		/// </summary>
		/// <param name="userID">The identifier of the user whose review should be removed.</param>
		/// <returns>`true` if a review by the specified user was found and removed, `false` otherwise.</returns>
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
		/// <summary>
/// Retrieves the first review created by the specified user.
/// </summary>
/// <param name="userID">The identifier of the user whose review to find.</param>
/// <returns>The first <see cref="Review"/> with a matching <c>UserID</c>, or <c>null</c> if none exists.</returns>
public virtual Review? GetReview(long userID) => Reviews.FirstOrDefault(x => x.UserID == userID);
	}
}