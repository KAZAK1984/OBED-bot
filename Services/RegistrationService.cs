using OBED.Include;
using Telegram.Bot.Types;

namespace OBED.Services
{
    static class RegistrationService
    {
        public static async Task<Person> TryGetOrRegisterUser(Message msg)
        {
            if (ObjectLists.Persons.TryGetValue(msg.Chat.Id, out Person? foundPerson))
            {
                if (foundPerson == null)
                {
                    ObjectLists.Persons.TryRemove(msg.Chat.Id, out _);
                    return await TryGetOrRegisterUser(msg);
                }
                return foundPerson;
            }

            ObjectLists.Persons.TryAdd(msg.Chat.Id, new Person(msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName), msg.Chat.Id, RoleType.CommonUser));
            UserState.dictionary.TryAdd(msg.Chat.Id, new());
            ObjectLists.Persons.TryGetValue(msg.Chat.Id, out Person? person);

            if (person!.UserID == 1204402944)    // TODO: Удалить хардкод после интеграции с бд
                person.SetRole(RoleType.Administrator);

            await SendResponseAsync(DateTime.Now, person.UserID, $"Пользователь {person.Username} зарегистрирован");
            return person;
        }
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
        public static async Task SendResponseAsync(DateTime date, long userId, string text) => Console.WriteLine($"{date} | {userId} | {text}"); // TODO: Реализовать логирование сообщений в бд или файл
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
    }
}
