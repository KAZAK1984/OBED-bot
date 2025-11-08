using System.Data;

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
		NoActiveModeration
	}
	class UserState
	{
		public UserAction? Action { get; set; }
		public string? ReferenceToPlace { get; set; }
		public string? Comment { get; set; }
		public int Rating { get; set; }
    }
}
