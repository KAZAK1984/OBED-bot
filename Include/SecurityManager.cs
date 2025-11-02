using System.Collections.Concurrent;

namespace OBED.Include
{
	public enum SuspiciousClass
	{
		Light,
		Medium,
		High
	}
	static class SecurityManager
	{
		public static ConcurrentDictionary<long, string> BlockedUsers { get; private set; } = [];
		public static ConcurrentDictionary<long, (SuspiciousClass suspiciousClass, DateTime time)> SuspiciousUsers { get; private set; } = [];
		static ConcurrentDictionary<long, List<(DateTime date, string message)>> LastUsersRequests { get; set; } = [];

		public static async Task SecurityCheck(long userID, string? message)
		{
			if (message == null)
				return;

			if (BlockedUsers.ContainsKey(userID))
			{
				await SlowDownUserAsync(userID);
				return;
			}

			var userRequests = LastUsersRequests.GetOrAdd(userID, _ => []);

			int requestsPerSecond = 0;
			lock (userRequests)
			{
				userRequests.Add((DateTime.Now, message));
				if (userRequests.Count > 10)
					userRequests.RemoveAt(0);

				requestsPerSecond = userRequests.Count(x => (DateTime.Now - x.date).TotalSeconds <= 1);
			}

			_ = requestsPerSecond switch
			{
				> 6 => BlockedUsers.TryAdd(userID, "Попытка отправить слишком много запросов в секунду"),
				> 5 => UpdateSuspiciousUser(userID, SuspiciousClass.High),
				> 4 => UpdateSuspiciousUser(userID, SuspiciousClass.Medium),
				> 3 => UpdateSuspiciousUser(userID, SuspiciousClass.Light),
				_ => false
			};

			await SlowDownUserAsync(userID);
		}
		public static bool RepeatCheck(long userID, string? message)
		{
			if (BlockedUsers.TryGetValue(userID, out string? reason) || message == null)
				return false;

			var userRequests = LastUsersRequests.GetOrAdd(userID, _ => []);

			if (userRequests.Count < 3)
				return false;

			if (userRequests[^2].message == message && userRequests[^3].message != message)
				return true;

			return false;
		}
		public static bool UpdateSuspiciousUser(long userID, SuspiciousClass newLevel)
		{
			try
			{
				SuspiciousUsers.AddOrUpdate(userID, (newLevel, DateTime.Now), (key, oldValue) => (newLevel, DateTime.Now));
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return false;
			}
			return true;
		}
		private static async Task SlowDownUserAsync(long userID)
		{
			int delay;
			if (BlockedUsers.ContainsKey(userID))
			{
				delay = 30000;
			}
			else
			{
				if (!SuspiciousUsers.TryGetValue(userID, out var suspiciousUser))
					return;

				delay = suspiciousUser.suspiciousClass switch
				{
					SuspiciousClass.Light => 1000,
					SuspiciousClass.Medium => 3000,
					SuspiciousClass.High => 6000,
					_ => 0
				};

				TryReduceSuspicious(userID, suspiciousUser);
			}

			if (delay > 0)
				await Task.Delay(delay);
		}

		private static void TryReduceSuspicious(long userID, (SuspiciousClass suspiciousClass, DateTime time) suspiciousUser)
		{
			var timePassed = DateTime.Now - suspiciousUser.time;

			switch (suspiciousUser.suspiciousClass)
			{
				case SuspiciousClass.Light when timePassed.TotalMinutes > 3:
					{
						SuspiciousUsers.TryRemove(userID, out _);
						break;
					}
				case SuspiciousClass.Medium when timePassed.TotalMinutes > 30:
					{
						UpdateSuspiciousUser(userID, SuspiciousClass.Light);
						break;
					}
				case SuspiciousClass.High when timePassed.TotalHours > 5:
					{
						UpdateSuspiciousUser(userID, SuspiciousClass.Medium);
						break;
					}
			}
		}
	}
}
