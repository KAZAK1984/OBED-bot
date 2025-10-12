using OBED.Include;
using System;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Programm
{
	static async Task Main()
	{
		using var cts = new CancellationTokenSource();
		var token = Environment.GetEnvironmentVariable("TOKEN");
		var bot = new TelegramBotClient(token!, cancellationToken: cts.Token);
		var meBot = await bot.GetMe();

		// PLACEHOLDERS
		List<Profile> profiles = [];
		List<Canteen> canteens = [new("Вкусочка", 6, 2), new("КургерКинг", 5, 3, null, [new("craze Humburger", 99.99, false, ProductType.MainDishes), new("Cook cola", 50, true, ProductType.Drinks), new("3", 99.99, false, ProductType.SideDishes), new("4", 50, true, ProductType.Drinks),
																						new("5", 99.99, false, ProductType.Drinks), new("6", 50, true, ProductType.SideDishes), new("7", 99.99, false, ProductType.MainDishes), new("8", 50, true, ProductType.Drinks),
																						new("9", 99.99, false, ProductType.MainDishes), new("10", 50, true, ProductType.Drinks), new("11", 99.99, false, ProductType.SideDishes), new("12", 50, true, ProductType.MainDishes),
																						new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes),
																						new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes),
																						new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes),
																						new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes),
																						new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes), new("MD", 99.99, false, ProductType.MainDishes)]),
								  new("Башни-близняшки", 9, 11, [new(12315156, 1, "Вкусно, но не грустно"), new(12315156, 2, "Грустно, но вкусно"), new(12315156, 4, "Грустно, но вкусно"), new(12315156, 2, "Грустно, но вкусно"),
																 new(12315156, 1, "Вкусно, но не грустно"), new(12315156, 9, "Грустно, но вкусно"), new(12315156, 7, "Грустно, но вкусно"), new(12315156, 10, "Грустно, но вкусно")],
								  [new("Левый", 9, false, ProductType.Drinks), new("Правый", 11, true, ProductType.MainDishes)]),
								  new("ОБЕД, УЮТНЕНЬКО", 1, 1), new("PLACEHOLDER", 3, 3), new("Оригинальный PLACEHOLDER", 5, 1)];
		// PLACEHOLDERS

		Dictionary<string, Dictionary<string, string>> allLocal = [];

        try
		{
            allLocal = SetLocal();
        }
		catch (Exception e)
		{
			Console.WriteLine(e);
			return; // Защита от дураков
		}
		
        Dictionary<long, UserState> usersState = []; // TODO: переход на noSQL

		bot.OnError += OnError;
		bot.OnMessage += OnMessage;
		bot.OnUpdate += OnUpdate;

		Console.WriteLine($"@{meBot.Username} is running... Press Enter to terminate\n");
		Console.ReadLine();
		cts.Cancel();

		async Task OnError(Exception exception, HandleErrorSource source)
		{
			Console.WriteLine(exception);
			await Task.Delay(2000, cts.Token);
		}

		async Task OnMessage(Message msg, UpdateType type)
		{
			switch (msg)
			{
				//case { ReplyToMessage: { } reply }:
				//	{
				//		break;
				//	}
				case { Type: { } mType }:
					{
						if (mType == MessageType.Text)
							if (msg.Text![0] == '/')
							{
								var splitStr = msg.Text.Split(' ');
								if (splitStr.Length > 1)
									await OnCommand(splitStr[0].ToLower(), splitStr[1].ToLower(), msg);
								else
									await OnCommand(splitStr[0].ToLower(), null, msg);
							}

						var foundUser = profiles
							.Where(x => x.UserID == msg.Chat.Id)
							.FirstOrDefault();

						if (foundUser == null)
						{
							await bot.SendMessage(msg.Chat.Id, "Вы не прошли регистрацию путём ввода /start, большая часть функций бота недоступна",
								replyMarkup: new InlineKeyboardButton[] { ("Зарегистрироваться", "/start") });
							break;
						}

						switch (usersState[foundUser.UserID].Action)
						{
							case (UserAction.RatingRequest):
								{
									if (int.TryParse(msg.Text, out int rating) && (rating > 0 && rating < 11))
									{
										usersState[foundUser.UserID].Rating = rating;
										usersState[foundUser.UserID].Action = UserAction.CommentRequest;
										await bot.SendMessage(msg.Chat, "Введите текст отзыва или откажитесь от сообщения отправив -", replyMarkup: new ForceReplyMarkup());
										break;
									}

									await bot.SendMessage(msg.Chat, "Ошибка при обработке! Убедитесь, что ваше сообщение содержит только цифры или они входят в промежуток от 1 до 10", replyMarkup: new ForceReplyMarkup());
									break;
								}
							case (UserAction.CommentRequest):
								{
									if (string.IsNullOrWhiteSpace(msg.Text))
									{
										await bot.SendMessage(msg.Chat, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит текст или откажитесь от сообщения отправив -", replyMarkup: new ForceReplyMarkup());
										break;
									}
									usersState[foundUser.UserID].Comment = null; // Очистка прошлого коммента

									if (msg.Text[0] != '-')
										usersState[foundUser.UserID].Comment = msg.Text;

									usersState[foundUser.UserID].Action = UserAction.NoActiveRequest;
									await bot.SendHtml(msg.Chat.Id, $"""
										Ваш отзыв:
										
											• Оценка: {usersState[foundUser.UserID].Rating}
											• Комментарий: {((msg.Text[0] == '-') ? "Отсутствует" : usersState[foundUser.UserID].Comment)}

										Всё верно?
										<keyboard>
										<button text="Да" callback="/sendreviev {usersState[foundUser!.UserID].RefTo}"
										<button text="Нет" callback="callback_resetAction"
										</keyboard>
										""");
									break;
								}
						}
						break;
					}
			}
		}

		async Task OnCommand(string command, string? args, Message msg)
		{
            //if (args == null)
            //	Console.WriteLine($"NOW COMMAND {msg.Chat.Username ?? msg.Chat.FirstName + msg.Chat.LastName}: {command}");
            //else
            //	Console.WriteLine($"NOW COMMAND {msg.Chat.Username ?? msg.Chat.FirstName + msg.Chat.LastName}: {command} {args}");

            var foundUser = profiles
                            .Where(x => x.UserID == msg.Chat.Id)
                            .FirstOrDefault();

            if (foundUser == null && command != "/start")
            {
                await bot.SendMessage(msg.Chat.Id, "Вы не прошли регистрацию путём ввода /start, большая часть функций бота недоступна",
                    replyMarkup: new InlineKeyboardButton[] { ("Зарегистрироваться", "/start") });
				return;
            }
			
            switch (command)
			{
				case ("/start"):
					{
                        if (!profiles.Select(x => x.UserID).Contains(msg.Chat.Id))
                        {
                            Console.WriteLine($"REG: {msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName)}");
                            profiles.Add(new Profile(msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName), msg.Chat.Id));
                            usersState.Add(msg.Chat.Id, new());

                            foundUser = profiles
                            .Where(x => x.UserID == msg.Chat.Id)
                            .FirstOrDefault();
                        }

                        await bot.SendHtml(msg.Chat.Id, $"""
											  {allLocal[foundUser!.local]["Start_command"]}
											  <keyboard>
											  <button text="{allLocal[foundUser!.local]["Place_button"]}" callback="/places"
											  </row>
											  <row> <button text="{allLocal[foundUser!.local]["Profile_button"]}" callback="/profile"
											  </row>
											  </row>
											  <row> <button text="{allLocal[foundUser!.local]["Help_button"]}" callback="/help"
											  <row> <button text="{allLocal[foundUser!.local]["Report_button"]}" callback="/report"
											  <row> <button text="{allLocal[foundUser!.local]["Language_button"]}" callback="/language"
											  </row>
											  </keyboard>
											  """);
						break;
					}
				case ("/profile"):
					{
						await bot.SendMessage(msg.Chat, $"{foundUser!.UserID} - {foundUser!.Name}", replyMarkup: new InlineKeyboardButton[][]
											 {
												[("Назад","/start")]
											 });
						break;
					}
				case ("/help"):
					{
						// TODO: обращение "по кусочкам" для вывода справки
						await bot.SendMessage(msg.Chat, "TODO");
						break;
					}
				case ("/report"):
					{
						// TODO: Сообщать нам только о тех ошибках, которые реально мешают юзерам, а не о фантомных стикерах
						await bot.SendMessage(msg.Chat, "TODO");
						break;
					}
				case ("/places"):
					{
						await bot.SendMessage(msg.Chat, "placeholderPlaces", replyMarkup: new InlineKeyboardButton[][]
											 {
												[("Столовые", "/canteens")],
												[("Буфеты/автоматы", "/buffets")],
												[("Внешние магазины", "/shops")],
                                                [("Назад", "/start")]
                                             });
						break;
					}
				case ("/canteens"):
					{
						int page = 0;
						if (args != null && !int.TryParse(args, out page))
						{
							Console.WriteLine($"{msg.Chat.Id} | Invalid command args - {msg.Text}");
							break;
						}

						int elemCounter = canteens.Count;
						if (page < 0 || page >= elemCounter)
							page = 0;

						int nowCounter = page * 5;
						await bot.SendMessage(msg.Chat, "placeholderCanteen", replyMarkup: new InlineKeyboardButton[][]
											 {
												[($"{canteens[0 + nowCounter].Name}", $"/info cants{0 + nowCounter}")],
												[($"{((elemCounter > (1 + nowCounter)) ? canteens[1 + nowCounter].Name : "")}", $"/info cants{1 + nowCounter}")],
												[($"{((elemCounter > (2 + nowCounter)) ? canteens[2 + nowCounter].Name : "")}", $"/info cants{2 + nowCounter}")],
												[($"{((elemCounter > (3 + nowCounter)) ? canteens[3 + nowCounter].Name : "")}", $"/info cants{3 + nowCounter}")],
												[($"{((elemCounter > (4 + nowCounter)) ? canteens[4 + nowCounter].Name : "")}", $"/info cants{4 + nowCounter}")],
												[($"{((page != 0) ? "◀️" : "")}", $"/canteens {page - 1}"), ("Назад","/places"), ($"{((elemCounter > (5 + nowCounter)) ? "▶️" : "")}", $"/canteens {page + 1}")]
											 });
						break;
					}
				case ("/buffets"): 
					{
						// TODO: Перенести функционал canteens
						await bot.SendMessage(msg.Chat, "TODO");
						break;
					}
				case ("/shops"):
					{
						// TODO: Перенести функционал canteens
						await bot.SendMessage(msg.Chat, "TODO");
						break;
					}
				case ("/info"):
					{
						if (args == null)
						{
							await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: /info не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
                            throw new Exception($"No command args: {msg.Text}");
                        }

						switch (args![..5])
						{
							case ("cants"):
								{
                                    string subArgs = args[5..];
                                    if (!int.TryParse(subArgs, out int index) || index > canteens.Count)
                                    {
                                        await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /info.", replyMarkup: new InlineKeyboardButton[]
                                        {
                                            ("Назад", "/places")
                                        });
                                        throw new Exception($"Invalid command agrs: {msg.Text}");
                                    }

                                    var revievsWithComment = canteens[index].PlaceRevievs.Where(x => x.Comment != null);
									await bot.SendHtml(msg.Chat.Id, $"""
											  placeholderCanteenName: {canteens[index].Name}
											  placeholderOverageRating: TODO
											  placeholderCaunteenCountReviev: {$"{canteens[index].PlaceRevievs.Count}"}
											  placeholderLastReviev: {((canteens[index].PlaceRevievs.Count != 0 && revievsWithComment.Any()) ? ($"{revievsWithComment.ToList()[^1].Rating} ⭐️| {revievsWithComment.ToList()[^1].Comment}") : "Отзывы с комментариями не найдены")}
											  <keyboard>
											  <button text="Меню" callback="/menu {args}"
											  </row>
											  <row> <button text="Оставить отзыв" callback="/sendreviev {args}"
											  <row> <button text="Отзывы" callback="/revievs {args}"
											  </row>
											  <row> <button text="Назад" callback="/canteens"
											  </row>
											  </keyboard>
											  """);
									break;
								}
							case ("bufts"):
								{
									break;
								}
							case ("shops"):
								{
									break;
								}
							default:
								{
                                    await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /info.", replyMarkup: new InlineKeyboardButton[]
                                        {
                                            ("Назад", "/places")
                                        });
                                    throw new Exception($"Invalid command agrs: {msg.Text}");
                                }
						}
						break;
					}
				case ("/menu"):
					{
						if (args == null)
						{
                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: /menu не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
                                        {
                                            ("Назад", "/places")
                                        });
                            throw new Exception($"No command args: {msg.Text}");
                        }

						ProductType? productType = null;
						bool checkUnderscore = false;
						int posUnderscore = 0, sortCorrector = 0; // sortCorrector увеличивает "рамки" поиска на 2, дабы избежать сдвигов из-за буквы сортировки
						for (int i = 0; i < args.Length; ++i)     // На 2, т.к. включаем ключевую букву F как возможность для расширения, мб в будущем будет больше сортировок
                        {
							if (args[i] == '_')
							{
								posUnderscore = i;
								checkUnderscore = true;
								break;
							}
							if (char.IsUpper(args[i]))
							{
								switch (args[i])
								{
									case ('M'):
										{
											sortCorrector = 2;
											productType = ProductType.MainDishes;
											break;
										}
									case ('S'):
										{
											sortCorrector = 2;
											productType = ProductType.SideDishes;
											break;
										}
									case ('D'):
										{
											sortCorrector = 2;
											productType = ProductType.Drinks;
											break;
										}
									case ('A'):
										{
											sortCorrector = 2;
											productType = ProductType.Appetizer;
											break;
										}
									case ('F'): // Заглушка, дабы не вызвать ошибку
										{
											break;
										}
									default:
										{
                                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /menu.", replyMarkup: new InlineKeyboardButton[]
												{
													("Назад", "/places")
												});
                                            throw new Exception($"Invalid command agrs: {msg.Text}");
                                        }
								}
							}
						}

						switch (args[..5])
						{
							case ("cants"):
								{
                                    int page = 0, index = 0;
                                    if (checkUnderscore)
                                    {
                                        if (!int.TryParse(args[5..(posUnderscore - sortCorrector)], out index) || index > canteens.Count)
                                        {
                                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /menu.", replyMarkup: new InlineKeyboardButton[]
                                                {
                                                    ("Назад", "/places")
                                                });
											throw new Exception($"Invalid command agrs: {msg.Text}");
                                        }
                                        if (!int.TryParse(args[(posUnderscore + 1)..], out page))
                                        {
                                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /menu.", replyMarkup: new InlineKeyboardButton[]
                                                {
                                                    ("Назад", "/places")
                                                });
                                            throw new Exception($"Invalid command agrs: {msg.Text}");
                                        }
                                        if (page < 0 || page >= canteens[index].Menu.Count)
                                            page = 0;
                                    }
                                    else
                                    {
                                        if (!int.TryParse(args[5..(args.Length - sortCorrector)], out index) || index > canteens.Count)
                                        {
                                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /menu.", replyMarkup: new InlineKeyboardButton[]
                                                {
                                                    ("Назад", "/places")
                                                });
                                            throw new Exception($"Invalid command agrs: {msg.Text}");
                                        }
                                    }
                                    int nowCounter = page * 20;

									var sortedCanteens = canteens[index].Menu;
                                    if (sortCorrector != 0)
									{
                                        sortedCanteens = [.. canteens[index].Menu.Where(x => x.Type == productType)];
                                    }

                                    await bot.SendHtml(msg.Chat.Id, $"""
                                    	placeholderCanteenMenu: {canteens[index].Name}
                                    	placeholderCaunteenCountMenu: {canteens[index].Menu.Count}
                                    	{(productType != null ? $"placeholderCaunteenSortMod: {productType}" : "")}
                                    	{(sortedCanteens.Count > (0 + nowCounter) ? $"{sortedCanteens[0 + nowCounter].PName} | {sortedCanteens[0 + nowCounter].Price} за {(sortedCanteens[0 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : $"Позиций по тегу {productType} не обнаружены.")}
                                    	{(sortedCanteens.Count > (1 + nowCounter) ? $"{sortedCanteens[1 + nowCounter].PName} | {sortedCanteens[1 + nowCounter].Price} за {(sortedCanteens[1 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (2 + nowCounter) ? $"{sortedCanteens[2 + nowCounter].PName} | {sortedCanteens[2 + nowCounter].Price} за {(sortedCanteens[2 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (3 + nowCounter) ? $"{sortedCanteens[3 + nowCounter].PName} | {sortedCanteens[3 + nowCounter].Price} за {(sortedCanteens[3 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (4 + nowCounter) ? $"{sortedCanteens[4 + nowCounter].PName} | {sortedCanteens[4 + nowCounter].Price} за {(sortedCanteens[4 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (5 + nowCounter) ? $"{sortedCanteens[5 + nowCounter].PName} | {sortedCanteens[5 + nowCounter].Price} за {(sortedCanteens[5 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (6 + nowCounter) ? $"{sortedCanteens[6 + nowCounter].PName} | {sortedCanteens[6 + nowCounter].Price} за {(sortedCanteens[6 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (7 + nowCounter) ? $"{sortedCanteens[7 + nowCounter].PName} | {sortedCanteens[7 + nowCounter].Price} за {(sortedCanteens[7 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (8 + nowCounter) ? $"{sortedCanteens[8 + nowCounter].PName} | {sortedCanteens[8 + nowCounter].Price} за {(sortedCanteens[8 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (9 + nowCounter) ? $"{sortedCanteens[9 + nowCounter].PName} | {sortedCanteens[9 + nowCounter].Price} за {(sortedCanteens[9 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (10 + nowCounter) ? $"{sortedCanteens[10 + nowCounter].PName} | {sortedCanteens[10 + nowCounter].Price} за {(sortedCanteens[10 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (11 + nowCounter) ? $"{sortedCanteens[11 + nowCounter].PName} | {sortedCanteens[11 + nowCounter].Price} за {(sortedCanteens[11 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (12 + nowCounter) ? $"{sortedCanteens[12 + nowCounter].PName} | {sortedCanteens[12 + nowCounter].Price} за {(sortedCanteens[12 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (13 + nowCounter) ? $"{sortedCanteens[13 + nowCounter].PName} | {sortedCanteens[13 + nowCounter].Price} за {(sortedCanteens[13 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (14 + nowCounter) ? $"{sortedCanteens[14 + nowCounter].PName} | {sortedCanteens[14 + nowCounter].Price} за {(sortedCanteens[14 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (15 + nowCounter) ? $"{sortedCanteens[15 + nowCounter].PName} | {sortedCanteens[15 + nowCounter].Price} за {(sortedCanteens[15 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (16 + nowCounter) ? $"{sortedCanteens[16 + nowCounter].PName} | {sortedCanteens[16 + nowCounter].Price} за {(sortedCanteens[16 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (17 + nowCounter) ? $"{sortedCanteens[17 + nowCounter].PName} | {sortedCanteens[17 + nowCounter].Price} за {(sortedCanteens[17 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (18 + nowCounter) ? $"{sortedCanteens[18 + nowCounter].PName} | {sortedCanteens[18 + nowCounter].Price} за {(sortedCanteens[18 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	{(sortedCanteens.Count > (19 + nowCounter) ? $"{sortedCanteens[19 + nowCounter].PName} | {sortedCanteens[19 + nowCounter].Price} за {(sortedCanteens[19 + nowCounter].IsPer100G ? "100 грамм" : "порцию")}" : "")}
                                    	<keyboard>
                                    	</row>
                                    	<row><button text="{(canteens[index].Menu.Where(x => x.Type == ProductType.MainDishes).Any() ? "Блюда" : "")}" callback="/menu {args[..5] + index.ToString()}FM"
                                    	<row><button text="{(canteens[index].Menu.Where(x => x.Type == ProductType.SideDishes).Any() ? "Гарниры" : "")}" callback="/menu {args[..5] + index.ToString()}FS"
                                    	<row><button text="{(canteens[index].Menu.Where(x => x.Type == ProductType.Drinks).Any() ? "Напитки" : "")}" callback="/menu {args[..5] + index.ToString()}FD"
                                    	<row><button text="{(canteens[index].Menu.Where(x => x.Type == ProductType.Appetizer).Any() ? "Десерты" : "")}" callback="/menu {args[..5] + index.ToString()}FA"
                                    	</row>
                                    	<row><button text="{((nowCounter != 0) ? "◀️" : "")}" callback="/menu {(posUnderscore == 0 ? $"{args}_{page - 1}" : $"{args[..posUnderscore]}_{page - 1}")}"
                                    	<row><button text="Назад" callback="/info {args[..5] + index.ToString()}"
                                    	<row><button text="{(sortedCanteens.Count > (20 + nowCounter) ? "▶️" : "")}" callback="/menu {(posUnderscore == 0 ? $"{args}_{page + 1}" : $"{args[..posUnderscore]}_{page + 1}")}"
                                    	</row>
                                    	</keyboard>
                                    	""");
									break;
								}
							case ("bufts"):
								{
									break;
								}
							case ("shops"):
								{
									break;
								}
							default:
								{
                                    await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /menu.", replyMarkup: new InlineKeyboardButton[]
                                                {
                                                    ("Назад", "/places")
                                                });
                                    throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}
						break;
					}
				case ("/revievs"):
					{
						if (args == null)
						{
                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: /revievs не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
                                        {
                                            ("Назад", "/places")
                                        });
                            throw new Exception($"No command args: {msg.Text}");
                        }

						char? modeSelector = null;
						bool checkUnderscore = false;
						int posUnderscore = 0, sortCorrector = 0; // sortCorrector увеличивает "рамки" поиска на 2, дабы избежать сдвигов из-за буквы сортировки
						for (int i = 0; i < args.Length; ++i)     // На 2, т.к. включаем ключевую букву F как возможность для расширения, мб в будущем будет больше сортировок
						{
							if (args[i] == '_')
							{
								posUnderscore = i;
								checkUnderscore = true;
								break;
							}
							if (char.IsUpper(args[i]))
							{
								switch (args[i])
								{
									case ('H'): // Высокий рейтинг
										{
											modeSelector = 'H';
											sortCorrector = 2;
											break;
										}
									case ('L'): // Низкий рейтинг
										{
											modeSelector = 'L';
											sortCorrector = 2;
											break;
										}
									case ('F'): // Заглушка, дабы не вызвать ошибку
										{
											break;
										}
									default:
										{
                                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /revievs.", replyMarkup: new InlineKeyboardButton[]
												{
													("Назад", "/places")
												});
                                            throw new Exception($"Invalid command agrs: {msg.Text}");
                                        }
								}
							}
						}

						switch (args[..5])
						{
							case ("cants"):
								{
                                    int page = 0, index = 0;
                                    if (checkUnderscore)
                                    {
                                        if (!int.TryParse(args[5..(posUnderscore - sortCorrector)], out index) || index > canteens.Count)
                                        {
                                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /revievs.", replyMarkup: new InlineKeyboardButton[]
                                                {
                                                    ("Назад", "/places")
                                                });
                                            throw new Exception($"Invalid command agrs: {msg.Text}");
                                        }
                                        if (!int.TryParse(args[(posUnderscore + 1)..], out page))
                                        {
                                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /revievs.", replyMarkup: new InlineKeyboardButton[]
                                                {
                                                    ("Назад", "/places")
                                                });
                                            throw new Exception($"Invalid command agrs: {msg.Text}");
                                        }
                                        if (page < 0 || page >= canteens[index].Menu.Count)
                                            page = 0;
                                    }
                                    else
                                    {
                                        if (!int.TryParse(args[5..(args.Length - sortCorrector)], out index) || index > canteens.Count)
                                        {
                                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /revievs.", replyMarkup: new InlineKeyboardButton[]
                                                {
                                                    ("Назад", "/places")
                                                });
                                            throw new Exception($"Invalid command agrs: {msg.Text}");
                                        }
                                    }
                                    int nowCounter = page * 5;

                                    var sortedRevievs = canteens[index].PlaceRevievs
                                                    .Select(x => new
                                                    {
                                                        x.Rating,
                                                        x.Comment
                                                    })
                                                    .Reverse()
                                                    .ToList();
									switch (modeSelector)
									{
										case ('H'):
											{
												sortedRevievs = [.. sortedRevievs.OrderBy(x => x.Rating).Reverse()];
                                                break;
											}
										case ('L'):
											{
                                                sortedRevievs = [.. sortedRevievs.OrderBy(x => x.Rating)];
                                                break;
											}
									}

                                    await bot.SendHtml(msg.Chat.Id, $"""
													placeholderCanteenReviev: {canteens[index].Name}
													placeholderOverageRating: TODO
													placeholderCaunteenCountReviev: {$"{canteens[index].PlaceRevievs.Count}"}
													placeholderCaunteenCountRevievWithComment: {$"{canteens[index].PlaceRevievs.Where(x => x.Comment != null).Count()}"}

													{((sortedRevievs.Count > (0 + nowCounter) && sortedRevievs[0 + nowCounter].Comment != null) ? $"{sortedRevievs[0 + nowCounter].Rating}⭐️| {sortedRevievs[0 + nowCounter].Comment}" : "Отзывы с комментариями не найдены.")}
													{((sortedRevievs.Count > (1 + nowCounter) && sortedRevievs[1 + nowCounter].Comment != null) ? $"{sortedRevievs[1 + nowCounter].Rating}⭐️| {sortedRevievs[1 + nowCounter].Comment}" : "")}
													{((sortedRevievs.Count > (2 + nowCounter) && sortedRevievs[2 + nowCounter].Comment != null) ? $"{sortedRevievs[2 + nowCounter].Rating}⭐️| {sortedRevievs[2 + nowCounter].Comment}" : "")}
													{((sortedRevievs.Count > (3 + nowCounter) && sortedRevievs[3 + nowCounter].Comment != null) ? $"{sortedRevievs[3 + nowCounter].Rating}⭐️| {sortedRevievs[3 + nowCounter].Comment}" : "")}
													{((sortedRevievs.Count > (4 + nowCounter) && sortedRevievs[4 + nowCounter].Comment != null) ? $"{sortedRevievs[4 + nowCounter].Rating}⭐️| {sortedRevievs[4 + nowCounter].Comment}" : "")}
													<keyboard>
													</row>
													<row><button text="{((sortedRevievs.Count > 1 && modeSelector != null) ? "Без сортировки" : "")}" callback="/revievs {args[..5] + index.ToString()}"
													<row><button text="{((sortedRevievs.Count > 1 && modeSelector != 'H') ? "Оценка ↑" : "")}" callback="/revievs {args[..5] + index.ToString()}FH"
													<row><button text="{((sortedRevievs.Count > 1 && modeSelector != 'L') ? "Оценка ↓" : "")}" callback="/revievs {args[..5] + index.ToString()}FL"
													</row>
													<row><button text="{((nowCounter != 0) ? "◀️" : "")}" callback="/revievs {(posUnderscore == 0 ? $"{args}_{page - 1}" : $"{args[..posUnderscore]}_{page - 1}")}"
													<row><button text="Назад" callback="/info {args[..5] + index.ToString()}"
													<row><button text="{(sortedRevievs.Count > (5 + nowCounter) ? "▶️" : "")}" callback="/revievs {(posUnderscore == 0 ? $"{args}_{page + 1}" : $"{args[..posUnderscore]}_{page + 1}")}"
													</row>
													</keyboard>
													""");
									break;
								}
							case ("bufts"):
								{
									await bot.SendMessage(msg.Chat.Id, "TODO");
									break;
								}
							case ("shops"):
								{
                                    await bot.SendMessage(msg.Chat.Id, "TODO");
                                    break;
								}
							default:
								{
                                    await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /revievs.", replyMarkup: new InlineKeyboardButton[]
                                                {
                                                    ("Назад", "/places")
                                                });
                                    throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						break;
					}
				case ("/sendreviev"):
					{
						if (args == null)
						{
                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: /sendreviev не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
								{
                                ("Назад", "/places")
								});
                            throw new Exception($"No command agrs: {msg.Text}");
                        }

						switch (args[..5])
						{
							case ("cants"):
								{
									if (!int.TryParse(args[5..args.Length], out int index) || index > canteens.Count)
									{
                                        await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /sendreviev.", replyMarkup: new InlineKeyboardButton[]
											{
											("Назад", "/places")
											});
                                        throw new Exception($"Invalid command agrs: {msg.Text}");
                                    }

									if (canteens[index].PlaceRevievs.Where(x => x.FromID == foundUser!.UserID).Any())
										await bot.SendHtml(msg.Chat.Id, $"""
														Вы уже оставили отзыв на {canteens[index].Name}

														• Оценка: {usersState[foundUser!.UserID].Rating}
														• Комментарий: {usersState[foundUser!.UserID].Comment ?? "Отсутствует"}

														<keyboard>
														</row>
														<row><button text="Изменить" callback="/deletereviev {args[..5] + index.ToString()}_"
														<row><button text="Удалить" callback="/deletereviev {args[..5] + index.ToString()}"
														</row>
														<row><button text="Назад" callback="/info {args[..5] + index.ToString()}"
														</row>
														</keyboard>
														"""); // _ у изменить в конце обозначает "модификатор" запроса, но т.к. он может быть один, то нет дальнейшего пояснения
									else
										switch (usersState[foundUser!.UserID].Action)
										{
											case (null):
												{
													usersState[foundUser!.UserID].Action = UserAction.RatingRequest;
													usersState[foundUser!.UserID].RefTo = args[..5] + index.ToString();

													await bot.SendMessage(msg.Chat, "Введите оценку от 1⭐️ до 10⭐️", replyMarkup: new ForceReplyMarkup());
													break;
												}
											case (UserAction.RatingRequest):
												{
													if (usersState[foundUser!.UserID].RefTo != args[..5] + index.ToString())
													{
                                                        await bot.SendMessage(msg.Chat, "Зафиксирована попытка оставить отзыв на другую точку. Сброс ранее введённой информации...");
                                                        usersState[foundUser!.UserID].Action = null;
														await OnCommand("/sendreviev", args[..5] + index.ToString(), msg);
													}
													break;
												}
											case (UserAction.CommentRequest):
												{
                                                    if (usersState[foundUser!.UserID].RefTo != args[..5] + index.ToString())
                                                    {
                                                        await bot.SendMessage(msg.Chat, $"Зафиксирована попытка оставить отзыв на другую точку. Сброс ранее введённой информации...");
                                                        usersState[foundUser!.UserID].Action = null;
                                                        await OnCommand("/sendreviev", args[..5] + index.ToString(), msg);
                                                    }
													break;
												}
											default:
												{
													Reviev reviev = new(foundUser!.UserID, usersState[foundUser!.UserID].Rating, usersState[foundUser!.UserID].Comment);
													usersState[foundUser!.UserID].Action = null;

													if (canteens[index].AddRevievs(reviev))
													{
														await bot.SendMessage(msg.Chat.Id, $"Отзыв успешно оставлен!");
														await OnCommand("/info", usersState[foundUser!.UserID].RefTo, msg);
													}
													else
													{
														await bot.SendMessage(msg.Chat.Id, $"Ошибка при попытке оставить отзыв: {reviev.Rating}⭐️| {reviev.Comment ?? "Комментарий отсутствует"}", replyMarkup: new InlineKeyboardButton[]
															{
																("Назад", $"/info {usersState[foundUser!.UserID].RefTo}")
															});
														throw new Exception($"Error while user {foundUser!.UserID} trying to leave a review on {usersState[foundUser!.UserID].RefTo}. {reviev.Rating} | {reviev.Comment ?? "No comment"}");
													}
													break;
												}
										}
									break;
								}
							case ("bufts"):
								{
                                    await bot.SendMessage(msg.Chat.Id, "TODO");
                                    break;
								}
							case ("shops"):
								{
                                    await bot.SendMessage(msg.Chat.Id, "TODO");
                                    break;
								}
							default:
								{
                                    await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /sendreviev.", replyMarkup: new InlineKeyboardButton[]
                                            {
                                            ("Назад", "/places")
                                            });
                                    throw new Exception($"Invalid command agrs: {msg.Text}");
                                }
						}
						break;
					}
				case ("/deletereviev"):
					{
						if (args == null)
						{
                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: /deletereviev не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
                                {
                                ("Назад", "/places")
                                });
                            throw new Exception($"No command agrs: {msg.Text}");
                        }

						switch (args[..5])
						{
							case ("cants"):
								{
									int index;
									if (args[^1] == '_')
									{
										if (!int.TryParse(args[5..^1], out index) || index > canteens.Count)
										{
                                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /deletereviev.", replyMarkup: new InlineKeyboardButton[]
												{
													("Назад", "/places")
												});
                                            throw new Exception($"Invalid command agrs: {msg.Text}");
                                        }

										if (canteens[index].DeleteRevievs(foundUser!.UserID))
										{
											usersState[foundUser!.UserID].Action = null;
											await OnCommand("/sendreviev", $"cants{index}", msg);
											break;
										}
									}
									else
									{
										if (!int.TryParse(args[5..], out index) || index > canteens.Count)
										{
                                            await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /deletereviev.", replyMarkup: new InlineKeyboardButton[]
                                                {
                                                    ("Назад", "/places")
                                                });
                                            throw new Exception($"Invalid command agrs: {msg.Text}");
                                        }

										if (canteens[index].DeleteRevievs(foundUser!.UserID))
										{
											await bot.SendMessage(msg.Chat.Id, $"Отзыв на {canteens[index].Name} успешно удалён!");
											await OnCommand("/info", $"cants{index}", msg);
											break;
										}
									}
                                    await bot.SendMessage(msg.Chat.Id, $"Ошибка при попытке удалить отзыв на {canteens[index].Name}", replyMarkup: new InlineKeyboardButton[]
                                                {
                                                    ("Назад", $"/info cants{index}")
                                                });
                                    throw new Exception($"Error while user {foundUser!.UserID} trying to delite/change reviev on {canteens[index].Name}");
								}
							case ("bufts"):
								{
                                    await bot.SendMessage(msg.Chat.Id, "TODO");
                                    break;
								}
							case ("shops"):
								{
                                    await bot.SendMessage(msg.Chat.Id, "TODO");
                                    break;
								}
							default:
								{
                                    await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /deletereviev.", replyMarkup: new InlineKeyboardButton[]
                                                {
                                                    ("Назад", "/places")
                                                });
                                    throw new Exception($"Invalid command agrs: {msg.Text}");
                                }
						}

						break;
					}
				case ("/admin"):
					{
						break;
					}
				default:
					{
                        await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: неизвестная команда.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/start")
							});
						break;
					}

			}
		}

		async Task OnUpdate(Update update)
		{
			switch (update)
			{
				case { CallbackQuery: { } query }:
					{
						await OnCallbackQuery(query);
						break;
					}
				default:
					{
						Console.WriteLine($"Received unhandled update {update.Type}");
						break;
					}
			}
		}

		async Task OnCallbackQuery(CallbackQuery callbackQuery)
		{
			if (callbackQuery.Data![0] == '/')
			{
				await bot.AnswerCallbackQuery(callbackQuery.Id);

				var splitStr = callbackQuery.Data.Split(' ');
				if (splitStr.Length > 1)
					await OnCommand(splitStr[0], splitStr[1], callbackQuery.Message!);
				else
					await OnCommand(splitStr[0], null, callbackQuery.Message!);
			}
			else if (callbackQuery.Data == "callback_resetAction")
			{
				await bot.AnswerCallbackQuery(callbackQuery.Id);

				var foundUser = profiles
							.Where(x => x.UserID == callbackQuery.Message!.Chat.Id)
							.FirstOrDefault();

				if (foundUser == null)
				{
					await OnCommand("/start", null, callbackQuery.Message!);
				}

				usersState[foundUser!.UserID].Action = null;
				await OnCommand("/info", usersState[foundUser!.UserID].RefTo, callbackQuery.Message!);
			}
			else
				Console.WriteLine($"Received unhandled callbackQuery {callbackQuery.Data}");
		}
	}

	static Dictionary<string, Dictionary<string, string>> SetLocal()
	{
		Dictionary<string, Dictionary<string, string>> allLocale = [];

		foreach(var localFile in Directory.GetFiles(GetPath(@"Include\Local")))
		{
            Dictionary<string, string> oneLocale = [];
            foreach (var localText in File.ReadAllText(localFile).Split('@'))
			{
				if (string.IsNullOrEmpty(localText))
					continue;
				if (localText[0] == '#')
					continue;

				var splitText = localText.Split('=');
				if (splitText.Length != 2)
					throw new Exception($"Can't load locale {splitText[0]} in {localFile}");

				oneLocale.Add(splitText[0].Trim(), splitText[1].Trim());
			}
			allLocale.Add(localFile.Split('\\').Last()[..3], oneLocale);
        }
        return allLocale;
    }

    static string GetPath(string path) => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\", path));
}