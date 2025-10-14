namespace OBED.Include
{
	public enum RoleType
	{
		Administrator,
		Common_User,
		VIP_User
	}
	class Person(string username, long userID, RoleType role)
	{
		public string Username { get; private set; } = username;
		public long UserID { get; init; } = userID;
		public RoleType Role { get; private set; } = role;

		// TODO: ChangeUsername()
		// TODO: SetRole()
	}
	enum UserAction
	{
		RatingRequest,
		CommentRequest,
		NoActiveRequest
	}
	class UserState()
	{
		public UserAction? Action { get; set; }
		public string? RefTo { get; set; }
		public int Rating { get; set; }
		public string? Comment { get; set; }
	}
}
