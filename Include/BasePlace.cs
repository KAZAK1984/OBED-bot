using Microsoft.Data.Sqlite;
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

	class Review
	{
		public int Place_Id { get; private set; }
		public long UserID { get; init; }
		public int Rating { get; private set; }
		public string? Comment { get; private set; }

		public DateTime? Date { get; private set; }

		public Review(int placeid,long userID, int rating, string? comment = null, DateTime? date = null)
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

	abstract class BasePlace(int placeid,string name, string? description = null, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null)
	{
		public int Place_id { get; private set; } = placeid;
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

		public virtual bool Save(Review review)
		{

			string dbConnectionString = "Data Source=OBED_DB.db";
			if (review.UserID <= 0)
				throw new ArgumentException("UserID должно быть больше 0", nameof(review.UserID));
			if (review.Rating < 1 || review.Rating > 10)
				throw new ArgumentException("Рейтинг должен быть от 1 до 10", nameof(review.Rating));
			using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				var command = new SqliteCommand();
				command.Connection = connection;
				//Создание таблицы если её нету
				command.CommandText =
					@"CREATE TABLE IF NOT EXISTS ""Reviews"" (
                    ""Review_id"" INTEGER,
                	""Users_id""	INTEGER,
                    ""Place_id"" INTEGER,
                	""Comment""	TEXT,
                	""Rating""	INTEGER NOT NULL,
                    ""Date"" TEXT,
                	FOREIGN KEY(""Users_id"") REFERENCES ""TG_Users""(""TG_id"") ON UPDATE CASCADE,
                    FOREIGN KEY(""Place_id"") REFERENCES ""Places""(Place_id) ON UPDATE CASCADE,
                    PRIMARY KEY(""Review_id"" AUTOINCREMENT)
                );";
				command.ExecuteNonQuery();
                if (IfUserHaveReviewOnPlace(review.UserID, review.Place_Id))
				{
					return false;
				}
				command.CommandText =
					@"INSERT INTO Reviews(TG_id,Place_id,Comment,Rating,Date) VALUES (@UserID,@Place,@comment,@Rating,@date)";
				command.Parameters.Add(new SqliteParameter("@UserID", review.UserID));
				command.Parameters.Add(new SqliteParameter("@Rating", review.Rating));
				command.Parameters.Add(new SqliteParameter("@comment", review.Comment));
				command.Parameters.Add(new SqliteParameter("@Place", review.Place_Id));
				command.Parameters.Add(new SqliteParameter("@date", review.Date));
				int number = command.ExecuteNonQuery();
				Console.WriteLine($"Кол-во добавленных элементов: {number}");
				return true;
			}
		}

		public virtual Review? Load(int Place_id,long UserID)
		{
			string dbConnectionString = "Data Source=OBED_DB.db";
			Review review = new Review(0,0,0,"");
			using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				var command = new SqliteCommand();
				command.Connection = connection;
				command.CommandText = $@"SELECT Place_id,Users_id,Comment,Rating,Date FROM Reviews WHERE Place_id LIKE {Place_id} AND Users_id LIKE {UserID}";
				using (SqliteDataReader reader = command.ExecuteReader())
				{
					if (reader.HasRows)
					{
						int id = reader.GetInt32(0);
						long userid = reader.GetInt64(1);
						string comment = reader.GetString(2);
						int rating = reader.GetInt32(3);
						DateTime date = reader.GetDateTime(4);
						review = new Review(id, userid, rating, comment,date);
					}
				}
			}
			return review;
		}

		public static void LoadAllPlaces(int type)
		{
            string dbConnectionString = "Data Source=OBED_DB.db";
			using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				var command = new SqliteCommand();
				command.Connection = connection;
				command.CommandText = $@"SELECT * FROM Places WHERE Type = {type}";
				using(SqliteDataReader reader = command.ExecuteReader())
				{
					switch (type)
					{
						case 1:
							{
								List<Buffet> list = [];
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        int placeid = reader.GetInt32(0);
                                        string name = reader.GetString(1);
                                        int corpus = reader.GetInt32(3);
                                        string description = reader.GetString(4);
                                        int floor = reader.GetInt32(5);
										list.Add(new Buffet(placeid,name,corpus,floor,description));
                                    }
                                }
								ObjectLists.AddRangeList<Buffet>(list);
                                break;
							}
						case 2:
							{
								List<Canteen> list = [];
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        int placeid = reader.GetInt32(0);
                                        string name = reader.GetString(1);
                                        int corpus = reader.GetInt32(3);
                                        string description = reader.GetString(4);
                                        int floor = reader.GetInt32(5);
                                        list.Add(new Canteen(placeid, name, corpus, floor, description));
                                    }
                                }
								ObjectLists.AddRangeList<Canteen>(list);
								break;
                            }
						case 3:
							{
								List<Grocery> list = [];
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        int placeid = reader.GetInt32(0);
                                        string name = reader.GetString(1);
                                        string description = reader.GetString(4);
                                        list.Add(new Grocery(placeid, name, description));
                                    }
                                }
								ObjectLists.AddRangeList<Grocery>(list);
								break;
                            }
					}
					
				}
			}
        }

		public virtual bool AddReview(Review review)
		{
			ArgumentNullException.ThrowIfNull(review);

			lock (reviewLock)
			{
				if (!Reviews.Any(x => x.UserID == review.UserID))
				{
					Reviews.Add(review);
					Save(review);
					return true;
				}
				return false;
			}
		}
		public virtual bool AddReview(int placeid,long userID, int rating, string? comment)
		{
			lock (reviewLock)
			{
				if (!Reviews.Any(x => x.UserID == userID))
				{
					Review review = new Review(placeid, userID, rating, comment);

                    Reviews.Add(review);
					Save(review);
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
        private static bool IfUserHaveReviewOnPlace(long UserID, int Place)
        {

            string dbConnectionString = "Data Source=OBED_DB.db";
            using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
                var command = new SqliteCommand();
                command.Connection = connection;

                command.CommandText =
                    $@"SELECT 1 FROM Reviews WHERE
                    ""Users_id"" LIKE {UserID} AND ""Place_id"" LIKE '{Place}'";
                return command.ExecuteScalar() != null;
            }
        }
    }
}
