using OBED.Include;
using Telegram.Bot.Types;

namespace OBED.Handlers
{
	public class StartHandler : ICommandHandler
	{
		public bool CanHandle(string command) => command.StartsWith("/start");

		public async Task<HandlerResult> HandleAsync(Message msg)
		{
			ObjectLists.Persons.TryGetValue(msg.Chat.Id, out Person? foundUser);
			if (foundUser == null)
			{
				ObjectLists.Persons.TryAdd(msg.Chat.Id, new Person(msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName), msg.Chat.Id, RoleType.CommonUser));
				UserState.dictionary.TryAdd(msg.Chat.Id, new());
				ObjectLists.Persons.TryGetValue(msg.Chat.Id, out foundUser);

				if (foundUser!.UserID == 1204402944)
					foundUser.SetRole(RoleType.Administrator);
			}

            return new (true, "Lol");
        }
	}
}
