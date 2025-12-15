using Microsoft.Data.Sqlite;
using Telegram.Bot.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OBED.Include
{
    enum ReportTeg // Будет пополнятся новыми тегами
    {
        Bug,
        OutdatedInfo,
        WrongInfo,
        Suggestion
    }
    
    class FeedbackReport(long userID, string comment, List<ReportTeg> tegs, string[]? screenshots = null)
    {
		private static readonly string dbConnectionString = "Data Source=OBED_DB.db";
		public long IdInDB { get; set; }
		public long UserID { get; init; } = userID;
        public string Comment { get; private set; } = comment;
        public string? Answer { get; set; } = null;
        public List<ReportTeg> Tegs { get; set; } = tegs;
        public string[] Screenshots { get; private set; } = screenshots ?? [];
        public DateTime Date { get; private set; } = DateTime.Now;

        public void ChangeComment(string comment,long id)
        {
			ChangeCommentInDB(comment,id);
            Comment = comment;
            Date = DateTime.Now;
        }

		public static void DeleteReportFromDB(long id)
		{
			using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				var command = new SqliteCommand();
				command.Connection = connection;
				command.CommandText = @"DELETE FROM FeedbackReports WHERE FeedbackReport_id = @id";
				command.Parameters.Add(new SqliteParameter("@id", id));
				command.ExecuteNonQuery();
			}
		}

		public void ChangeCommentInDB(string comment,long id)
		{
			using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				var command = new SqliteCommand();
				command.Connection = connection;
				command.CommandText = @"UPDATE FeedbackReports SET Comment = @comment WHERE FeedbackReport_id = @id";
				command.Parameters.Add(new SqliteParameter("@comment", comment));
				command.Parameters.Add(new SqliteParameter("@id", id));
				command.ExecuteNonQuery();
			}
		}

		public static void UpdateReportAnswer(string comment,long id)
		{
			using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				var command = new SqliteCommand();
				command.Connection = connection;
				command.CommandText = @"UPDATE FeedbackReports SET Answer = @comment WHERE FeedbackReport_id = @id";
				command.Parameters.Add(new SqliteParameter("@comment", comment));
				command.Parameters.Add(new SqliteParameter("@id", id));
				command.ExecuteNonQuery();
			}
		}

        public static List<FeedbackReport> LoadAllReportsFromPerson(long userID)
        {
            using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
                var command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = @"SELECT * FROM FeedbackReports WHERE UserID = @userid";
                command.Parameters.Add(new SqliteParameter("@userid", userID));
                using(SqliteDataReader reader = command.ExecuteReader())
                {
                    List<FeedbackReport> list = [];
                    while (reader.Read())
                    {
						long id = reader.GetInt64(reader.GetOrdinal("FeedbackReport_id"));
                        string comment = reader.GetString(reader.GetOrdinal("Comment"));
                        //string[] screenshot = reader.(reader.GetOrdinal("Screenshot"));
                        string tegs = reader.GetString(reader.GetOrdinal("Tegs"));
						string? answer = reader.IsDBNull(reader.GetOrdinal("Answer")) ? null : reader.GetString(reader.GetOrdinal("Answer"));
						DateTime date = reader.GetDateTime(reader.GetOrdinal("Date"));
                        List<ReportTeg> tegstrue = [];
                        for(int i = 0;i < tegs.Length; i++)
                        {
                            switch (tegs[i])
                            {
                                case '0':
                                    {
                                        tegstrue.Add(ReportTeg.Bug);
                                        break;
                                    }
								case '1':
									{
										tegstrue.Add(ReportTeg.OutdatedInfo);
										break;
									}
								case '2':
									{
										tegstrue.Add(ReportTeg.WrongInfo);
										break;
									}
								case '3':
									{
										tegstrue.Add(ReportTeg.Suggestion);
										break;
									}
                                default:
                                    {
                                        Console.WriteLine("Неверные tegs");
                                        break;
                                    }
							}
                        }
						FeedbackReport report = new FeedbackReport(userID, comment, tegstrue);
						report.IdInDB = id;
						report.Date = date;
						report.Answer = answer;
                        list.Add(report);

                    }
                    return list;
                }
            }
        }

        public static FeedbackReport? GetFirstReportFromPerson(long userID,int offset)
        {
            using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
                var command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = @"SELECT * FROM FeedbackReports WHERE UserID = @userid LIMIT 1 OFFSET @off";
				command.Parameters.Add(new SqliteParameter("@userid", userID));
				command.Parameters.Add(new SqliteParameter("@off", offset));
				using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
						long id = reader.GetInt64(reader.GetOrdinal("FeedbackReport_id"));
						string comment = reader.GetString(reader.GetOrdinal("Comment"));
						string? answer = reader.IsDBNull(reader.GetOrdinal("Answer")) ? null : reader.GetString(reader.GetOrdinal("Answer"));
						//string[] screenshot = reader.(reader.GetOrdinal("Screenshot"));
						string tegs = reader.GetString(reader.GetOrdinal("Tegs"));
                        DateTime date = reader.GetDateTime(reader.GetOrdinal("Date"));
						List<ReportTeg> tegstrue = [];
						for (int i = 0; i < tegs.Length; i++)
						{
							switch (tegs[i])
							{
								case '0':
									{
										tegstrue.Add(ReportTeg.Bug);
										break;
									}
								case '1':
									{
										tegstrue.Add(ReportTeg.OutdatedInfo);
										break;
									}
								case '2':
									{
										tegstrue.Add(ReportTeg.WrongInfo);
										break;
									}
								case '3':
									{
										tegstrue.Add(ReportTeg.Suggestion);
										break;
									}
								default:
									{
										Console.WriteLine("Неверные tegs");
										break;
									}
							}
						}
                        FeedbackReport? report = new FeedbackReport(userID, comment, tegstrue);
                        report.Date = date;
                        report.Answer = answer;
						report.IdInDB = id;
                        return report;
					}
                    return null;
				}
			}
        }
		public static FeedbackReport? GetFirstReportFromPerson(long userID)
		{
			using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				var command = new SqliteCommand();
				command.Connection = connection;
				command.CommandText = @"SELECT * FROM FeedbackReports WHERE UserID = @userid LIMIT 1";
				command.Parameters.Add(new SqliteParameter("@userid", userID));
				using (SqliteDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						long id = reader.GetInt64(reader.GetOrdinal("FeedbackReport_id"));
						string comment = reader.GetString(reader.GetOrdinal("Comment"));
						string? answer = reader.IsDBNull(reader.GetOrdinal("Answer")) ? null : reader.GetString(reader.GetOrdinal("Answer"));
						//string[] screenshot = reader.(reader.GetOrdinal("Screenshot"));
						string tegs = reader.GetString(reader.GetOrdinal("Tegs"));
						DateTime date = reader.GetDateTime(reader.GetOrdinal("Date"));
						List<ReportTeg> tegstrue = [];
						for (int i = 0; i < tegs.Length; i++)
						{
							switch (tegs[i])
							{
								case '0':
									{
										tegstrue.Add(ReportTeg.Bug);
										break;
									}
								case '1':
									{
										tegstrue.Add(ReportTeg.OutdatedInfo);
										break;
									}
								case '2':
									{
										tegstrue.Add(ReportTeg.WrongInfo);
										break;
									}
								case '3':
									{
										tegstrue.Add(ReportTeg.Suggestion);
										break;
									}
								default:
									{
										Console.WriteLine("Неверные tegs");
										break;
									}
							}
						}
						FeedbackReport? report = new FeedbackReport(userID, comment, tegstrue);
						report.Date = date;
						report.Answer = answer;
						report.IdInDB = id;
						return report;
					}
					return null;
				}
			}
		}

		public static void UpdateTegsInReport(string tegs,long id)
		{
			using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				var command = new SqliteCommand();
				command.Connection = connection;
				command.CommandText = @"UPDATE FeedbackReports SET Tegs = @tegs WHERE FeedbackReport_id = @id";
				command.Parameters.Add(new SqliteParameter("@tegs", tegs));
				command.Parameters.Add(new SqliteParameter("@id", id));
				command.ExecuteNonQuery();
			}
		}

		public static FeedbackReport GetFirstReport()
		{
			using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				var command = new SqliteCommand();
				command.Connection = connection;
				command.CommandText = @"SELECT * FROM FeedbackReports LIMIT 1";
				using (SqliteDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						long id = reader.GetInt64(reader.GetOrdinal("FeedbackReport_id"));
						string comment = reader.GetString(reader.GetOrdinal("Comment"));
						string? answer = reader.IsDBNull(reader.GetOrdinal("Answer")) ? null : reader.GetString(reader.GetOrdinal("Answer"));
						long userID = reader.GetInt64(reader.GetOrdinal("UserID"));
						//string[] screenshot = reader.(reader.GetOrdinal("Screenshot"));
						string tegs = reader.GetString(reader.GetOrdinal("Tegs"));
						DateTime date = reader.GetDateTime(reader.GetOrdinal("Date"));
						List<ReportTeg> tegstrue = [];
						for (int i = 0; i < tegs.Length; i++)
						{
							switch (tegs[i])
							{
								case '0':
									{
										tegstrue.Add(ReportTeg.Bug);
										break;
									}
								case '1':
									{
										tegstrue.Add(ReportTeg.OutdatedInfo);
										break;
									}
								case '2':
									{
										tegstrue.Add(ReportTeg.WrongInfo);
										break;
									}
								case '3':
									{
										tegstrue.Add(ReportTeg.Suggestion);
										break;
									}
								default:
									{
										Console.WriteLine("Неверные tegs");
										break;
									}
							}
						}
						FeedbackReport report = new FeedbackReport(userID, comment, tegstrue);
						report.Date = date;
						report.Answer = answer;
						report.IdInDB = id;
						return report;
					}
					return new FeedbackReport(0, "Произошёл баг", new List<ReportTeg>());
				}
			}
		}

		public static void SaveFeedbackReport(FeedbackReport report)
		{
			using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
			{
				connection.Open();
				var command = new SqliteCommand();
				command.Connection = connection;
				command.CommandText = @"INSERT INTO FeedbackReports(UserID,Comment,Tegs,Date) VALUES (@userid,@comment,@tegs,@date)";
				if(report.Answer != null)
				{
					command.CommandText = @"INSERT INTO FeedbackReports(UserID,Comment,Answer,Tegs,Date) VALUES (@userid,@comment,@ans,@tegs,@date)";
					command.Parameters.Add(new SqliteParameter("@ans", report.Answer));
				}
				command.Parameters.Add(new SqliteParameter("@userid", report.UserID));
				command.Parameters.Add(new SqliteParameter("@comment", report.Comment));
				string tegs = "";
				foreach(var x in report.Tegs)
				{
					tegs += ((int)x).ToString();
				}
				command.Parameters.Add(new SqliteParameter("@tegs", tegs));
				command.Parameters.Add(new SqliteParameter("@date", report.Date));
				command.ExecuteNonQuery();
			}
		}

		public static bool GetFirstPersonInFeedbackReports(out Person? person)
        {
            using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
                var command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = @"SELECT TG_id,Name,Role FROM FeedbackReports JOIN TG_Users WHERE TG_Users.TG_id = FeedbackReports.UserID LIMIT 1";
                using(SqliteDataReader reader = command.ExecuteReader())
                {
                    person = null;
                    while (reader.Read())
                    {
                        long userid = reader.GetInt64(reader.GetOrdinal("TG_id"));
                        string name = reader.GetString(reader.GetOrdinal("Name"));
                        RoleType role;
                        switch (reader.GetString(reader.GetOrdinal("Role")))
                        {
                            case "CommonUser":
                                {
                                    role = RoleType.CommonUser;
                                    break;
                                }
							case "VipUser":
								{
									role = RoleType.VipUser;
									break;
								}
							case "Administrator":
								{
									role = RoleType.Administrator;
									break;
								}
                            default:
                                {
                                    role = RoleType.CommonUser;
                                    break;
                                }
						}
                        person = new Person(name, userid, role);
                        return true;
                    }
                    return false;
                }

			}
        }

        public static long CountFeedbackReports()
        {
            using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
                var command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = @"SELECT * FROM FeedbackReports";
                long r = 0;
                object? res = command.ExecuteScalar();
                if(res != null && res != DBNull.Value) { r = (long)res; }
                return r;
            }
        }

        public static void CreateFeedbackReportsTable(SqliteCommand command)
        {
            command.CommandText = @"CREATE TABLE IF NOT EXISTS ""FeedbackReports"" (
	""FeedbackReport_id""	INTEGER NOT NULL,
	""UserID""	INTEGER NOT NULL,
	""Comment""	TEXT NOT NULL,
	""Answer""	TEXT,
	""Screenshot""	BLOB,
	""Tegs""	TEXT,
	""Date""	TEXT,
	PRIMARY KEY(""FeedbackReport_id""),
	FOREIGN KEY(""UserID"") REFERENCES ""TG_Users""(""TG_id"") ON UPDATE CASCADE
);";
            command.ExecuteNonQuery();
		}
    }

    class ComplaintReport(long userID, string comment, List<ReportTeg> tegs, long subjectID, string[]? screenshots = null) : FeedbackReport(userID, comment, tegs, screenshots)
    {
        public long SubjectID { get; init; } = subjectID;
    }
}
