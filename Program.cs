using OBED.Include;
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

		// TODO: переход на SQL
		List<Person> persons = [];

		List<Canteen> canteens = [new("Canteen1", 1, 1, null, null, null, null),
			new("Canteen2", 2, 2, null, null, null, null),
			new("Canteen3", 2, 2, null, null, null, null),
			new("Canteen4", 2, 2, null, null, null, null),
			new("Canteen5", 2, 2, null, null, null, null),
			new("Canteen6", 2, 2, null, null, null, null),
			new("Canteen7", 2, 2, null, null, null, null),
			new("Canteen8", 2, 2, null, null, null, null),
			new("Canteen9", 2, 2, null, null, null, null),
			new("Canteen10", 2, 2, null, null, null, null),
			new("Canteen11", 2, 2, null, null, null, null),
			new("Canteen12", 2, 2, null, null, null, null),
			new("Canteen13", 2, 2, null, null, null, null),
			new("Canteen14", 2, 2, null, null, null, null),
			new("Canteen15", 2, 2, null, null, null, null),
			new("Canteen16", 3, 3, null, null, null, null)];
		List<Buffet> buffets = [new("Buffet1", 1, 1, null, null, null, null),
			new("Buffet2", 2, 2, null, null, null, null),
			new("Buffet3", 3, 3, null, null, null, null)];
		List<Grocery> groceries = [new("Grocery1", null, null, null, null),
			new("Grocery2", null, null, null, null),
			new("Grocery3", null, null, null, null)];

		// TODO: переход на noSQL
		Dictionary<long, UserState> usersState = []; 

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

						var foundUser = persons
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
										await bot.SendMessage(msg.Chat, $"Введите текст отзыва или откажитесь от сообщения отправив -", replyMarkup: new ForceReplyMarkup());
										break;
									}

									await bot.SendMessage(msg.Chat, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит только цифры или они входят в промежуток от 1 до 10", replyMarkup: new ForceReplyMarkup());
									break;
								}
							case (UserAction.CommentRequest):
								{
									if (string.IsNullOrWhiteSpace(msg.Text))
									{
										await bot.SendMessage(msg.Chat, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит текст или откажитесь от сообщения отправив -", replyMarkup: new ForceReplyMarkup());
										break;
									}

									usersState[foundUser.UserID].Comment = msg.Text.Trim();

									usersState[foundUser.UserID].Action = UserAction.NoActiveRequest;
									await bot.SendHtml(msg.Chat.Id, $"""
										Ваш отзыв:
										
											• Оценка: {usersState[foundUser.UserID].Rating}
											• Комментарий: {((msg.Text[0] == '-') ? "Отсутствует" : usersState[foundUser.UserID].Comment)}

										Всё верно?
										<keyboard>
										<button text="Да" callback="/sendreview {usersState[foundUser.UserID].RefTo}"
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
			if (args == null)
				Console.WriteLine($"NOW COMMAND {msg.Chat.Username ?? msg.Chat.FirstName + msg.Chat.LastName}: {command}");
			else
				Console.WriteLine($"NOW COMMAND {msg.Chat.Username ?? msg.Chat.FirstName + msg.Chat.LastName}: {command} {args}");
			switch (command)
			{
				case ("/start"):
					{
						await bot.SendMessage(msg.Chat, "placeholderStart", replyMarkup: new InlineKeyboardButton[][]
											 {
												[("Места", "/places")],
												[("Профиль", "/person")],
												[("Помощь", "/help"), ("Поддержка", "/report")]
											 });

						if (!persons.Select(x => x.UserID).Contains(msg.Chat.Id))
						{
							Console.WriteLine($"REG: {msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName)}");
							persons.Add(new Person(msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName), msg.Chat.Id, RoleType.Common_User));
							usersState.Add(msg.Chat.Id, new());
						}
						break;
					}
				case ("/person"):
					{
						var foundUser = persons
							.Where(x => x.UserID == msg.Chat.Id)
							.FirstOrDefault();

						if (foundUser != null)
							await bot.SendMessage(msg.Chat, $"{foundUser.UserID} - {foundUser.Username}", replyMarkup: new InlineKeyboardButton[][]
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
												[("Столовые", "/placeSelector C")],
												[("Буфеты/автоматы", "/placeSelector B")],
												[("Внешние магазины", "/placeSelector G")],
                                                [("Назад", "/start")]
                                             });
						break;
					}
				case ("/placeSelector"):
					{
						if (args == null)
						{
							await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: /placeSelector не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						int page = 0;
						if (!char.IsLetter(args[0]) || (args.Length > 1 && !int.TryParse(args[1..], out page)))
						{
							await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /placeSelector.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"Invalid command agrs: {msg.Text}");
						}
						if (page < 0)
							page = 0;
						int nowCounter = page * 5;

						List<BasePlace> places;
						switch (args[0])
						{
							case ('C'):
								{
									places = [.. canteens.Cast<BasePlace>()];
									break;
								}
							case ('B'):
								{
									places = [.. buffets.Cast<BasePlace>()];
									break;
								}
							case ('G'):
								{
									places = [.. groceries.Cast<BasePlace>()];
									break;
								}
							default:
								{
									await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /placeSelector.", replyMarkup: new InlineKeyboardButton[]
									   {
										   ("Назад", "/places")
									   });
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						int placesCounter = places.Count;
						await bot.SendMessage(msg.Chat, "placeholder", replyMarkup: new InlineKeyboardButton[][]
											 {
												[($"{((placesCounter != 0) ? places[nowCounter].Name : "")}", $"/info {args[0]}{nowCounter}")],
												[($"{((placesCounter > ++nowCounter) ? places[nowCounter].Name : "")}", $"/info {args[0]}{nowCounter}")],
												[($"{((placesCounter > ++nowCounter) ? places[nowCounter].Name : "")}", $"/info {args[0]}{nowCounter}")],
												[($"{((placesCounter > ++nowCounter) ? places[nowCounter].Name : "")}", $"/info {args[0]}{nowCounter}")],
												[($"{((placesCounter > ++nowCounter) ? places[nowCounter].Name : "")}", $"/info {args[0]}{nowCounter}")],
												[($"{((page != 0) ? "◀️" : "")}", $"/placeSelector {args[0]}{page - 1}"), ("Назад","/places"), ($"{(placesCounter > nowCounter ? "▶️" : "")}", $"/placeSelector {args[0]}{page + 1}")]
											 });
						break;
					}
				case ("/info"):
					{
						if (args == null)
						{
							await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: /placeSelector не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						int index = 0;
						if (!char.IsLetter(args[0]) || (args.Length > 1 && !int.TryParse(args[1..], out index)))
						{
							await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /placeSelector.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"Invalid command agrs: {msg.Text}");
						}

						BasePlace place;
						switch (args[0])
						{
							case ('C'):
								{
									place = canteens[index];
									break;
								}
							case ('B'):
								{
									place = buffets[index];
									break;
								}
							case ('G'):
								{
									place = groceries[index];
									break;
								}
							default:
								{
									await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /placeSelector.", replyMarkup: new InlineKeyboardButton[]
									   {
										   ("Назад", "/places")
									   });
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						await bot.SendHtml(msg.Chat.Id, $"""
							placeholderCanteenName: {place.Name}
							placeholderOverageRating: TODO
							placeholderCaunteenCountreview: {place.Reviews.Count}
							placeholderLastreview: {((place.Reviews.Count != 0 && place.Reviews.Where(x => x.Comment != null).Any()) ? ($"{place.Reviews.Where(x => x.Comment != null).Last().Rating} ⭐️| {place.Reviews.Where(x => x.Comment != null).Last().Comment}") : "Отзывы с комментариями не найдены")}
							<keyboard>
							<button text="Меню" callback="/menu {args}"
							</row>
							<row> <button text="Оставить отзыв" callback="/sendreview {args}"
							<row> <button text="Отзывы" callback="/reviews {args}"
							</row>
							<row> <button text="Назад" callback="/placeSelector {args[0]}"
							</row>
							</keyboard>
							""");

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
											productType = ProductType.MainDish;
											break;
										}
									case ('S'):
										{
											sortCorrector = 2;
											productType = ProductType.SideDish;
											break;
										}
									case ('D'):
										{
											sortCorrector = 2;
											productType = ProductType.Drink;
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
										placeholderCaunteenCountMenu: {$"{canteens[index].Menu.Count}"}
										{(productType != null ? $"placeholderCaunteenSortMod: {productType}" : "")}
										{(sortedCanteens.Count > (0 + nowCounter) ? $"{sortedCanteens[0 + nowCounter].Name} | {sortedCanteens[0 + nowCounter].Price.value} за {(sortedCanteens[0 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : $"Позиций по тегу {productType} не обнаружены.")}
										{(sortedCanteens.Count > (1 + nowCounter) ? $"{sortedCanteens[1 + nowCounter].Name} | {sortedCanteens[1 + nowCounter].Price.value} за {(sortedCanteens[1 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (2 + nowCounter) ? $"{sortedCanteens[2 + nowCounter].Name} | {sortedCanteens[2 + nowCounter].Price.value} за {(sortedCanteens[2 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (3 + nowCounter) ? $"{sortedCanteens[3 + nowCounter].Name} | {sortedCanteens[3 + nowCounter].Price.value} за {(sortedCanteens[3 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (4 + nowCounter) ? $"{sortedCanteens[4 + nowCounter].Name} | {sortedCanteens[4 + nowCounter].Price.value} за {(sortedCanteens[4 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (5 + nowCounter) ? $"{sortedCanteens[5 + nowCounter].Name} | {sortedCanteens[5 + nowCounter].Price.value} за {(sortedCanteens[5 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (6 + nowCounter) ? $"{sortedCanteens[6 + nowCounter].Name} | {sortedCanteens[6 + nowCounter].Price.value} за {(sortedCanteens[6 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (7 + nowCounter) ? $"{sortedCanteens[7 + nowCounter].Name} | {sortedCanteens[7 + nowCounter].Price.value} за {(sortedCanteens[7 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (8 + nowCounter) ? $"{sortedCanteens[8 + nowCounter].Name} | {sortedCanteens[8 + nowCounter].Price.value} за {(sortedCanteens[8 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (9 + nowCounter) ? $"{sortedCanteens[9 + nowCounter].Name} | {sortedCanteens[9 + nowCounter].Price.value} за {(sortedCanteens[9 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (10 + nowCounter) ? $"{sortedCanteens[10 + nowCounter].Name} | {sortedCanteens[10 + nowCounter].Price.value} за {(sortedCanteens[10 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (11 + nowCounter) ? $"{sortedCanteens[11 + nowCounter].Name} | {sortedCanteens[11 + nowCounter].Price.value} за {(sortedCanteens[11 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (12 + nowCounter) ? $"{sortedCanteens[12 + nowCounter].Name} | {sortedCanteens[12 + nowCounter].Price.value} за {(sortedCanteens[12 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (13 + nowCounter) ? $"{sortedCanteens[13 + nowCounter].Name} | {sortedCanteens[13 + nowCounter].Price.value} за {(sortedCanteens[13 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (14 + nowCounter) ? $"{sortedCanteens[14 + nowCounter].Name} | {sortedCanteens[14 + nowCounter].Price.value} за {(sortedCanteens[14 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (15 + nowCounter) ? $"{sortedCanteens[15 + nowCounter].Name} | {sortedCanteens[15 + nowCounter].Price.value} за {(sortedCanteens[15 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (16 + nowCounter) ? $"{sortedCanteens[16 + nowCounter].Name} | {sortedCanteens[16 + nowCounter].Price.value} за {(sortedCanteens[16 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (17 + nowCounter) ? $"{sortedCanteens[17 + nowCounter].Name} | {sortedCanteens[17 + nowCounter].Price.value} за {(sortedCanteens[17 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (18 + nowCounter) ? $"{sortedCanteens[18 + nowCounter].Name} | {sortedCanteens[18 + nowCounter].Price.value} за {(sortedCanteens[18 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										{(sortedCanteens.Count > (19 + nowCounter) ? $"{sortedCanteens[19 + nowCounter].Name} | {sortedCanteens[19 + nowCounter].Price.value} за {(sortedCanteens[19 + nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
										<keyboard>
										</row>
										<row><button text="{(canteens[index].Menu.Where(x => x.Type == ProductType.MainDish).Any() ? "Блюда" : "")}" callback="/menu {args[..5] + index.ToString()}FM"
										<row><button text="{(canteens[index].Menu.Where(x => x.Type == ProductType.SideDish).Any() ? "Гарниры" : "")}" callback="/menu {args[..5] + index.ToString()}FS"
										<row><button text="{(canteens[index].Menu.Where(x => x.Type == ProductType.Drink).Any() ? "Напитки" : "")}" callback="/menu {args[..5] + index.ToString()}FD"
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
				case ("/reviews"):
					{
						if (args == null)
						{
							await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: /reviews не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
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
											await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /reviews.", replyMarkup: new InlineKeyboardButton[]
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
											await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /reviews.", replyMarkup: new InlineKeyboardButton[]
												{
													("Назад", "/places")
												});
											throw new Exception($"Invalid command agrs: {msg.Text}");
										}
										if (!int.TryParse(args[(posUnderscore + 1)..], out page))
										{
											await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /reviews.", replyMarkup: new InlineKeyboardButton[]
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
											await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /reviews.", replyMarkup: new InlineKeyboardButton[]
												{
													("Назад", "/places")
												});
											throw new Exception($"Invalid command agrs: {msg.Text}");
										}
									}
									int nowCounter = page * 5;

									var sortedreviews = canteens[index].Reviews
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
												sortedreviews = [.. sortedreviews.OrderBy(x => x.Rating).Reverse()];
												break;
											}
										case ('L'):
											{
												sortedreviews = [.. sortedreviews.OrderBy(x => x.Rating)];
												break;
											}
									}

									await bot.SendHtml(msg.Chat.Id, $"""
													placeholderCanteenreview: {canteens[index].Name}
													placeholderOverageRating: TODO
													placeholderCaunteenCountreview: {$"{canteens[index].Reviews.Count}"}
													placeholderCaunteenCountreviewWithComment: {$"{canteens[index].Reviews.Where(x => x.Comment != null).Count()}"}

													{((sortedreviews.Count > (0 + nowCounter) && sortedreviews[0 + nowCounter].Comment != null) ? $"{sortedreviews[0 + nowCounter].Rating}⭐️| {sortedreviews[0 + nowCounter].Comment}" : "Отзывы с комментариями не найдены.")}
													{((sortedreviews.Count > (1 + nowCounter) && sortedreviews[1 + nowCounter].Comment != null) ? $"{sortedreviews[1 + nowCounter].Rating}⭐️| {sortedreviews[1 + nowCounter].Comment}" : "")}
													{((sortedreviews.Count > (2 + nowCounter) && sortedreviews[2 + nowCounter].Comment != null) ? $"{sortedreviews[2 + nowCounter].Rating}⭐️| {sortedreviews[2 + nowCounter].Comment}" : "")}
													{((sortedreviews.Count > (3 + nowCounter) && sortedreviews[3 + nowCounter].Comment != null) ? $"{sortedreviews[3 + nowCounter].Rating}⭐️| {sortedreviews[3 + nowCounter].Comment}" : "")}
													{((sortedreviews.Count > (4 + nowCounter) && sortedreviews[4 + nowCounter].Comment != null) ? $"{sortedreviews[4 + nowCounter].Rating}⭐️| {sortedreviews[4 + nowCounter].Comment}" : "")}
													<keyboard>
													</row>
													<row><button text="{((sortedreviews.Count > 1 && modeSelector != null) ? "Без сортировки" : "")}" callback="/reviews {args[..5] + index.ToString()}"
													<row><button text="{((sortedreviews.Count > 1 && modeSelector != 'H') ? "Оценка ↑" : "")}" callback="/reviews {args[..5] + index.ToString()}FH"
													<row><button text="{((sortedreviews.Count > 1 && modeSelector != 'L') ? "Оценка ↓" : "")}" callback="/reviews {args[..5] + index.ToString()}FL"
													</row>
													<row><button text="{((nowCounter != 0) ? "◀️" : "")}" callback="/reviews {(posUnderscore == 0 ? $"{args}_{page - 1}" : $"{args[..posUnderscore]}_{page - 1}")}"
													<row><button text="Назад" callback="/info {args[..5] + index.ToString()}"
													<row><button text="{(sortedreviews.Count > (5 + nowCounter) ? "▶️" : "")}" callback="/reviews {(posUnderscore == 0 ? $"{args}_{page + 1}" : $"{args[..posUnderscore]}_{page + 1}")}"
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
									await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /reviews.", replyMarkup: new InlineKeyboardButton[]
												{
													("Назад", "/places")
												});
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						break;
					}
				case ("/sendreview"):
					{
						if (args == null)
						{
							await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: /sendreview не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
								{
								("Назад", "/places")
								});
							throw new Exception($"No command agrs: {msg.Text}");
						}

						var foundUser = persons
							.Where(x => x.UserID == msg.Chat.Id)
							.FirstOrDefault();

						if (foundUser == null)
						{
							await bot.SendMessage(msg.Chat.Id, "Вы не прошли регистрацию путём ввода /start, большая часть функций бота недоступна",
								replyMarkup: new InlineKeyboardButton[] { ("Зарегистрироваться", "/start") });
							break;
						}

						switch (args[..5])
						{
							case ("cants"):
								{
									if (!int.TryParse(args[5..args.Length], out int index) || index > canteens.Count)
									{
										await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /sendreview.", replyMarkup: new InlineKeyboardButton[]
											{
											("Назад", "/places")
											});
										throw new Exception($"Invalid command agrs: {msg.Text}");
									}

									if (canteens[index].Reviews.Where(x => x.UserID == foundUser.UserID).Any())
										await bot.SendHtml(msg.Chat.Id, $"""
														Вы уже оставили отзыв на {canteens[index].Name}

														• Оценка: {usersState[foundUser.UserID].Rating}
														• Комментарий: {usersState[foundUser.UserID].Comment ?? "Отсутствует"}

														<keyboard>
														</row>
														<row><button text="Изменить" callback="/deletereview {args[..5] + index.ToString()}_"
														<row><button text="Удалить" callback="/deletereview {args[..5] + index.ToString()}"
														</row>
														<row><button text="Назад" callback="/info {args[..5] + index.ToString()}"
														</row>
														</keyboard>
														"""); // _ у изменить в конце обозначает "модификатор" запроса, но т.к. он может быть один, то нет дальнейшего пояснения
									else
										switch (usersState[foundUser.UserID].Action)
										{
											case (null):
												{
													usersState[foundUser.UserID].Action = UserAction.RatingRequest;
													usersState[foundUser.UserID].RefTo = args[..5] + index.ToString();

													await bot.SendMessage(msg.Chat, $"Введите оценку от 1⭐️ до 10⭐️", replyMarkup: new ForceReplyMarkup());
													break;
												}
											case (UserAction.RatingRequest):
												{
													if (usersState[foundUser.UserID].RefTo != args[..5] + index.ToString())
													{
														await bot.SendMessage(msg.Chat, $"Зафиксирована попытка оставить отзыв на другую точку. Сброс ранее введённой информации...");
														usersState[foundUser.UserID].Action = null;
														await OnCommand("/sendreview", args[..5] + index.ToString(), msg);
													}
													break;
												}
											case (UserAction.CommentRequest):
												{
													if (usersState[foundUser.UserID].RefTo != args[..5] + index.ToString())
													{
														await bot.SendMessage(msg.Chat, $"Зафиксирована попытка оставить отзыв на другую точку. Сброс ранее введённой информации...");
														usersState[foundUser.UserID].Action = null;
														await OnCommand("/sendreview", args[..5] + index.ToString(), msg);
													}
													break;
												}
											default:
												{
													Review review = new(foundUser.UserID, usersState[foundUser.UserID].Rating, usersState[foundUser.UserID].Comment);
													usersState[foundUser.UserID].Action = null;

													if (canteens[index].AddReview(review))
													{
														await bot.SendMessage(msg.Chat.Id, $"Отзыв успешно оставлен!");
														await OnCommand("/info", usersState[foundUser.UserID].RefTo, msg);
													}
													else
													{
														await bot.SendMessage(msg.Chat.Id, $"Ошибка при попытке оставить отзыв: {review.Rating}⭐️| {review.Comment ?? "Комментарий отсутствует"}", replyMarkup: new InlineKeyboardButton[]
															{
																("Назад", $"/info {usersState[foundUser.UserID].RefTo}")
															});
														throw new Exception($"Error while user {foundUser.UserID} trying to leave a review on {usersState[foundUser.UserID].RefTo}. {review.Rating} | {review.Comment ?? "No comment"}");
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
									await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /sendreview.", replyMarkup: new InlineKeyboardButton[]
											{
											("Назад", "/places")
											});
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}
						break;
					}
				case ("/deletereview"):
					{
						if (args == null)
						{
							await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: /deletereview не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
								{
								("Назад", "/places")
								});
							throw new Exception($"No command agrs: {msg.Text}");
						}

						var foundUser = persons
							.Where(x => x.UserID == msg.Chat.Id)
							.FirstOrDefault();

						if (foundUser == null)
						{
							await bot.SendMessage(msg.Chat.Id, "Вы не прошли регистрацию путём ввода /start, большая часть функций бота недоступна",
								replyMarkup: new InlineKeyboardButton[] { ("Зарегистрироваться", "/start") });
							break;
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
											await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /deletereview.", replyMarkup: new InlineKeyboardButton[]
												{
													("Назад", "/places")
												});
											throw new Exception($"Invalid command agrs: {msg.Text}");
										}

										if (canteens[index].DeleteReview(foundUser.UserID))
										{
											usersState[foundUser.UserID].Action = null;
											await OnCommand("/sendreview", $"cants{index}", msg);
											break;
										}
									}
									else
									{
										if (!int.TryParse(args[5..], out index) || index > canteens.Count)
										{
											await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /deletereview.", replyMarkup: new InlineKeyboardButton[]
												{
													("Назад", "/places")
												});
											throw new Exception($"Invalid command agrs: {msg.Text}");
										}

										if (canteens[index].DeleteReview(foundUser.UserID))
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
									throw new Exception($"Error while user {foundUser.UserID} trying to delite/change review on {canteens[index].Name}");
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
									await bot.SendMessage(msg.Chat.Id, "Ошибка при запросе: некорректный аргумент команды /deletereview.", replyMarkup: new InlineKeyboardButton[]
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
								("Назад", "/places")
							});
						throw new Exception($"Invalid command: {msg.Text}");
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

				var foundUser = persons
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
}