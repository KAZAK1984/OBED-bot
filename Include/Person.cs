namespace OBED.Include
{
	public enum RoleType
	{
		Administrator,
		Common_User,
		VIP_User
	}
	class Person
	{
		public string Username { get; private set; }
		public long UserID { get; init; }
		public RoleType Role { get; private set; }

		public Person(string username, long userID, RoleType role)
		{
			if (userID < 0)
				throw new ArgumentException("UserID должно быть больше 0", nameof(userID));
			if (string.IsNullOrWhiteSpace(username))
				throw new ArgumentException("Username не может быть пустым или нулевым.", nameof(username));

			Username = username;
			UserID = userID;
			Role = role;
		}
		// TODO: ChangeUsername()
		// TODO: SetRole()
	}
	enum UserAction
	{
		RatingRequest,
		RatingChange,
		CommentRequest,
		CommentChange,
		NoActiveRequest,
		NoActiveChange
	}
	class UserState()
	{
		public UserAction? Action { get; set; }
		public string? ReferenceToPlace { get; set; }
		public string? Comment { get; set; }
		public int Rating
		{ 
			get => Rating;
			set
			{
				if (value < 1 || value > 10)
						throw new ArgumentOutOfRangeException(nameof(value), "Рейтинг должен быть от 1 до 10");
				Rating = value;
			}
		}
	}
}
