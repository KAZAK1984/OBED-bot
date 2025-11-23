namespace OBED.Common
{
	abstract class BasePlace(string name, string? description = null, List<Review>? reviews = null, List<Product>? menu = null)
	{
		public string Name { get; private set; } = name;
		public string? Description { get; private set; } = description;

		public List<Review> Reviews { get; private set; } = reviews ?? [];
		public List<Product> Menu { get; private set; } = menu ?? [];
		private static readonly object reviewLock = new();

		public virtual bool AddReview(Review review)
		{
			ArgumentNullException.ThrowIfNull(review);

			lock (reviewLock)
			{
				if (!Reviews.Any(x => x.UserID == review.UserID))
				{
					Reviews.Add(review);
					return true;
				}
				return false;
			}
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
	class Canteen(string name, int buildingNumber, int floor, string? description = null, List<Review>? reviews = null, List<Product>? menu = null) : BasePlace(name, description, reviews, menu), ILocatedUni
	{
		public int BuildingNumber { get; private set; } = buildingNumber;
		public int Floor { get; private set; } = floor;
	}
	class Buffet(string name, int buildingNumber, int floor, string? description = null, List<Review>? reviews = null, List<Product>? menu = null) : BasePlace(name, description, reviews, menu), ILocatedUni
	{
		public int BuildingNumber { get; private set; } = buildingNumber;
		public int Floor { get; private set; } = floor;
	}
	class Grocery(string name, string? description, List<Review>? reviews = null, List<Product>? menu = null) : BasePlace(name, description, reviews, menu) { }
}
