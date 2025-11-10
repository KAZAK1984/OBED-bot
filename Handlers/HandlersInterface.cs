using Telegram.Bot.Types;

namespace OBED.Handlers
{
	public record HandlerResult(bool Result, string Info);

	public interface ICommandHandler
	{
		bool CanHandle(string messageText);
		Task<HandlerResult> HandleAsync(Message message);
	}
	public interface IMessageSender
	{
		Task SendResponseAsync(long userId, string text);
	}
}
