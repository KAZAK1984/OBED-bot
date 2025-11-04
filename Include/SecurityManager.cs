using System.Collections.Concurrent;
using Telegram.Bot.Types;

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
		public static ConcurrentQueue<(Message msg, string message, Chat chat, int msgID, DateTime time)> RequestQueue { get; private set; } = [];
		static ConcurrentDictionary<long, List<(DateTime date, string message)>> LastUsersRequests { get; set; } = [];

		public static void SecurityCheck<T>(long userID, T type, string? priorityRecording = null)
		{
			string? message = null;
			Chat? chat = null;
			int messageId = 0;
			Message? refMSG = null;
			if (type is Message msg)
			{
				ArgumentNullException.ThrowIfNullOrEmpty(msg.Text);
				message = priorityRecording ?? msg.Text;
				chat = msg.Chat;
				messageId = msg.Id;
				refMSG = msg;
			}
			else if (type is CallbackQuery callback)
			{
				ArgumentNullException.ThrowIfNullOrEmpty(callback.Data);
				if (callback.Message == null)
					throw new ArgumentNullException(callback.Data);

				message = priorityRecording ?? callback.Data;
				chat = callback.Message.Chat;
				messageId = callback.Message.Id;
				refMSG = callback.Message;
			}
			else
				throw new Exception($"{type} - uncorrect type");

			if (BlockedUsers.ContainsKey(userID))
			{
				SlowDownUserAsync(userID, chat, message, messageId, refMSG);
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
				> 6 => BlockedUsers.TryAdd(userID, "Попытка совершить спам атаку"),
				> 5 => UpdateSuspiciousUser(userID, SuspiciousClass.High),
				> 4 => UpdateSuspiciousUser(userID, SuspiciousClass.Medium),
				> 3 => UpdateSuspiciousUser(userID, SuspiciousClass.Light),
				_ => false
			};

			SlowDownUserAsync(userID, chat, message, messageId, refMSG);
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
		private static void SlowDownUserAsync(long userID, Chat chat, string message, int messageId, Message msg)
		{
			TimeSpan delay;
			if (BlockedUsers.ContainsKey(userID))
			{
				delay = TimeSpan.FromSeconds(12);
			}
			else
			{
				if (!SuspiciousUsers.TryGetValue(userID, out var suspiciousUser))
					return;

				delay = suspiciousUser.suspiciousClass switch
				{
					SuspiciousClass.Light => TimeSpan.FromSeconds(1),
					SuspiciousClass.Medium => TimeSpan.FromSeconds(3),
					SuspiciousClass.High => TimeSpan.FromSeconds(6),
					_ => TimeSpan.FromSeconds(0)
				};

				TryReduceSuspicious(userID, suspiciousUser);
			}

			RequestQueue.Enqueue((msg, message, chat, messageId, DateTime.Now + delay));
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
