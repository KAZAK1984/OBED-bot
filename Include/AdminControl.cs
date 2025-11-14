namespace OBED.Include
{
	static class AdminControl
	{
		public static List<(Review review, BasePlace place)> ReviewCollector { get; private set; } = [];
		private static readonly object adminControlLock = new();

		public static bool AddReviewOnMod(BasePlace place, Review review)
		{
			ArgumentNullException.ThrowIfNull(review);
			ArgumentNullException.ThrowIfNull(place);
			if (review.Comment == null)
				return place.AddReview(review);

			lock (adminControlLock)
			{
				if (!place.Reviews.Any(x => x.UserID == review.UserID) || !ReviewCollector.Any(x => x.review.UserID == review.UserID))
				{
					ReviewCollector.Add((review, place));
					return true;
				}
				return false;
			}
		}
		public static bool AddReviewOnMod(BasePlace place, long userID, int rating, string? comment)
		{
			ArgumentNullException.ThrowIfNull(place);
			if (comment == null)
				return place.AddReview(new Review(userID, rating, comment));

			lock (adminControlLock)
			{
				if (!place.Reviews.Any(x => x.UserID == userID) || !ReviewCollector.Any(x => x.review.UserID == userID))
				{
					ReviewCollector.Add((new Review(userID, rating, comment), place));
					return true;
				}
				return false;
			}
		}
		public static void SetReviewStatus(bool status = false, int index = 0)
		{
			lock (adminControlLock)
			{
				if (index < 0 || index >= ReviewCollector.Count)
					throw new InvalidDataException($"index {index} должен быть в рамках ReviewCollector ({ReviewCollector.Count})");

				if (status)
					ReviewCollector[index].place.AddReview(ReviewCollector[index].review);

				ReviewCollector.RemoveAt(index);
			}
		}

		public static void SetReviewStatus(string censorStr, int index = 0)
		{
			lock (adminControlLock)
			{
				if (index < 0 || index >= ReviewCollector.Count)
					throw new InvalidDataException($"index {index} должен быть в рамках ReviewCollector ({ReviewCollector.Count})");

				ReviewCollector[index].place.AddReview(ReviewCollector[index].review.UserID,
					ReviewCollector[index].review.Rating, censorStr);

				ReviewCollector.RemoveAt(index);
			}
		}
	}
}
