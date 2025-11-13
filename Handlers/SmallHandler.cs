using OBED.Services;
using OBED.Include;
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
			    [("Места", "/type")],
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

    public class TypeHandler : ICommandHandler, IResponseSender
    {
        public bool CanHandle(string command) => command.StartsWith("/type");
        public async Task HandleAsync(Message msg)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(msg.Text);

            var user = await RegistrationService.TryGetOrRegisterUser(msg);

            await Sender.EditOrSend(new(msg, "Выберите тип точки", new InlineKeyboardButton[][]
            {
                [("Столовые", "/placeSelector -C")],
                [("Буфеты", "/placeSelector -B")],
                [("Внешние магазины", "/placeSelector -G")],
                [("Назад", "/start")]
            }));

            await SendResponseAsync(DateTime.Now, user.UserID, msg.Text);
        }
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
        public async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
    }
}
