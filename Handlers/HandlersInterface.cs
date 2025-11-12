using Telegram.Bot.Types;

namespace OBED.Handlers
{
	public interface ICommandHandler
	{
		bool CanHandle(string messageText);
		Task HandleAsync(Message message);
	}
	public interface IResponseSender
	{
		Task SendResponseAsync(DateTime date, long userId, string text);
	}
}
