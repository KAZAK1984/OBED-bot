using Telegram.Bot;

namespace OBED.TelegramBot
{
	static class TelegramBot
	{
		public static CancellationTokenSource? Cts { get; private set; } = null;
		public static TelegramBotClient? Bot { get; private set; } = null;
		public static string? Token { get; } = Environment.GetEnvironmentVariable("TOKEN");

		public static void BotInit()
		{
			if (Bot != null)
				throw new Exception("Invalid request, bot already created.");
			if (Token == null)
				throw new Exception("Invalid token, check environment setting.");

			using var cts = new CancellationTokenSource();
			Cts = cts;
			Bot = new TelegramBotClient(Token!, cancellationToken: cts.Token);
		}
	}
}
