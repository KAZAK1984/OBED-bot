using Microsoft.Data.Sqlite;
using System.Data;
using Telegram.Bot.Types;

namespace OBED.Include
{
	public enum RoleType
	{
		Administrator,
		CommonUser,
		VipUser
	}
	class Person
	{
		private static readonly string dbConnectionString = "Data Source=OBED_DB.db";
		public string Username { get; private set; }
		public long UserID { get; init; }
		public RoleType Role { get; private set; }
		
		public Person(string username, long userID, RoleType role)
		{
			if (userID <= 0)
				throw new ArgumentException("UserID должно быть больше 0", nameof(userID));
			if (string.IsNullOrWhiteSpace(username))
				throw new ArgumentException("Username не может быть пустым или нулевым.", nameof(username));
			if (!Enum.IsDefined(typeof(RoleType), role))
				throw new ArgumentException("Недопустимое значение роли.", nameof(role));

			Username = username;
			UserID = userID;
			Role = role;
		}
		public void SetRole(RoleType role)
		{
			if (!Enum.IsDefined(typeof(RoleType), role))
				throw new ArgumentException("Недопустимое значение роли.", nameof(role));
			Role = role;
		}

		public static void LoadPersonsFromBD()
		{
			using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				var command = new SqliteCommand();
				command.Connection = connection;
				command.CommandText = @"SELECT * FROM TG_Users";
				using(SqliteDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						string username = reader.GetString(reader.GetOrdinal("Name"));
						long UserID = reader.GetInt64(reader.GetOrdinal("TG_id"));
						string role = reader.GetString(reader.GetOrdinal("Role"));
						RoleType role1;
						switch (role)
						{
							case ("CommonUser"):
								{
									role1 = RoleType.CommonUser;
									break;
								}
							case ("VipUser"):
								{
									role1 = RoleType.VipUser;
									break;
								}
							case ("Administrator"):
								{
									role1 = RoleType.Administrator;
									break;
								}
							default:
								{
									role1 = RoleType.CommonUser;
									break;
								}
						}
						int onban = reader.GetInt32(reader.GetOrdinal("OnBan"));
						ObjectLists.Persons.TryAdd(UserID, new Person(username, UserID, role1));
					}
				}
			}
		}
	// TODO: ChangeUsername()
}
	/// <summary>
	/// Текущий тип обработки сообщений в чате от юзера. Без данных тегов UserState у сообщения игнорируются
	/// </summary>
	enum UserAction
	{
		/// <summary> Для НОВЫХ отзывов. Запрос рейтинга от 1 до 10</summary>
		RatingRequest,
		/// <summary> Для ОБНОВЛЕНИЯ отзывов. Запрос нового рейтинга от 1 до 10</summary>
		RatingChange,
		/// <summary> Для НОВЫХ отзывов. Запрос не-пустой строки</summary>
		CommentRequest,
		/// <summary> Для ОБНОВЛЕНИЯ отзывов. Запрос новой не-пустой строки</summary>
		CommentChange,
		/// <summary> Для НОВЫХ отзывов. Отметка, позволяющая перейти к финальному этапу отправки отзыва</summary>
        NoActiveRequest,
		/// <summary> Для ОБНОВЛЕНИЯ отзывов. Отметка, позволяющая перейти к финальному этапу обновления отзыва</summary>
		NoActiveChange,
		/// <summary> Для МОДЕРАЦИИ отзывов. Отметка, позволяющая перейти к отправки отредактированного сообщения</summary>
		Moderation,
		/// <summary> Для МОДЕРАЦИИ отзывов. Отметка, позволяющая перейти к финальному этапу отправки отредактированного сообщения</summary>
		NoActiveModeration,
		/// <summary> Для НОВЫХ отчетов (репортов). Запрос комментария</summary>
		ReportRequest,
        /// <summary> Для ОБНОВЛЕНИЯ отчетов (репортов). Запрос нового комментария</summary>
        ReportChange,
        /// <summary> Для НОВЫХ отчетов (репортов). Отметка, позволяющая перейти к финальному этапу отправки репорта</summary>
        NoActiveReport,
		/// <summary> Для РЕАГИРОВАНИЯ НА РЕПОРТЫ. Запрос ответа на репорт пользователя</summary>
		ReportResponse,
        /// <summary> Для РЕАГИРОВАНИЯ НА РЕПОРТЫ. Отметка, позволяющая перейти к финальному этапу отправки ответа на репорт</summary>
        NoActiveReportResponse,
		/// <summary> Для ДОБАВЛЕНИЯ ТЕГОВ для репортов. Запрос тегов для репорта от пользователя</summary>
		ReportSetTegs,
        /// <summary> Для ДОБАВЛЕНИЯ ТЕГОВ для репортов. Отметка, позволяющая перейти к финальному этапу отправки тегов для репорт</summary>
        NoActiveReportSetTegs,
		/// <summary> Для ДОБАВЛЕНИЯ точки питания админом. Запрос не-пустой строки/названия точки</summary>
		PlaceNameRequest,
		NoPlaceNameRequest,
        CorpusRequest,
        FloorRequest,
        DescriptionRequest,
        TypeRequest,
		ProductNameRequest,
		ProductValueRequest,
		ProductperGramRequest,
		ProductTypeRequest,
		ProductPlaceRequest
    }
	class UserState
	{
		public UserAction? Action { get; set; }
		public string? ActionArguments { get; set; }
		public string? Comment { get; set; }
		public int Rating { get; set; }

		public PlaceData? TempData { get; set; }
    }
}
