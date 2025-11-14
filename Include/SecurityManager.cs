using System.Collections.Concurrent;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OBED.Include
{
	public enum SuspiciousClass
	{
		Light,
		Medium,
		High
	}
	class MessageData(Message msg, UpdateType type)
	{
		public Message Msg { get; init; } = msg;
		public UpdateType Type { get; init; } = type;
	}
	static class SecurityManager
	{
		public static ConcurrentDictionary<long, string> BlockedUsers { get; private set; } = [];
		public static ConcurrentDictionary<long, (SuspiciousClass suspiciousClass, DateTime time)> SuspiciousUsers { get; private set; } = [];
		public static ConcurrentQueue<(object obj, DateTime deferredTime)> RequestQueue { get; private set; } = [];
		static ConcurrentDictionary<long, List<(DateTime date, string message)>> LastUsersRequests { get; set; } = [];

		public static bool SecurityCheck<T>(long userID, T type)
		{
			string message = "";
			if (type is MessageData msgData)
			{
				message = msgData.Msg.Text ?? msgData.Msg.Caption ?? msgData.Msg.Type.ToString();
			}
			else if (type is CallbackQuery callback)
			{
				ArgumentNullException.ThrowIfNullOrEmpty(callback.Data);
				if (callback.Message == null)
					throw new ArgumentNullException(callback.Data);
				message = callback.Data;
			}
			else
				throw new Exception($"{type} - uncorrect type");

			if (BlockedUsers.ContainsKey(userID))
				return false;

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
				> 6 => BlockedUsers.TryAdd(userID, "Попытка совершить спам атаку"),
				> 5 => UpdateSuspiciousUser(userID, SuspiciousClass.High),
				> 4 => UpdateSuspiciousUser(userID, SuspiciousClass.Medium),
				> 3 => UpdateSuspiciousUser(userID, SuspiciousClass.Light),
				_ => false
			};

			return SlowDownUserAsync(userID, type);
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
		private static bool SlowDownUserAsync<T>(long userID, T type)
		{
			TimeSpan delay;
			if (BlockedUsers.ContainsKey(userID))
			{
				delay = TimeSpan.FromSeconds(12);
			}
			else
			{
				if (!SuspiciousUsers.TryGetValue(userID, out var suspiciousUser))
					return false;

				delay = suspiciousUser.suspiciousClass switch
				{
					SuspiciousClass.Light => TimeSpan.FromSeconds(1),
					SuspiciousClass.Medium => TimeSpan.FromSeconds(3),
					SuspiciousClass.High => TimeSpan.FromSeconds(6),
					_ => TimeSpan.FromSeconds(0)
				};

				TryReduceSuspicious(userID, suspiciousUser);
			}

			ArgumentNullException.ThrowIfNull(type);
			if (type is MessageData || type is CallbackQuery)
				RequestQueue.Enqueue((type, DateTime.Now + delay));
			else
				throw new Exception($"{type} - uncorrect type");

			return true;
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
