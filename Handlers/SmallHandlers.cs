using OBED.Include;
using OBED.Services;
using OBED.TelegramBot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OBED.Handlers
{
	public class StartHandler : ICommandHandler, IResponseSender
	{
		public bool CanHandle(string command) => command.StartsWith("/start");
		public async Task HandleAsync(Message msg)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(msg.Text);

			var user = await RegistrationService.TryGetOrRegisterUser(msg);

			await Sender.EditOrSend(new(msg, "Старт", new InlineKeyboardButton[][]
				{
					[("Места", "/selector")],
					[("Профиль", "/person")],
					[("Помощь", "/help"), ("Поддержка", "/report")],
					[(user.Role == RoleType.Administrator ? "Админ панель" : "", "/admin")]
				}));

			await SendResponseAsync(DateTime.Now, user.UserID, msg.Text);
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}

	public class HelpHandler : ICommandHandler, IResponseSender
	{
		public bool CanHandle(string command) => command.StartsWith("/help");
		public async Task HandleAsync(Message msg)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(msg.Text);

			var user = await RegistrationService.TryGetOrRegisterUser(msg);

			await Sender.EditOrSend(new(msg, "Помощь", new InlineKeyboardButton[][]
				{
					[("Назад","/start")]
				}));

			await SendResponseAsync(DateTime.Now, user.UserID, msg.Text);
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}

	public class PersonHandler : ICommandHandler, IResponseSender
	{
		public bool CanHandle(string command) => command.StartsWith("/person");
		public async Task HandleAsync(Message msg)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(msg.Text);

			var user = await RegistrationService.TryGetOrRegisterUser(msg);

			await Sender.EditOrSend(new(msg, $"""
				Ваше имя: {user.Username} ({user.UserID})
				Ваш статус: {user.Role}
				""", new InlineKeyboardButton[][]
				{
					[("Назад","/start")]
				}));

			await SendResponseAsync(DateTime.Now, user.UserID, msg.Text);
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}

	public class ReportHandler : ICommandHandler, IResponseSender   // Placeholder - Перенести код Олега в новый файл, а этот хендлер удалить
	{
		public bool CanHandle(string command) => command.StartsWith("/report");
		public async Task HandleAsync(Message msg)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(msg.Text);

			var user = await RegistrationService.TryGetOrRegisterUser(msg);

			await Sender.EditOrSend(new(msg, "Обратная связь", new InlineKeyboardButton[][]
				{
					[("Назад","/start")]
				}));

			await SendResponseAsync(DateTime.Now, user.UserID, msg.Text);
		}
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
		public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
	}
}
