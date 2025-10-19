using OBED.Include;
using System.Collections.Concurrent;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
	static async Task Main()
	{
		using var cts = new CancellationTokenSource();
		var token = Environment.GetEnvironmentVariable("TOKEN");
		var bot = new TelegramBotClient(token!, cancellationToken: cts.Token);
		var meBot = await bot.GetMe();

		// TODO: переход на SQL
		List<Product> products1 = [new("Main1", ProductType.MainDish, (50, false)), new("Side1", ProductType.SideDish, (100, false)), new("Drink1", ProductType.Drink, (150, false)), new("Appetizer1", ProductType.Appetizer, (200, false)),
			new("Main2", ProductType.MainDish, (250, true)), new("Side2", ProductType.SideDish, (300, true)), new("Drink2", ProductType.Drink, (350, true)), new("Appetizer2", ProductType.Appetizer, (400, true)),
			new("Main3", ProductType.MainDish, (450, false)), new("Side3", ProductType.SideDish, (500, false)), new("Drink3", ProductType.Drink, (550, false)), new("Appetizer3", ProductType.Appetizer, (600, false))];

		List<Product> products2 = [new("Main1", ProductType.MainDish, (50, false)), new("Side1", ProductType.SideDish, (100, false)), new("Drink1", ProductType.Drink, (150, false))];

		List<Product> products3 = [new("Main1", ProductType.MainDish, (50, false)), new("Side1", ProductType.SideDish, (100, false)), new("Drink1", ProductType.Drink, (150, false)), new("Appetizer1", ProductType.Appetizer, (200, false)),
			new("Main2", ProductType.MainDish, (250, true))];

		List<Product> products4 = [new("Main1", ProductType.MainDish, (50, false)), new("Side1", ProductType.SideDish, (100, false)), new("Drink1", ProductType.Drink, (150, false)), new("Appetizer1", ProductType.Appetizer, (200, false)),
			new("Main2", ProductType.MainDish, (250, true)), new("Side2", ProductType.SideDish, (300, true))];

		List<Product> products5 = [new("Main1", ProductType.MainDish, (50, false)), new("Main1", ProductType.MainDish, (50, false)), new("Main1", ProductType.MainDish, (50, false)), new("Main1", ProductType.MainDish, (50, false)),
			new("Main1", ProductType.MainDish, (50, false)), new("Main1", ProductType.MainDish, (50, false)), new("Main1", ProductType.MainDish, (50, false)), new("Main1", ProductType.MainDish, (50, false)),
			new("Main1", ProductType.MainDish, (50, false)), new("Main1", ProductType.MainDish, (50, false)), new("Main1", ProductType.MainDish, (50, false)), new("Main1", ProductType.MainDish, (50, false))];

		List<Review> reviews1 = [new(123456789, 10), new(123456789, 9), new(123456789, 8), new(123456789, 7), new(123456789, 6), new(123456789, 5), new(123456789, 4)];

		List<Review> reviews2 = [new(123456789, 10), new(123456789, 9), new(123456789, 8, "8"), new(123456789, 7, "7"), new(123456789, 6), new(123456789, 5, "5"), new(123456789, 4)];

		List<Review> reviews3 = [new(123456789, 7, "Old"), new(123456789, 9, "Old"), new(123456789, 5, "Old"), new(123456789, 10, "Old"), new(123456789, 6, "Old"), new(123456789, 8, "Old"), new(123456789, 4, "Old")];
		reviews3.Add(new(987654321, 3, "New"));

		ObjectLists.AddRangeList<Canteen>([new("Canteen1", 1, 1, null, reviews3, products1, null),
			new("Canteen2", 2, 2, null, reviews2, products2, null),
			new("Canteen3", 2, 2, null, reviews1, products3, null),
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
			new("Canteen15", 2, 2, null, reviews1, products5, null),
			new("Canteen16", 3, 3, null, reviews1, products4, null)]);
		ObjectLists.AddRangeList<Buffet>([new("Buffet1", 1, 1, null, reviews1, products1, null),
			new("Buffet2", 2, 2, null, reviews2, products2, null),
			new("Buffet3", 3, 3, null, reviews3, products4, null)]);
		ObjectLists.AddRangeList<Grocery>([new("Grocery1", null, reviews1, products1, null),
			new("Grocery2", null, reviews2, products2, null),
			new("Grocery3", null, reviews3, products4, null)]);

		reviews3.Add(new(611614145, 3, "SuperNew"));

		// TODO: переход на noSQL
		ConcurrentDictionary<long, UserState> usersState = [];

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
								break;
							}

						ObjectLists.Persons.TryGetValue(msg.Chat.Id, out Person? foundUser);

						if (foundUser == null)
						{
							await bot.SendMessage(msg.Chat, "Вы не прошли регистрацию путём ввода /start, большая часть функций бота недоступна",
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

									await bot.SendMessage(msg.Chat, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит только цифры, также они должны входить в промежуток от 1 до 10 включительно", replyMarkup: new ForceReplyMarkup());
									break;
								}
							case (UserAction.RatingChange):
								{
									if (int.TryParse(msg.Text, out int rating) && (rating > 0 && rating < 11))
									{
										usersState[foundUser.UserID].Rating = rating;
										usersState[foundUser.UserID].Comment = "-";
										usersState[foundUser.UserID].Action = UserAction.NoActiveChange;
										await OnCommand("/changeReview", $"-{usersState[foundUser.UserID].ReferenceToPlace}", msg);
										break;
									}

									await bot.SendMessage(msg.Chat, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит только цифры, также они должны входить в промежуток от 1 до 10 включительно", replyMarkup: new ForceReplyMarkup());
									break;
								}
							case (UserAction.CommentRequest):
								{
									if (string.IsNullOrWhiteSpace(msg.Text))
									{
										await bot.SendMessage(msg.Chat, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит текст или откажитесь от сообщения отправив -", replyMarkup: new ForceReplyMarkup());
										break;
									}

									usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
									usersState[foundUser.UserID].Action = UserAction.NoActiveRequest;
									await bot.SendMessage(msg.Chat, $"""
									Ваш отзыв:
									
										• Оценка: {usersState[foundUser.UserID].Rating}
										• Комментарий: {((usersState[foundUser.UserID].Comment == "-") ? "Отсутствует" : usersState[foundUser.UserID].Comment)}
									
									Всё верно?
									""", ParseMode.Html, replyMarkup: new InlineKeyboardButton[][]
									{
										[("Да", $"#sendReview {usersState[foundUser.UserID].ReferenceToPlace}"), ("Нет", $"callback_resetAction")],
									});
									break;
								}
							case (UserAction.CommentChange):
								{
									if (string.IsNullOrWhiteSpace(msg.Text))
									{
										await bot.SendMessage(msg.Chat, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит текст или откажитесь от сообщения отправив -", replyMarkup: new ForceReplyMarkup());
										break;
									}

									usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
									usersState[foundUser.UserID].Rating = 0;
									usersState[foundUser.UserID].Action = UserAction.NoActiveChange;
									await OnCommand("/changeReview", $"-{usersState[foundUser.UserID].ReferenceToPlace}", msg);
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
						if (msg.From!.IsBot)
							await bot.DeleteMessage(msg.Chat, msg.Id);

						await bot.SendMessage(msg.Chat, "Старт", replyMarkup: new InlineKeyboardButton[][]
						{
							[("Места", "/places")],
							[("Профиль", "/person")],
							[("Помощь", "/help"), ("Поддержка", "/report")]
						});

						if (!ObjectLists.Persons.ContainsKey(msg.Chat.Id))
						{
							Console.WriteLine($"REG: {msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName)}");
							ObjectLists.Persons.TryAdd(msg.Chat.Id, new Person(msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName), msg.Chat.Id, RoleType.CommonUser));
							usersState.TryAdd(msg.Chat.Id, new());
						}
						break;
					}
				case ("/person"):
					{
						ObjectLists.Persons.TryGetValue(msg.Chat.Id, out Person? foundUser);

						if (foundUser != null)
							await bot.EditMessageText(msg.Chat, msg.Id, $"{foundUser.UserID} - {foundUser.Username}", replyMarkup: new InlineKeyboardButton[][]
							{
								[("Назад","/start")]
							});
						break;
					}
				case ("/help"):
					{
						// TODO: обращение "по кусочкам" для вывода справки
						await bot.EditMessageText(msg.Chat, msg.Id, $"TODO: help", replyMarkup: new InlineKeyboardButton[][]
							{
								[("Назад","/start")]
							});
						break;
					}
				case ("/report"):
					{
						// TODO: Сообщать нам только о тех ошибках, которые реально мешают юзерам, а не о фантомных стикерах
						await bot.EditMessageText(msg.Chat, msg.Id, $"TODO: report", replyMarkup: new InlineKeyboardButton[][]
							{
								[("Назад","/start")]
							});
						break;
					}
				case ("/places"):
					{
						await bot.EditMessageText(msg.Chat, msg.Id, "Выбор типа точек", replyMarkup: new InlineKeyboardButton[][]
						{
							[("Столовые", "/placeSelector -C")],
							[("Буфеты", "/placeSelector -B")],
							[("Внешние магазины", "/placeSelector -G")],
							[("Назад", "/start")]
						});
						break;
					}
				case ("/placeSelector"):
					{
						if (args == null)
						{
							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: /placeSelector не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						int page = 0;
						if (!char.IsLetter(args[1]) || (args.Length > 2 && !int.TryParse(args[2..], out page)))
						{
							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /placeSelector.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"Invalid command agrs: {msg.Text}");
						}
						if (page < 0)
							page = 0;
						int nowCounter = page * 5;

						List<BasePlace> places;
						switch (args[1])
						{
							case ('C'):
								{
									places = [.. ObjectLists.Canteens.Cast<BasePlace>()];
									break;
								}
							case ('B'):
								{
									places = [.. ObjectLists.Buffets.Cast<BasePlace>()];
									break;
								}
							case ('G'):
								{
									places = [.. ObjectLists.Groceries.Cast<BasePlace>()];
									break;
								}
							default:
								{
									await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /placeSelector.", replyMarkup: new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						bool checker = false;
						if (places.FirstOrDefault() is ILocatedUni)
							checker = true;

						List<BasePlace> sortedPlaces = [.. places.OrderByDescending(x => (double)x.Reviews.Sum(x => x.Rating) / (x.Reviews.Count + 1))];

						if (args[0] != '-')
						{
							for (int i = 0; i < sortedPlaces.Count; ++i)
							{
								if (sortedPlaces[i] is ILocatedUni located && located.BuildingNumber != (args[0] - '0'))
								{
									sortedPlaces.RemoveAt(i);
									--i;
								}
							}
						}

						int placesCounter = sortedPlaces.Count;
						Dictionary<int, int> indexPairs = [];
						for (int i = 0; i < placesCounter; ++i)
							indexPairs.Add(i, places.IndexOf(sortedPlaces[i]));

						await bot.EditMessageText(msg.Chat, msg.Id, "Выбор точки", replyMarkup: new InlineKeyboardButton[][]
						{
							[($"{((args[0] == '-' && checker) ? "Сортировка по корпусу" : (checker ? "Отключить сортировку" : ""))}", (args[0] == '-') ? $"/buildingNumberSelector {args[1..]}" : $"/placeSelector -{args[1]}")],
							[($"{((placesCounter != 0) ? sortedPlaces[nowCounter].Name : "")}", $"{((indexPairs.Count - 1) >= nowCounter ? $"/info {args[..2]}{indexPairs[nowCounter]}_{page}" : "/places")}")],
							[($"{((placesCounter > ++nowCounter) ? sortedPlaces[nowCounter].Name : "")}", $"{((indexPairs.Count - 1) >= nowCounter ? $"/info {args[..2]}{indexPairs[nowCounter]}_{page}" : "/places")}")],
							[($"{((placesCounter > ++nowCounter) ? sortedPlaces[nowCounter].Name : "")}", $"{((indexPairs.Count - 1) >= nowCounter ? $"/info {args[..2]}{indexPairs[nowCounter]}_{page}" : "/places")}")],
							[($"{((placesCounter > ++nowCounter) ? sortedPlaces[nowCounter].Name : "")}", $"{((indexPairs.Count - 1) >= nowCounter ? $"/info {args[..2]}{indexPairs[nowCounter]}_{page}" : "/places")}")],
							[($"{((placesCounter > ++nowCounter) ? sortedPlaces[nowCounter].Name : "")}", $"{((indexPairs.Count - 1) >= nowCounter ? $"/info {args[..2]}{indexPairs[nowCounter]}_{page}" : "/places")}")],
							[($"{((page != 0) ? "◀️" : "")}", $"/placeSelector {args[..2]}{page - 1}"), ("Назад","/places"), ($"{(placesCounter > nowCounter ? "▶️" : "")}", $"/placeSelector {args[..2]}{page + 1}")]
						});
						break;
					}
				case ("/buildingNumberSelector"):
					{
						if (args == null)
						{
							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: /buildingNumberSelector не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						await bot.EditMessageText(msg.Chat, msg.Id, "Выбор точки", replyMarkup: new InlineKeyboardButton[][]
						{
							[("1", $"/placeSelector 1{args[0]}"), ("2", $"/placeSelector 2{args[0]}"), ("3", $"/placeSelector 3{args[0]}")],
							[("4", $"/placeSelector 4{args[0]}"), ("5", $"/placeSelector 5{args[0]}"), ("6", $"/placeSelector 6{args[0]}")],
							[("ИАТУ", $"/placeSelector 0{args[0]}"), ("На территории кампуса", $"/placeSelector 7{args[0]}")],
							[("Назад","/places")]
						});
						break;
					}
				case ("/info"):
					{
						if (args == null)
						{
							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: /info не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						int index = 0, placeSelectorPage = 0;
						if (args.Contains('_'))
						{
							if (!char.IsLetter(args[1]) || !int.TryParse(args[2..args.IndexOf('_')], out index) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
							{
								await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /info.", replyMarkup: new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
								throw new Exception($"Invalid command agrs: {msg.Text}");
							}
						}
						else if (!char.IsLetter(args[1]) || !int.TryParse(args[2..], out index))
						{
							
							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /info.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"Invalid command agrs: {msg.Text}");
						}

						BasePlace place;
						switch (args[1])
						{
							case ('C'):
								{
									place = ObjectLists.Canteens.ElementAt(index);
									break;
								}
							case ('B'):
								{
									place = ObjectLists.Buffets.ElementAt(index);
									break;
								}
							case ('G'):
								{
									place = ObjectLists.Groceries.ElementAt(index);
									break;
								}
							default:
								{
									await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /info.", replyMarkup: new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						await bot.EditMessageText(msg.Chat, msg.Id, $"""
						Название: {place.Name}
						Средний рейтинг: {(place.Reviews.Count != 0 ? $"{Math.Round((double)place.Reviews.Sum(x => x.Rating) / place.Reviews.Count, 2)}⭐" : "Отзывы не найдены")}
						Всего отзывов: {place.Reviews.Count}
						Последний текстовый отзыв: {((place.Reviews.Count != 0 && place.Reviews.Where(x => x.Comment != null).Any()) ? ($"{place.Reviews.Where(x => x.Comment != null).Last().Rating} ⭐️| {place.Reviews.Where(x => x.Comment != null).Last().Comment}") : "Отзывы с комментариями не найдены")}
						""", ParseMode.Html, replyMarkup: new InlineKeyboardButton[][]
						{
							[("Меню", $"/menu -{args}")],
							[("Оставить отзыв", $"/sendReview {args}"), ("Отзывы", $"/reviews N{args}")],
							[("Назад", $"/placeSelector {args[..2]}{placeSelectorPage}")]
						});
						break;
					}
				case ("/menu"):
					{
						if (args == null)
						{
							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: /menu не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						int index = 0, page = 0, placeSelectorPage = 0;
						if (args.Contains('|'))
						{
							if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('|')], out index) 
								|| !int.TryParse(args[(args.IndexOf('|') + 1)..args.IndexOf('_')], out page) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
							{
								await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /menu.", replyMarkup: new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
								throw new Exception($"Invalid command agrs: {msg.Text}");
							}
						}
						else if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('_')], out index) 
							|| !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
						{
							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /menu.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"Invalid command agrs: {msg.Text}");
						}

						if (page < 0)
							page = 0;
						int nowCounter = page * 10;

						string placeName;
						List<Product> menu;
						switch (args[2])
						{
							case ('C'):
								{
									placeName = ObjectLists.Canteens.ElementAt(index).Name;
									menu = ObjectLists.Canteens.ElementAt(index).Menu;
									break;
								}
							case ('B'):
								{
									placeName = ObjectLists.Buffets.ElementAt(index).Name;
									menu = ObjectLists.Buffets.ElementAt(index).Menu;
									break;
								}
							case ('G'):
								{
									placeName = ObjectLists.Groceries.ElementAt(index).Name;
									menu = ObjectLists.Groceries.ElementAt(index).Menu;
									break;
								}
							default:
								{
									await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /menu.", replyMarkup: new InlineKeyboardButton[]
									   {
										   ("Назад", "/places")
									   });
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						ProductType? productType = null;
						switch (args[0])
						{
							case ('M'):
								{
									productType = ProductType.MainDish;
									menu = [.. menu.Where(x => x.Type == ProductType.MainDish)];
									break;
								}
							case ('S'):
								{
									productType = ProductType.SideDish;
									menu = [.. menu.Where(x => x.Type == ProductType.SideDish)];
									break;
								}
							case ('D'):
								{
									productType = ProductType.Drink;
									menu = [.. menu.Where(x => x.Type == ProductType.Drink)];
									break;
								}
							case ('A'):
								{
									productType = ProductType.Appetizer;
									menu = [.. menu.Where(x => x.Type == ProductType.Appetizer)];
									break;
								}
						}

						await bot.EditMessageText(msg.Chat, msg.Id, $"""
						Название: {placeName}
						Всего позиций: {$"{menu.Count}"}
						{(productType != null ? $"Режим сортировки: {productType}\n" : "")}
						{(menu.Count > nowCounter ? $"{menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : $"{(productType == null ? $"Меню у {placeName} не обнаружено" : $"Позиций по тегу {productType} не обнаружено")}")}
						{(menu.Count > ++nowCounter ? $"{menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
						{(menu.Count > ++nowCounter ? $"{menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
						{(menu.Count > ++nowCounter ? $"{menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
						{(menu.Count > ++nowCounter ? $"{menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
						{(menu.Count > ++nowCounter ? $"{menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
						{(menu.Count > ++nowCounter ? $"{menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
						{(menu.Count > ++nowCounter ? $"{menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
						{(menu.Count > ++nowCounter ? $"{menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
						{(menu.Count > ++nowCounter ? $"{menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
						""", ParseMode.Html, replyMarkup: new InlineKeyboardButton[][]
						{
							[(productType == null ? "" : "Без сортировки", $"/menu -{args[1..3]}{index}_{placeSelectorPage}")],

							[(productType == ProductType.MainDish ? "" : "Блюда", $"/menu M{args[1..3]}{index}_{placeSelectorPage}"), (productType == ProductType.SideDish ? "" : "Гарниры", $"/menu S{args[1..3]}{index}_{placeSelectorPage}"),
							(productType == ProductType.Drink ? "" : "Напитки", $"/menu D{args[1..3]}{index}_{placeSelectorPage}"), (productType == ProductType.Appetizer ? "" : "Закуски", $"/menu A{args[1..3]}{index}_{placeSelectorPage}")],

							[((page != 0) ? "◀️" : "", $"/menu {args[..3]}{index}|{page - 1}_{placeSelectorPage}"), ("Назад", $"/info {args[1..3]}{index}_{placeSelectorPage}"), (menu.Count > ++nowCounter ? "▶️" : "", $"/menu {args[..3]}{index}|{page + 1}_{placeSelectorPage}")]
						});
						break;
					}
				case ("/reviews"):
					{
						if (args == null)
						{
							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: /reviews не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						int index = 0, page = 0, placeSelectorPage = 0;
						if (args.Contains('|'))
						{
							if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('|')], out index)
								|| !int.TryParse(args[(args.IndexOf('|') + 1)..args.IndexOf('_')], out page) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
							{
								await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /reviews.", replyMarkup: new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
								throw new Exception($"Invalid command agrs: {msg.Text}");
							}
						}
						else if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('_')], out index)
							|| !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
						{
							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /reviews.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"Invalid command agrs: {msg.Text}");
						}

						if (page < 0)
							page = 0;
						int nowCounter = page * 5;

						string placeName;
						List<Review> reviews;
						switch (args[2])
						{
							case ('C'):
								{
									placeName = ObjectLists.Canteens.ElementAt(index).Name;
									reviews = ObjectLists.Canteens.ElementAt(index).Reviews;
									break;
								}
							case ('B'):
								{
									placeName = ObjectLists.Buffets.ElementAt(index).Name;
									reviews = ObjectLists.Buffets.ElementAt(index).Reviews;
									break;
								}
							case ('G'):
								{
									placeName = ObjectLists.Groceries.ElementAt(index).Name;
									reviews = ObjectLists.Groceries.ElementAt(index).Reviews;
									break;
								}
							default:
								{
									await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /reviews.", replyMarkup: new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						int reviewCounter = reviews.Count;
						reviews = [.. reviews.Where(x => x.Comment != null)];

						ReviewSort? sortType = null;
						switch (args[0])
						{
							case ('U'):
								{
									sortType = ReviewSort.Upper;
									reviews = [.. reviews.OrderByDescending(x => x.Rating)];
									break;
								}
							case ('L'):
								{
									sortType = ReviewSort.Lower;
									reviews = [.. reviews.OrderBy(x => x.Rating)];
									break;
								}
							case ('N'):
								{
									sortType = ReviewSort.NewDate;
									reviews = [.. reviews.OrderByDescending(x => x.Date)];
									break;
								}
							case ('O'):
								{
									sortType = ReviewSort.OldDate;
									reviews = [.. reviews.OrderBy(x => x.Date)];
									break;
								}
						}

						await bot.EditMessageText(msg.Chat, msg.Id, $"""
						Название: {placeName}
						Всего отзывов: {$"{reviewCounter}"}
						Всего отзывов с комментариями: {$"{reviews.Count}"}
						{(sortType != null ? $"Режим сортировки: {sortType}\n" : "")}
						{(reviews.Count > nowCounter ? $"{reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : $"{(sortType == null ? $"Развёрнутые отзывы на {placeName} не обнаружено" : $"Развёрнутых отзывов по тегу {sortType} не обнаружено")}")}
						{(reviews.Count > ++nowCounter ? $"{reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}
						{(reviews.Count > ++nowCounter ? $"{reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}
						{(reviews.Count > ++nowCounter ? $"{reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}
						{(reviews.Count > ++nowCounter ? $"{reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}
						""", ParseMode.Html, replyMarkup: new InlineKeyboardButton[][]
						{
							[(sortType == ReviewSort.Upper ? "" : "Оценка ↑", $"/reviews U{args[1..3]}{index}_{placeSelectorPage}"), (sortType == ReviewSort.Lower ? "" : "Оценка ↓", $"/reviews L{args[1..3]}{index}_{placeSelectorPage}"),
							(sortType == ReviewSort.NewDate ? "" : "Новые", $"/reviews N{args[1..3]}{index}_{placeSelectorPage}"), (sortType == ReviewSort.OldDate ? "" : "Старые", $"/reviews O{args[1..3]}{index}_{placeSelectorPage}")],

							[((page != 0) ? "◀️" : "", $"/reviews {args[..3]}{index}|{page - 1}_{placeSelectorPage}"), ("Назад", $"/info {args[1..3]}{index}_{placeSelectorPage}"), (reviews.Count > ++nowCounter ? "▶️" : "", $"/reviews {args[..3]}{index}|{page + 1}_{placeSelectorPage}")]
						});
						break;
					}
				case ("/sendReview"):
					{
						if (args == null)
						{
							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: /sendReview не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						ObjectLists.Persons.TryGetValue(msg.Chat.Id, out Person? foundUser);

						if (foundUser == null)
						{
							await bot.SendMessage(msg.Chat.Id, "Вы не прошли регистрацию путём ввода /start, большая часть функций бота недоступна",
								replyMarkup: new InlineKeyboardButton[] { ("Зарегистрироваться", "/start") });
							break;
						}

						int index = 0, placeSelectorPage = 0;
						if (args.Contains('_'))
						{
							if (!char.IsLetter(args[1]) || !int.TryParse(args[2..args.IndexOf('_')], out index) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
							{
								await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /sendReview.", replyMarkup: new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
								throw new Exception($"Invalid command agrs: {msg.Text}");
							}
						}
						else if (!char.IsLetter(args[1]) || !int.TryParse(args[2..], out index))
						{

							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /sendReview.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"Invalid command agrs: {msg.Text}");
						}

						BasePlace place;
						switch (args[1])
						{
							case ('C'):
								{
									place = ObjectLists.Canteens.ElementAt(index);
									break;
								}
							case ('B'):
								{
									place = ObjectLists.Buffets.ElementAt(index);
									break;
								}
							case ('G'):
								{
									place = ObjectLists.Groceries.ElementAt(index);
									break;
								}
							default:
								{
									await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /sendReview.", replyMarkup: new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						if (place.Reviews.Where(x => x.UserID == foundUser.UserID).Any())
						{
							await bot.EditMessageText(msg.Chat, msg.Id, $"""
								Вы уже оставили отзыв на {place.Name}

								• Оценка: {place.Reviews.Where(x => x.UserID == foundUser.UserID).First().Rating}
								• Комментарий: {place.Reviews.Where(x => x.UserID == foundUser.UserID).First().Comment ?? "Отсутствует"}
								""", ParseMode.Html, replyMarkup: new InlineKeyboardButton[][]
								{
									[("Изменить", $"/changeReview -{args}"), ("Удалить", $"#deleteReview {args}")],
									[("Назад", $"/info {args}")]
								});
							break;
						}

						switch (usersState[foundUser.UserID].Action)
						{
							case (null):
								{
									usersState[foundUser.UserID].Action = UserAction.RatingRequest;
									usersState[foundUser.UserID].ReferenceToPlace = args;

									await bot.SendMessage(msg.Chat, $"Введите оценку от 1⭐️ до 10⭐️", replyMarkup: new ForceReplyMarkup());
									break;
								}
							default:
								{
									await bot.SendMessage(msg.Chat, $"Зафиксирована попытка оставить отзыв на другую точку. Сброс ранее введённой информации...");
									usersState[foundUser.UserID].Action = null;
									await OnCommand("/sendReview", args, msg);
									break;
								}
						}
						break;
					}
				case ("/changeReview"):
					{
						if (args == null)
						{
							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: /changeReview не применяется без аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						ObjectLists.Persons.TryGetValue(msg.Chat.Id, out Person? foundUser);

						if (foundUser == null)
						{
							await bot.SendMessage(msg.Chat, "Вы не прошли регистрацию путём ввода /start, большая часть функций бота недоступна",
								replyMarkup: new InlineKeyboardButton[] { ("Зарегистрироваться", "/start") });
							break;
						}

						int index = 0, placeSelectorPage = 0;
						if (args.Contains('_'))
						{
							if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('_')], out index) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
							{
								await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /sendReview.", replyMarkup: new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
								throw new Exception($"Invalid command agrs: {msg.Text}");
							}
						}
						else if (!char.IsLetter(args[2]) || !int.TryParse(args[3..], out index))
						{

							await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /sendReview.", replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"Invalid command agrs: {msg.Text}");
						}

						BasePlace place;
						switch (args[2])
						{
							case ('C'):
								{
									place = ObjectLists.Canteens.ElementAt(index);
									break;
								}
							case ('B'):
								{
									place = ObjectLists.Buffets.ElementAt(index);
									break;
								}
							case ('G'):
								{
									place = ObjectLists.Groceries.ElementAt(index);
									break;
								}
							default:
								{
									await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: некорректный аргумент команды /changeReview.", replyMarkup: new InlineKeyboardButton[]
									   {
										   ("Назад", "/places")
									   });
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						if (!place.Reviews.Where(x => x.UserID == foundUser.UserID).Any())
						{
							await bot.EditMessageText(msg.Chat, msg.Id, $"""
							Вы не можете изменить отзыв на {place.Name}

							Причина: Ваш отзыв не существует в системе
							""", ParseMode.Html, replyMarkup: new InlineKeyboardButton[]
							{
								("Назад", $"/placeSelector -{args}")
							});
							break;
						}

						switch (args[0])
						{
							case ('R'):
								{
									usersState[foundUser.UserID].Action = UserAction.RatingChange;
									usersState[foundUser.UserID].ReferenceToPlace = args[1..];

									await bot.SendMessage(msg.Chat, $"Введите НОВУЮ оценку от 1⭐️ до 10⭐️", replyMarkup: new ForceReplyMarkup());
									break;
								}
							case ('C'):
								{
									usersState[foundUser.UserID].Action = UserAction.CommentChange;
									usersState[foundUser.UserID].ReferenceToPlace = args[1..];

									await bot.SendMessage(msg.Chat, $"Введите НОВЫЙ текст отзыва или удалите его отправив -", replyMarkup: new ForceReplyMarkup());
									break;
								}
						}

						switch (usersState[foundUser.UserID].Action)
						{
							case (null):
								{
									await bot.EditMessageText(msg.Chat, msg.Id, $"""
									Что вы хотите изменить в отзыве на {place.Name}?
									""", ParseMode.Html, replyMarkup: new InlineKeyboardButton[][]
									{
										[("Оценку", $"/changeReview R{args[1..]}"), ("Комментарий", $"/changeReview C{args[1..]}")],
										[("Назад", $"/info {args[1..]}")]
									});
									break;
								}
							case (UserAction.NoActiveChange):
								{
									if (usersState[foundUser.UserID].Rating == 0)
										usersState[foundUser.UserID].Rating = place.Reviews.First(x => x.UserID == foundUser.UserID).Rating;
									if (usersState[foundUser.UserID].Comment == "-")
										usersState[foundUser.UserID].Comment = "Отсутствует";

									usersState[foundUser.UserID].Action = null;
									await bot.SendMessage(msg.Chat, $"""
									Ваш НОВЫЙ отзыв:
									
										• Оценка: {usersState[foundUser.UserID].Rating}
										• Комментарий: {usersState[foundUser.UserID].Comment}
									
									Всё верно?
									""", ParseMode.Html, replyMarkup: new InlineKeyboardButton[][]
									{
										[("Да", $"#changeReview {usersState[foundUser.UserID].ReferenceToPlace}"), ("Нет", $"/changeReview -{usersState[foundUser.UserID].ReferenceToPlace}")],
									});
									break;
								}
						}
						break;
					}
				case ("/admin"):
					{
						// TODO: при реализации runtime добавления новых точек обязательно использовать lock
						break;
					}
				default:
					{
						await bot.EditMessageText(msg.Chat, msg.Id, "Ошибка при запросе: неизвестная команда.", replyMarkup: new InlineKeyboardButton[]
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
			ArgumentNullException.ThrowIfNull(callbackQuery.Data);

			switch (callbackQuery.Data[0])
			{
				case ('/'):
					{
						await bot.AnswerCallbackQuery(callbackQuery.Id);

						var splitStr = callbackQuery.Data.Split(' ');
						if (splitStr.Length > 1)
							await OnCommand(splitStr[0], splitStr[1], callbackQuery.Message!);
						else
							await OnCommand(splitStr[0], null, callbackQuery.Message!);
						break;
					}
				case ('#'):
					{
						ArgumentNullException.ThrowIfNull(callbackQuery.Message);
						ObjectLists.Persons.TryGetValue(callbackQuery.Message.Chat.Id, out Person? foundUser);

						if (foundUser == null)
						{
							await bot.SendMessage(callbackQuery.Message.Chat.Id, "Вы не прошли регистрацию путём ввода /start, большая часть функций бота недоступна",
								replyMarkup: new InlineKeyboardButton[] { ("Зарегистрироваться", "/start") });
							break;
						}

						var splitStr = callbackQuery.Data.Split(' ');
						if (splitStr.Length < 2)
						{
							await bot.EditMessageText(callbackQuery.Message.Chat, callbackQuery.Message.Id, $"Ошибка при #{callbackQuery.Data} запросе: некорректный аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
											("Назад", "/places")
							});
							throw new Exception($"No command args: {callbackQuery.Message.Text}");
						}

						if (!char.IsLetter(splitStr[1][1]) || !int.TryParse(splitStr[1][2..splitStr[1].IndexOf('_')], out int index))
						{
							await bot.EditMessageText(callbackQuery.Message.Chat, callbackQuery.Message.Id, $"Ошибка при #{callbackQuery.Data} запросе: некорректный аргументов.", replyMarkup: new InlineKeyboardButton[]
							{
											("Назад", "/places")
							});
							throw new Exception($"Invalid command agrs: {callbackQuery.Message.Text}");
						}

						BasePlace place;
						switch (splitStr[1][1])
						{
							case ('C'):
								{
									place = ObjectLists.Canteens.ElementAt(index);
									break;
								}
							case ('B'):
								{
									place = ObjectLists.Buffets.ElementAt(index);
									break;
								}
							case ('G'):
								{
									place = ObjectLists.Groceries.ElementAt(index);
									break;
								}
							default:
								{
									await bot.EditMessageText(callbackQuery.Message.Chat, callbackQuery.Message.Id, $"Ошибка при #{callbackQuery.Data} запросе: некорректный аргументов.", replyMarkup: new InlineKeyboardButton[]
									   {
										   ("Назад", "/places")
									   });
									throw new Exception($"Invalid command agrs: {callbackQuery.Message.Text}");
								}
						}

						switch (splitStr[0][1..])
						{
							case ("sendReview"):
								{
									if (place.AddReview(foundUser.UserID, usersState[foundUser.UserID].Rating, usersState[foundUser.UserID].Comment) && usersState[foundUser.UserID].Action == UserAction.NoActiveRequest)
									{
										usersState[foundUser.UserID].Action = null;
										await bot.AnswerCallbackQuery(callbackQuery.Id, "Отзыв успешно оставлен!");
										await OnCommand("/info", usersState[foundUser.UserID].ReferenceToPlace, callbackQuery.Message);
									}
									else
									{
										await bot.EditMessageText(callbackQuery.Message.Chat, callbackQuery.Message.Id, $"Ошибка при попытке оставить отзыв: {usersState[foundUser.UserID].Rating}⭐️| {usersState[foundUser.UserID].Comment ?? "Комментарий отсутствует"}", replyMarkup: new InlineKeyboardButton[]
										{
											("Назад", $"/info {usersState[foundUser.UserID].ReferenceToPlace}")
										});
										throw new Exception($"Ошибка при попытке оставить отзыв: {usersState[foundUser.UserID].ReferenceToPlace} - {usersState[foundUser.UserID].Rating} | {usersState[foundUser.UserID].Comment ?? "Комментарий отсутствует"}");
									}

									break;
								}
							case ("deleteReview"):
								{
									if (!place.Reviews.Where(x => x.UserID == foundUser.UserID).Any())
									{
										await bot.EditMessageText(callbackQuery.Message.Chat, callbackQuery.Message.Id, $"""
										Вы не можете удалить отзыв на {place.Name}

										Причина: Ваш отзыв не существует в системе
										""", ParseMode.Html, replyMarkup: new InlineKeyboardButton[]
										{
											("Назад", $"/placeSelector {splitStr[1]}")
										});
										break;
									}

									if (place.DeleteReview(foundUser.UserID))
									{
										await bot.AnswerCallbackQuery(callbackQuery.Id, "Отзыв успешно удалён!");
										await OnCommand("/info", splitStr[1], callbackQuery.Message);
									}
									else
									{
										await bot.EditMessageText(callbackQuery.Message.Chat, callbackQuery.Message.Id, $"Ошибка при попытке удалить отзыв на {place.Name}", replyMarkup: new InlineKeyboardButton[]
										{
											("Назад", $"/info {splitStr[1]}")
										});
										throw new Exception($"Error while user {foundUser.UserID} trying to delete review on {place.Name}");
									}
									break;
								}
							case ("changeReview"):
								{
									if (usersState[foundUser.UserID].Action != null)
										break;

									place.DeleteReview(foundUser.UserID);
									place.AddReview(foundUser.UserID, usersState[foundUser.UserID].Rating, usersState[foundUser.UserID].Comment);

									await bot.AnswerCallbackQuery(callbackQuery.Id, "Отзыв успешно изменён!");
									await OnCommand("/info", usersState[foundUser.UserID].ReferenceToPlace, callbackQuery.Message);
									break;
								}
							default:
								{
									throw new InvalidDataException($"Некорректный #аргумент: {callbackQuery.Data}");
								}
						}
						break;
					}
				default:
					{
						if (callbackQuery.Data == "callback_resetAction")
						{
							await bot.AnswerCallbackQuery(callbackQuery.Id);

							ObjectLists.Persons.TryGetValue(callbackQuery.Message!.Chat.Id, out Person? foundUser);

							if (foundUser == null)
								await OnCommand("/start", null, callbackQuery.Message!);
							else
							{
								usersState[foundUser!.UserID].Action = null;
								await OnCommand("/info", usersState[foundUser!.UserID].ReferenceToPlace, callbackQuery.Message!);
							}
						}
						else
							Console.WriteLine($"Зафиксирован необработанный callbackQuery {callbackQuery.Data}");
						break;
					}
			}
		}
		static string HtmlEscape(string? s) => (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
	}
}