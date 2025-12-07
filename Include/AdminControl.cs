using Microsoft.Data.Sqlite;
using System.Xml.Linq;
using Telegram.Bot.Types;

namespace OBED.Include
{
	static class AdminControl
	{
		public static List<(Review review, BasePlace place)> ReviewCollector { get; private set; } = CollectReviewsOnMod();
		private static readonly object adminControlLock = new();

		public static List<(Review review, BasePlace place)> CollectReviewsOnMod()
		{
			string dbConnectionString = "Data Source=OBED_DB.db";
			List<(Review review, BasePlace place)> list = [];
			using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				using(SqliteCommand command = new SqliteCommand())
				{
					command.Connection = connection;
					command.CommandText = @"SELECT * FROM Reviews JOIN Places ON Reviews.Place_id = Places.Place_id WHERE OnMod = 1";
					using(SqliteDataReader reader =  command.ExecuteReader())
					{
						while (reader.Read())
						{
							long userid = reader.GetInt64(reader.GetOrdinal("Users_id"));
							long placeid = reader.GetInt64(reader.GetOrdinal("Place_id"));
							int rating = reader.GetInt32(reader.GetOrdinal("Rating"));
							int commentOrdinal = reader.GetOrdinal("Comment");
							string ? comment = reader.IsDBNull(commentOrdinal) ? null : reader.GetString(commentOrdinal);
							DateTime time = reader.GetDateTime(reader.GetOrdinal("Date"));
							int type = reader.GetInt32(reader.GetOrdinal("Type"));
							Review review = new Review(placeid, userid, rating, comment, time);
							switch (type)
							{
								case 1:
									{
										var place = ObjectLists.Buffets.FirstOrDefault(x => x.Place_id == placeid);
										if (place != null)
											list.Add((review, place));
										break;
									}
								case 2:
									{
										var place = ObjectLists.Canteens.FirstOrDefault(x => x.Place_id == placeid);
										if (place != null)
											list.Add((review, place));
										break;
									}
								case 3:
									{
										var place = ObjectLists.Groceries.FirstOrDefault(x => x.Place_id == placeid);
										if (place != null)
											list.Add((review, place));
										break;
									}
							}
						}
					}
				}
			}
			return list;
		}

		public static bool AddReviewOnMod(BasePlace place, Review review)
		{
			ArgumentNullException.ThrowIfNull(review);
			ArgumentNullException.ThrowIfNull(place);
			if (review.Comment == null)
				return place.AddReview(review,0);

			lock (adminControlLock)
			{
				if (!place.Reviews.Any(x => x.UserID == review.UserID) && !ReviewCollector.Any(x => x.review.UserID == review.UserID))
				{
					place.Save(review,1);
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
				return place.AddReview(new Review(place.Place_id,userID, rating, comment),0);

			lock (adminControlLock)
			{
				if (!place.Reviews.Any(x => x.UserID == userID) && !ReviewCollector.Any(x => x.review.UserID == userID))
				{
					place.Save(new Review(place.Place_id,userID,rating,comment), 1);
					ReviewCollector.Add((new Review(place.Place_id,userID, rating, comment), place));
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
					ReviewCollector[index].place.AddReview(ReviewCollector[index].review,0);
				else
					ReviewCollector[index].place.RemoveReviewFromBD(ReviewCollector[index].review);

				ReviewCollector.RemoveAt(index);
			}
		}

		public static void SetReviewStatus(string censorStr, int index = 0)
		{
			lock (adminControlLock)
			{
				if (index < 0 || index >= ReviewCollector.Count)
					throw new InvalidDataException($"index {index} должен быть в рамках ReviewCollector ({ReviewCollector.Count})");

				ReviewCollector[index].place.AddReview(ReviewCollector[index].place.Place_id,ReviewCollector[index].review.UserID,
					ReviewCollector[index].review.Rating, censorStr,0);

				ReviewCollector.RemoveAt(index);
			}
		}
	}
}
