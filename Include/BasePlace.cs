using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Xml.Linq;
using Telegram.Bot.Types;

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
    class PlaceData
    {
		public string? Name { get; set; } = "Unknown";
		public int Corpus { get;  set; }
		public int Floor { get;  set; }
		public string? Description { get;  set; } = "No Description";
    }

    class Review
	{
		public long Place_Id { get; private set; }
		public long UserID { get; init; }
		public int Rating { get; private set; }
		public string? Comment { get; private set; }

		public DateTime? Date { get; private set; }

		public Review(long placeid,long userID, int rating, string? comment = null, DateTime? date = null)
		{
			if (userID <= 0)
				throw new ArgumentException("UserID должно быть больше 0", nameof(userID));
			if (rating < 1 || rating > 10)
				throw new ArgumentOutOfRangeException(nameof(rating), "Рейтинг должен быть от 1 до 10");

			Place_Id = placeid;
			UserID = userID;
			Rating = rating;
			Comment = comment;
			Date = date ?? DateTime.Now;
		}
	}

	abstract class BasePlace(long placeid,string name, string? description = null, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null)
	{
        private static readonly string dbConnectionString = "Data Source=OBED_DB.db";
        public long Place_id { get; private set; } = placeid;
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

		public virtual bool Save(Review review,int mod)
		{
			if (review.UserID <= 0)
				throw new ArgumentException("UserID должно быть больше 0", nameof(review.UserID));
			if (review.Rating < 1 || review.Rating > 10)
				throw new ArgumentException("Рейтинг должен быть от 1 до 10", nameof(review.Rating));
			using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				using (var command = new SqliteCommand())
				{
					command.Connection = connection;
					//Создание таблицы если её нету
					CreateTableReviews(command);
					if (IfUserHaveReviewOnPlace(review.UserID, review.Place_Id, out long? review_id, connection))
					{
						command.CommandText = @"UPDATE Reviews SET Comment = @com,Rating = @rating,Date = @date,OnMod = @mod WHERE Review_id = @reviewid";
						if (review.Comment == null)
						{
							command.CommandText = @"UPDATE Reviews SET Rating = @rating,Date = @date WHERE Review_id = @reviewid";
						}
						command.Parameters.Add(new SqliteParameter("@mod", mod));
						command.Parameters.Add(new SqliteParameter("@com", review.Comment));
						command.Parameters.Add(new SqliteParameter("@rating", review.Rating));
						command.Parameters.Add(new SqliteParameter("@date", review.Date));
						command.Parameters.Add(new SqliteParameter("@reviewid", review_id));
						int idk = command.ExecuteNonQuery();
						Console.WriteLine($"Кол-во добавленных элементов: {idk}");
						return idk != 0;
					}
					command.CommandText =
							@"INSERT INTO Reviews(Users_id,Place_id,Comment,Rating,Date) VALUES (@UserID,@Place,@comment,@Rating,@date)";
					if (review.Comment == null)
					{
						command.CommandText =
							@"INSERT INTO Reviews(Users_id,Place_id,Rating,Date) VALUES (@UserID,@Place,@Rating,@date)";
					}
					command.Parameters.Add(new SqliteParameter("@comment", review.Comment));
					command.Parameters.Add(new SqliteParameter("@UserID", review.UserID));
					command.Parameters.Add(new SqliteParameter("@Rating", review.Rating));
					command.Parameters.Add(new SqliteParameter("@Place", review.Place_Id));
					command.Parameters.Add(new SqliteParameter("@date", review.Date));
					int number = command.ExecuteNonQuery();
					Console.WriteLine($"Кол-во добавленных элементов: {number}");
					return number != 0;
				}
			}
		}

		public static void CreateTableReviews(SqliteCommand command)
		{
			command.CommandText =
					@"CREATE TABLE IF NOT EXISTS ""Reviews"" (
                    ""Review_id"" INTEGER,
                	""Users_id""	INTEGER,
                    ""Place_id"" INTEGER,
                	""Comment""	TEXT,
                	""Rating""	INTEGER NOT NULL,
                    ""Date"" TEXT,
					""OnMod"" INTEGER,
                	FOREIGN KEY(""Users_id"") REFERENCES ""TG_Users""(""TG_id"") ON UPDATE CASCADE,
                    FOREIGN KEY(""Place_id"") REFERENCES ""Places""(Place_id) ON UPDATE CASCADE,
                    PRIMARY KEY(""Review_id"" AUTOINCREMENT)
                );";
			command.ExecuteNonQuery();
		}

		public virtual Review? Load(long Place_id,long UserID)
		{
			Review review = null;
			using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				using (var command = new SqliteCommand())
				{
					command.Connection = connection;
					command.CommandText = $@"SELECT Place_id,Users_id,Comment,Rating,Date FROM Reviews WHERE Place_id = @Place_id AND Users_id = @UserID";
					command.Parameters.Add(new SqliteParameter("@Place_id", Place_id));
					command.Parameters.Add(new SqliteParameter("@UserID", UserID));
					using (SqliteDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							if (reader.HasRows)
							{
								long id = reader.GetInt64(0);
								long userid = reader.GetInt64(1);
								string? comment = reader.IsDBNull(2) ? null : reader.GetString(2);
								int rating = reader.GetInt32(3);
								DateTime date = reader.GetDateTime(4);
								review = new Review(id, userid, rating, comment, date);
							}
						}
					}
				}
			}
			return review;
		}

		public static long GetPlaceId(string name,int corpus, int floor, int type)
		{
			if(type <= 0 || type > 3)
			{
                throw new ArgumentException("type - 1(Буфет), 2(Столовая) или 3(Продуктовый)", nameof(type));
            }
			long placeid = 0;
            using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
				using (var command = new SqliteCommand())
				{
					command.Connection = connection;
					command.CommandText = $@"SELECT Place_id FROM Places WHERE Name = @name AND Corpus = @corpus AND Floor = @floor AND Type = @type";
					command.Parameters.Add(new SqliteParameter("@name", name));
					command.Parameters.Add(new SqliteParameter("@corpus", corpus));
					command.Parameters.Add(new SqliteParameter("@floor", floor));
					command.Parameters.Add(new SqliteParameter("@type", type));
					using (SqliteDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							placeid = reader.GetInt64(0);
							return placeid;
						}
					}
				}
            }
            return placeid;
        }

		public static void LoadAllPlaces(int type)
		{
			using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				using (var command = new SqliteCommand())
				{
					command.Connection = connection;
					command.CommandText = $@"SELECT * FROM Places WHERE Type = @type";
					command.Parameters.Add(new SqliteParameter("@type", type));
					using (SqliteDataReader reader = command.ExecuteReader())
					{
						switch (type)
						{
							case 1:
								{
									List<Buffet> list = [];
									while (reader.Read())
									{
										long placeid = reader.GetInt64(0);
										string name = reader.GetString(1);
										int corpus = reader.GetInt32(3);
										string description = reader.GetString(4);
										int floor = reader.GetInt32(5);
										list.Add(new Buffet(placeid, name, corpus, floor, description, LoadAllReviews(placeid), Product.LoadAllProducts(placeid)));
									}
									ObjectLists.AddRangeList(list);
									break;
								}
							case 2:
								{
									List<Canteen> list = [];
									while (reader.Read())
									{
										long placeid = reader.GetInt64(0);
										string name = reader.GetString(1);
										int corpus = reader.GetInt32(3);
										string description = reader.GetString(4);
										int floor = reader.GetInt32(5);
										list.Add(new Canteen(placeid, name, corpus, floor, description, LoadAllReviews(placeid), Product.LoadAllProducts(placeid)));
									}
									ObjectLists.AddRangeList(list);
									break;
								}
							case 3:
								{
									List<Grocery> list = [];
									while (reader.Read())
									{
										long placeid = reader.GetInt64(0);
										string name = reader.GetString(1);
										string description = reader.GetString(4);
										list.Add(new Grocery(placeid, name, description, LoadAllReviews(placeid), Product.LoadAllProducts(placeid)));
									}
									ObjectLists.AddRangeList(list);
									break;
								}
						}
					}
				}
			}
        }

		public static List<Review> LoadAllReviews(long pd)
		{
			List<Review> list = [];
			using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				using (var command = new SqliteCommand())
				{
					command.Connection = connection;
					command.CommandText = $@"SELECT * FROM Reviews WHERE Place_id = @pd AND OnMod = 0";
					command.Parameters.Add(new SqliteParameter("@pd", pd));
					using (SqliteDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							long UserID = reader.GetInt64(1);
							long Placeid = reader.GetInt64(2);
							string? com = reader.IsDBNull(3) ? null : reader.GetString(3);
							int rate = reader.GetInt32(4);
							DateTime date = reader.GetDateTime(5);
							list.Add(new Review(Placeid, UserID, rate, com, date));
						}
					}
				}
            }
			return list;
        }

		public virtual bool AddReview(Review review,int mod)
		{
			ArgumentNullException.ThrowIfNull(review);

			lock (reviewLock)
			{
				if (!Reviews.Any(x => x.UserID == review.UserID) && mod == 0)
				{
					if (Save(review,mod)) { Reviews.Add(review); }
					return true;
				}
				return false;
			}
		}
		public virtual bool AddReview(long placeid,long userID, int rating, string? comment,int mod)
		{
			lock (reviewLock)
			{
				if (!Reviews.Any(x => x.UserID == userID) && mod == 0)
				{
					Review review = new Review(placeid, userID, rating, comment);

					if (Save(review,mod)) { Reviews.Add(review); }
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
					RemoveReviewFromBD(reviewToRemove);
					return true;
				}
				return false;
			}
		}

		public virtual bool RemoveReviewFromBD(Review review)
		{
			using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				using (var command = new SqliteCommand())
				{
					command.Connection = connection;
					if (IfUserHaveReviewOnPlace(review.UserID, review.Place_Id, out long? Reviewid, connection))
					{
						command.CommandText = $@"UPDATE Reviews SET OnMod = 2 WHERE Review_id = @reviewid";
						command.Parameters.Add(new SqliteParameter("@reviewid", Reviewid));
						int number = command.ExecuteNonQuery();
						return number != 0;
					}
				}
				return false;
			}
        }

		public virtual Review? GetReview(long userID) => Reviews.FirstOrDefault(x => x.UserID == userID);
        private static bool IfUserHaveReviewOnPlace(long UserID, long Place,out long? Review_id,SqliteConnection connection)
        {
			using (var command = new SqliteCommand())
			{
				command.Connection = connection;

				command.CommandText =
						$@"SELECT 1 FROM Reviews WHERE
                    ""Users_id"" = @UserID AND ""Place_id"" = @place AND ""OnMod"" != 2";
				command.Parameters.Add(new SqliteParameter("@UserID", UserID));
				command.Parameters.Add(new SqliteParameter("@place", Place));
				Review_id = (long?)command.ExecuteScalar();
				return Review_id != null;
			}
        }
    }
}
