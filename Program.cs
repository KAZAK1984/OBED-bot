using OBED.Include;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Extensions;
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
		bot.OnMessage += OnStandarMessage;
		bot.OnUpdate += OnUpdate;

		var queueController = new PeriodicTimer(TimeSpan.FromMilliseconds(1000));
		while (await queueController.WaitForNextTickAsync())
		{
			for (int i = 0; i < SecurityManager.RequestQueue.Count; ++i)
			{
				if (SecurityManager.RequestQueue.TryPeek(out var element) && element.deferredTime < DateTime.Now)
				{
					if (element.obj is MessageData messageData)
						await OnDirectMessage(messageData.Msg, messageData.Type);
					else if (element.obj is CallbackQuery callbackQuery)
						await OnDirectCallbackQuery(callbackQuery);

                    SecurityManager.RequestQueue.TryDequeue(out _);
                    --i;
                }
			}
        }

		static string HtmlEscape(string? s) => (s ?? "-").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

		async Task EditOrSendMessage(Message msg, string text, InlineKeyboardMarkup? markup = null, ParseMode parser = ParseMode.None, bool isForceReply = false)
		{
			ArgumentNullException.ThrowIfNull(msg.From);

			if (isForceReply)
			{
				await bot.SendMessage(msg.Chat, text, parser, replyMarkup: new ForceReplyMarkup());
				return;
			}
			if (msg.From.IsBot)
			{
				try
				{
					await bot.EditMessageText(msg.Chat, msg.Id, text, parser, replyMarkup: markup);
				}
				catch (Exception ex)
				{
					if (ex is not Telegram.Bot.Exceptions.ApiRequestException)
					{
						Console.WriteLine(ex);
						await Task.Delay(2000, cts.Token);
					}	
				}
			}
			else
				await bot.SendMessage(msg.Chat, text, parser, replyMarkup: markup);
		}

		async Task OnError(Exception exception, HandleErrorSource source)
		{
			Console.WriteLine(exception);
			await Task.Delay(2000, cts.Token);
		}

        async Task OnStandarMessage(Message msg, UpdateType type)
		{
			ObjectLists.Persons.TryGetValue(msg.Chat.Id, out Person? foundUser);
			
			if (foundUser != null)
			{
				if (SecurityManager.BlockedUsers.TryGetValue(foundUser.UserID, out string? reason))
				{
					await bot.SendMessage(msg.Chat, $"Вы были заблокированы за: {reason ?? "Траблмейкинг"}.");
					return;
				}

				if (SecurityManager.SecurityCheck<MessageData>(foundUser.UserID, new(msg, type)))
					return;
			}

			await OnDirectMessage(msg, type);
		}

        async Task OnDirectMessage(Message msg, UpdateType type)
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
							await EditOrSendMessage(msg, "Вы не прошли регистрацию путём ввода /start, большая часть функций бота недоступна",
								new InlineKeyboardButton[] { ("Зарегистрироваться", "/start") });
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
											await EditOrSendMessage(msg, $"Введите текст отзыва или откажитесь от сообщения отправив -", null, ParseMode.None, true);
											break;
										}

										await EditOrSendMessage(msg, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит только цифры, также они должны входить в промежуток от 1 до 10 включительно", null, ParseMode.None, true);
										break;
									}
								case (UserAction.RatingChange):
									{
										if (int.TryParse(msg.Text, out int rating) && (rating > 0 && rating < 11))
										{
											usersState[foundUser.UserID].Rating = rating;
											usersState[foundUser.UserID].Comment = "saved_mark";
											usersState[foundUser.UserID].Action = UserAction.NoActiveChange;
											await OnCommand("/changeReview", $"-{usersState[foundUser.UserID].ReferenceToPlace}", msg);
											break;
										}

										await EditOrSendMessage(msg, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит только цифры, также они должны входить в промежуток от 1 до 10 включительно", null, ParseMode.None, true);
										break;
									}
								case (UserAction.CommentRequest):
									{
										if (string.IsNullOrWhiteSpace(msg.Text))
										{
											await EditOrSendMessage(msg, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит текст или откажитесь от сообщения отправив -", null, ParseMode.None, true);
											break;
										}

										if (msg.Text.Length > 720)
										{
											await EditOrSendMessage(msg, $"""
											Ошибка при обработке! Комментарий не может быть больше 720 символов. Текущая длина сообщения: {msg.Text.Length}.
											Убедитесь, что ваше новое сообщение содержит текст или откажитесь от сообщения отправив -
											""", null, ParseMode.Html, true);
											break;
										}
										usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
										if (usersState[foundUser.UserID].Comment == "-")
											usersState[foundUser.UserID].Comment = null;

										usersState[foundUser.UserID].Action = UserAction.NoActiveRequest;
										await EditOrSendMessage(msg, $"""
									Ваш отзыв:
									
										• Оценка: {usersState[foundUser.UserID].Rating}
										• Комментарий: {usersState[foundUser.UserID].Comment ?? "Отсутствует"}
									
									Всё верно?
									""", new InlineKeyboardButton[][]
										{
										[("Да", $"#sendReview {usersState[foundUser.UserID].ReferenceToPlace}"), ("Нет", $"callback_resetAction")],
										}, ParseMode.Html);
										break;
									}
								case (UserAction.CommentChange):
									{
										if (string.IsNullOrWhiteSpace(msg.Text))
										{
											await EditOrSendMessage(msg, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит текст или откажитесь от сообщения отправив -", null, ParseMode.None, true);
											break;
										}

										usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
										usersState[foundUser.UserID].Rating = 0;
										usersState[foundUser.UserID].Action = UserAction.NoActiveChange;
										await OnCommand("/changeReview", $"-{usersState[foundUser.UserID].ReferenceToPlace}", msg);
										break;
									}
								case (UserAction.Moderation):
									{
										if (string.IsNullOrWhiteSpace(msg.Text))
										{
											await EditOrSendMessage(msg, $"Ошибка при обработке! Убедитесь, что ваше сообщение содержит текст или удалите сообщение отправив -", null, ParseMode.None, true);
											break;
										}

										usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
										usersState[foundUser.UserID].Action = UserAction.NoActiveModeration;
										await OnCommand("/admin", "chkA", msg);
										break;
									}
							}
						break;
					}
			}
		}

		async Task OnCommand(string command, string? args, Message msg)
		{
			ObjectLists.Persons.TryGetValue(msg.Chat.Id, out Person? foundUser);

			if (foundUser == null && command != "/start")
			{
				await EditOrSendMessage(msg, "Вы не прошли регистрацию путём ввода /start, большая часть функций бота недоступна",
					new InlineKeyboardButton[] { ("Зарегистрироваться", "/start") });
				return;
			}

			if (args == null)
				Console.WriteLine($"NOW COMMAND {msg.Chat.Username ?? msg.Chat.FirstName + msg.Chat.LastName}: {command}");
			else
				Console.WriteLine($"NOW COMMAND {msg.Chat.Username ?? msg.Chat.FirstName + msg.Chat.LastName}: {command} {args}");
			switch (command)
			{
				case ("/start"):
					{
						if (foundUser == null)
						{
							Console.WriteLine($"REG: {msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName)}");
							ObjectLists.Persons.TryAdd(msg.Chat.Id, new Person(msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName), msg.Chat.Id, RoleType.CommonUser));
							usersState.TryAdd(msg.Chat.Id, new());
							ObjectLists.Persons.TryGetValue(msg.Chat.Id, out foundUser);

							if (foundUser!.UserID == 1204402944)
								foundUser.SetRole(RoleType.Administrator);
						}

						await EditOrSendMessage(msg, "Старт", new InlineKeyboardButton[][]
						{
							[("Места", "/places")],
							[("Профиль", "/person")],
							[("Помощь", "/help"), ("Поддержка", "/report")],
							[(foundUser!.Role == RoleType.Administrator ? "Админ панель" : "", "/admin")]
						});
						break;
					}
				case ("/person"):
					{
						await EditOrSendMessage(msg, $"""
						Ваше имя: {foundUser!.Username} ({foundUser!.UserID})
						Ваш статус: {foundUser!.Role}
						""", new InlineKeyboardButton[]
						{
							("Назад","/start")
							}, ParseMode.Html);
						break;
					}
				case ("/help"):
					{
						// TODO: обращение "по кусочкам" для вывода справки
						await EditOrSendMessage(msg, $"TODO: help", new InlineKeyboardButton[][]
							{
								[("Назад","/start")]
							});
						break;
					}
				case ("/report"):
					{
						// TODO: Сообщать нам только о тех ошибках, которые реально мешают юзерам, а не о фантомных стикерах
						await EditOrSendMessage(msg, $"TODO: report", new InlineKeyboardButton[][]
							{
								[("Назад","/start")]
							});
						break;
					}
				case ("/places"):
					{
						await EditOrSendMessage(msg, "Выбор типа точек", new InlineKeyboardButton[][]
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
							await EditOrSendMessage(msg, "Ошибка при запросе: /placeSelector не применяется без аргументов.", new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						int page = 0;
						if (!char.IsLetter(args[1]) || (args.Length > 2 && !int.TryParse(args[2..], out page)))
						{
							await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /placeSelector.", new InlineKeyboardButton[]
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
									await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /placeSelector.", new InlineKeyboardButton[]
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

						await EditOrSendMessage(msg, "Выбор точки", new InlineKeyboardButton[][]
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
							await EditOrSendMessage(msg, "Ошибка при запросе: /buildingNumberSelector не применяется без аргументов.", new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						await EditOrSendMessage(msg, "Выбор точки", new InlineKeyboardButton[][]
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
							await EditOrSendMessage(msg, "Ошибка при запросе: /info не применяется без аргументов.", new InlineKeyboardButton[]
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
								await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /info.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
								throw new Exception($"Invalid command agrs: {msg.Text}");
							}
						}
						else if (!char.IsLetter(args[1]) || !int.TryParse(args[2..], out index))
						{

							await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /info.", new InlineKeyboardButton[]
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
									await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /info.", new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						await EditOrSendMessage(msg, $"""
						Название: {place.Name}
						Средний рейтинг: {(place.Reviews.Count != 0 ? $"{Math.Round((double)place.Reviews.Sum(x => x.Rating) / place.Reviews.Count, 2)}⭐" : "Отзывы не найдены")}
						Всего отзывов: {place.Reviews.Count}
						Последний текстовый отзыв: {((place.Reviews.Count != 0 && place.Reviews.Where(x => x.Comment != null).Any()) ? ($"{place.Reviews.Where(x => x.Comment != null).Last().Rating} ⭐️| {place.Reviews.Where(x => x.Comment != null).Last().Comment}") : "Отзывы с комментариями не найдены")}
						""", new InlineKeyboardButton[][]
						{
							[("Меню", $"/menu -{args}")],
							[("Оставить отзыв", $"/sendReview {args}"), ("Отзывы", $"/reviews N{args}")],
							[((foundUser!.Role == RoleType.Administrator && place.Reviews.Count != 0) ? "Панель удаления" : "", $"/admin delN{args}")],
							[("Назад", $"/placeSelector {args[..2]}{placeSelectorPage}")]
						}, ParseMode.Html);
						break;
					}
				case ("/menu"):
					{
						if (args == null)
						{
							await EditOrSendMessage(msg, "Ошибка при запросе: /menu не применяется без аргументов.", new InlineKeyboardButton[]
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
								await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /menu.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
								throw new Exception($"Invalid command agrs: {msg.Text}");
							}
						}
						else if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('_')], out index)
							|| !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
						{
							await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /menu.", new InlineKeyboardButton[]
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
									await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /menu.", new InlineKeyboardButton[]
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

						await EditOrSendMessage(msg, $"""
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
						""", new InlineKeyboardButton[][]
						{
							[(productType == null ? "" : "Без сортировки", $"/menu -{args[1..3]}{index}_{placeSelectorPage}")],

							[(productType == ProductType.MainDish ? "" : "Блюда", $"/menu M{args[1..3]}{index}_{placeSelectorPage}"), (productType == ProductType.SideDish ? "" : "Гарниры", $"/menu S{args[1..3]}{index}_{placeSelectorPage}"),
							(productType == ProductType.Drink ? "" : "Напитки", $"/menu D{args[1..3]}{index}_{placeSelectorPage}"), (productType == ProductType.Appetizer ? "" : "Закуски", $"/menu A{args[1..3]}{index}_{placeSelectorPage}")],

							[((page != 0) ? "◀️" : "", $"/menu {args[..3]}{index}|{page - 1}_{placeSelectorPage}"), ("Назад", $"/info {args[1..3]}{index}_{placeSelectorPage}"), (menu.Count > ++nowCounter ? "▶️" : "", $"/menu {args[..3]}{index}|{page + 1}_{placeSelectorPage}")]
						}, ParseMode.Html);
						break;
					}
				case ("/reviews"):
					{
						if (args == null)
						{
							await EditOrSendMessage(msg, "Ошибка при запросе: /reviews не применяется без аргументов.", new InlineKeyboardButton[]
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
								await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /reviews.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
								throw new Exception($"Invalid command agrs: {msg.Text}");
							}
						}
						else if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('_')], out index)
							|| !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
						{
							await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /reviews.", new InlineKeyboardButton[]
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
									await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /reviews.", new InlineKeyboardButton[]
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

						await EditOrSendMessage(msg, $"""
						Название: {placeName}
						Всего отзывов: {$"{reviewCounter}"}
						Всего отзывов с комментариями: {$"{reviews.Count}"}
						{(sortType != null ? $"Режим сортировки: {sortType}\n" : "")}
						{(reviews.Count > nowCounter ? $"{reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : $"{(sortType == null ? $"Развёрнутые отзывы на {placeName} не обнаружено" : $"Развёрнутых отзывов по тегу {sortType} не обнаружено")}")}
						{(reviews.Count > ++nowCounter ? $"{reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}
						{(reviews.Count > ++nowCounter ? $"{reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}
						{(reviews.Count > ++nowCounter ? $"{reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}
						{(reviews.Count > ++nowCounter ? $"{reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}
						""", new InlineKeyboardButton[][]
						{
							[(sortType == ReviewSort.Upper ? "" : "Оценка ↑", $"/reviews U{args[1..3]}{index}_{placeSelectorPage}"), (sortType == ReviewSort.Lower ? "" : "Оценка ↓", $"/reviews L{args[1..3]}{index}_{placeSelectorPage}"),
							(sortType == ReviewSort.NewDate ? "" : "Новые", $"/reviews N{args[1..3]}{index}_{placeSelectorPage}"), (sortType == ReviewSort.OldDate ? "" : "Старые", $"/reviews O{args[1..3]}{index}_{placeSelectorPage}")],

							[((page != 0) ? "◀️" : "", $"/reviews {args[..3]}{index}|{page - 1}_{placeSelectorPage}"), ("Назад", $"/info {args[1..3]}{index}_{placeSelectorPage}"), (reviews.Count > ++nowCounter ? "▶️" : "", $"/reviews {args[..3]}{index}|{page + 1}_{placeSelectorPage}")]
						}, ParseMode.Html);
						break;
					}
				case ("/sendReview"):
					{
						if (args == null)
						{
							await EditOrSendMessage(msg, "Ошибка при запросе: /sendReview не применяется без аргументов.", new InlineKeyboardButton[]
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
								await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /sendReview.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
								throw new Exception($"Invalid command agrs: {msg.Text}");
							}
						}
						else if (!char.IsLetter(args[1]) || !int.TryParse(args[2..], out index))
						{

							await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /sendReview.", new InlineKeyboardButton[]
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
									await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /sendReview.", new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						if (place.Reviews.Any(x => x.UserID == foundUser!.UserID) || AdminControl.ReviewCollector.Any(x => x.place == place && x.review.UserID == foundUser!.UserID))
						{
							if (place.Reviews.Any(x => x.UserID == foundUser!.UserID))
								await EditOrSendMessage(msg, $"""
									Вы уже оставили отзыв на {place.Name}

									• Оценка: {place.Reviews.Where(x => x.UserID == foundUser!.UserID).First().Rating}
									• Комментарий: {place.Reviews.Where(x => x.UserID == foundUser!.UserID).First().Comment ?? "Отсутствует"}
									""", new InlineKeyboardButton[][]
									{
										[("Изменить", $"/changeReview -{args}"), ("Удалить", $"#deleteReview {args}")],
										[("Назад", $"/info {args}")]
									}, ParseMode.Html);
							else
								await EditOrSendMessage(msg, $"""
									Вы уже оставили отзыв на {place.Name}

									• Оценка: {AdminControl.ReviewCollector.Where(x => x.place == place && x.review.UserID == foundUser!.UserID).First().review.Rating}
									• Комментарий: {AdminControl.ReviewCollector.Where(x => x.place == place && x.review.UserID == foundUser!.UserID).First().review.Comment}
									""", new InlineKeyboardButton[][]
									{
										[("Изменить", $"/changeReview -{args}"), ("Удалить", $"#deleteReview {args}")],
										[("Назад", $"/info {args}")]
									}, ParseMode.Html);
							break;
						}

						switch (usersState[foundUser!.UserID].Action)
						{
							case (null):
								{
									usersState[foundUser!.UserID].Action = UserAction.RatingRequest;
									usersState[foundUser!.UserID].ReferenceToPlace = args;

									await EditOrSendMessage(msg, $"Введите оценку от 1⭐️ до 10⭐️", null, ParseMode.None, true);
									break;
								}
							default:
								{
									await EditOrSendMessage(msg, $"Зафиксирована попытка оставить отзыв на другую точку. Сброс ранее введённой информации...");
									usersState[foundUser!.UserID].Action = null;
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
							await EditOrSendMessage(msg, "Ошибка при запросе: /changeReview не применяется без аргументов.", new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							throw new Exception($"No command args: {msg.Text}");
						}

						int index = 0, placeSelectorPage = 0;
						if (args.Contains('_'))
						{
							if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('_')], out index) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
							{
								await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /sendReview.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
								throw new Exception($"Invalid command agrs: {msg.Text}");
							}
						}
						else if (!char.IsLetter(args[2]) || !int.TryParse(args[3..], out index))
						{
							await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /sendReview.", new InlineKeyboardButton[]
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
									await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /changeReview.", new InlineKeyboardButton[]
									   {
										   ("Назад", "/places")
									   });
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}

						if (!place.Reviews.Where(x => x.UserID == foundUser!.UserID).Any() && !AdminControl.ReviewCollector.Any(x => x.place == place && x.review.UserID == foundUser!.UserID))
						{
							await EditOrSendMessage(msg, $"""
							Вы не можете изменить отзыв на {place.Name}

							Причина: Ваш отзыв не существует в системе
							""", new InlineKeyboardButton[]
							{
								("Назад", $"/info {args[1..]}")
							}, ParseMode.Html);
							break;
						}

						switch (args[0])
						{
							case ('R'):
								{
									usersState[foundUser!.UserID].Action = UserAction.RatingChange;
									usersState[foundUser!.UserID].ReferenceToPlace = args[1..];

									await EditOrSendMessage(msg, $"Введите НОВУЮ оценку от 1⭐️ до 10⭐️", null, ParseMode.None, true);
									break;
								}
							case ('C'):
								{
									usersState[foundUser!.UserID].Action = UserAction.CommentChange;
									usersState[foundUser!.UserID].ReferenceToPlace = args[1..];

									await EditOrSendMessage(msg, $"Введите НОВЫЙ текст отзыва или удалите его отправив -", null, ParseMode.None, true);
									break;
								}
						}

						switch (usersState[foundUser!.UserID].Action)
						{
							case (null):
								{
									await EditOrSendMessage(msg, $"""
									Что вы хотите изменить в отзыве на {place.Name}?
									""", new InlineKeyboardButton[][]
									{
										[("Оценку", $"/changeReview R{args[1..]}"), ("Комментарий", $"/changeReview C{args[1..]}")],
										[("Назад", $"/info {args[1..]}")]
									}, ParseMode.Html);
									break;
								}
							case (UserAction.NoActiveChange):
								{
									if (AdminControl.ReviewCollector.Any(x => x.place == place && x.review.UserID == foundUser!.UserID))
									{
										if (usersState[foundUser!.UserID].Rating == 0)
											usersState[foundUser!.UserID].Rating = AdminControl.ReviewCollector.First(x => x.place == place && x.review.UserID == foundUser!.UserID).review.Rating;
										if (usersState[foundUser!.UserID].Comment == "saved_mark")
											usersState[foundUser!.UserID].Comment = AdminControl.ReviewCollector.First(x => x.place == place && x.review.UserID == foundUser!.UserID).review.Comment;
									}
									else
									{
										if (usersState[foundUser!.UserID].Rating == 0)
											usersState[foundUser!.UserID].Rating = place.Reviews.First(x => x.UserID == foundUser!.UserID).Rating;
										if (usersState[foundUser!.UserID].Comment == "saved_mark")
											usersState[foundUser!.UserID].Comment = null;    // Если есть сохранённый коммент - его бы нашли в админ контроле
									}

									if (usersState[foundUser!.UserID].Comment == "-")
										usersState[foundUser!.UserID].Comment = null;

									usersState[foundUser!.UserID].Action = null;
									await EditOrSendMessage(msg, $"""
									Ваш НОВЫЙ отзыв:
									
										• Оценка: {usersState[foundUser!.UserID].Rating}
										• Комментарий: {usersState[foundUser!.UserID].Comment ?? "Отсутствует"}
									
									Всё верно?
									""", new InlineKeyboardButton[][]
									{
										[("Да", $"#changeReview {usersState[foundUser!.UserID].ReferenceToPlace}"), ("Нет", $"/changeReview -{usersState[foundUser!.UserID].ReferenceToPlace}")],
										[("Назад", $"/info {args[1..]}")]
									}, ParseMode.Html);
									break;
								}
						}
						break;
					}
				case ("/admin"):
					{
						if (foundUser!.Role != RoleType.Administrator)
						{
							await EditOrSendMessage(msg, "Ошибка при запросе: неизвестная команда.", new InlineKeyboardButton[]
							{
								("Назад", "/places")
							});
							break;
						}

						if (args == null)
						{
							await EditOrSendMessage(msg, $"""
							Доброго времени, адмеместратор {foundUser!.Username}
							
							Кол-во отзывов на проверку: {AdminControl.ReviewCollector.Count}
							""", new InlineKeyboardButton[][]
							{
								[(AdminControl.ReviewCollector.Count > 0 ? "Начать проверку" : "", $"/admin chk")],
								[("Меню блокировок", "/admin ban")],
								[("Обновить админ-меню", "/admin ref"), ("Назад", $"/start")]
							}, ParseMode.Html);
							break;
						}

						if (args.Length < 3)
						{
							await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin.", new InlineKeyboardButton[]
							{
								("Назад", "/admin")
							});
							throw new Exception($"Invalid command agrs: {msg.Text}");
						}

						switch (args[..3])
						{
							case ("ref"):
								{
									// Это НЕ такое же сообщение, как в главном мену, у "Доброго" английские о, чтобы не выдавались ошибки из-за смены на одно и то же
									await EditOrSendMessage(msg, $"""	
									Дoбрoгo времени, адмеместратор {foundUser!.Username}
							
									Кол-во отзывов на проверку: {AdminControl.ReviewCollector.Count}
									""", new InlineKeyboardButton[][]
									{
										[(AdminControl.ReviewCollector.Count > 0 ? "Начать проверку" : "", $"/admin chk")],
										[("Меню блокировок", "/admin ban")],
										[("Обновить админ-меню", "/admin"), ("Назад", $"/start")]
									}, ParseMode.Html);
									break;
								}
							case ("chk"):
								{
									if (args.Length < 4)
									{
										if (AdminControl.ReviewCollector.Count > 0)
										{
											ObjectLists.Persons.TryGetValue(AdminControl.ReviewCollector[0].review.UserID, out Person? writer);
											ArgumentNullException.ThrowIfNull(writer);

											await EditOrSendMessage(msg, $"""
											Отзыв ({AdminControl.ReviewCollector[0].review.Rating}⭐) от @{writer.Username}, {writer.Role} на {AdminControl.ReviewCollector[0].place.Name}.
											Дата отправки на модерацию: {AdminControl.ReviewCollector[0].review.Date}
											
											Оригинал: {AutoMod.BoldCandidateCensor(AdminControl.ReviewCollector[0].review.Comment!)}
											
											Авто-мод: {AutoMod.AddCensor(AdminControl.ReviewCollector[0].review.Comment!)}
											""", new InlineKeyboardButton[][]
												{
												[("Изменить вручную", $"/admin chkA")],
												[("Принять авто-мод", $"#admin chkM"), ("Принять оригинал", $"#admin chkO")],
												[("Назад", $"/admin")]
												}, ParseMode.Html);
										}
										else
											await EditOrSendMessage(msg, $"""	
											Отзывов на проверку не осталось, отличная работа!
											""", new InlineKeyboardButton[][]
												{
												[("Назад", $"/admin")]
												}, ParseMode.Html);
										break;
									}

									switch (args[3])
									{
										case ('A'):
											{
												switch (usersState[foundUser!.UserID].Action)
												{
													case (null):
														{
															usersState[foundUser!.UserID].Action = UserAction.Moderation;
															await EditOrSendMessage(msg, $"Введите ОТРЕДАКТИРОВАННЫЙ текст отзыва или удалите его отправив -", null, ParseMode.None, true);
															break;
														}
													case (UserAction.NoActiveModeration):
														{
															if (usersState[foundUser!.UserID].Comment == "-")
																usersState[foundUser!.UserID].Comment = null;

															usersState[foundUser!.UserID].Action = null;
															await EditOrSendMessage(msg, $"""
															ОТРЕДАКТИРОВАННЫЙ отзыв:
									
																• Оценка: {AdminControl.ReviewCollector[0].review.Rating}
																• Комментарий: {usersState[foundUser!.UserID].Comment ?? "Удалён"}
									
															Всё верно?
															""", new InlineKeyboardButton[][]
															{
																[("Да", $"#admin chkA"), ("Нет", "/admin chkA")],
																[("Назад", "/admin chk")]
															}, ParseMode.Html);
															break;
														}
													default:
														{
															await EditOrSendMessage(msg, $"Зафиксирована попытка приступить к модерации в процессе написания отзыва на другую точку. Сброс ранее введённой информации...");
															usersState[foundUser!.UserID].Action = null;
															await OnCommand("/admin", args, msg);
															break;
														}
												}
												break;
											}
										default:
											{
												await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin chk...", new InlineKeyboardButton[]
												{
													("Назад", "/admin chk")
												});
												throw new Exception($"Invalid command agrs: {msg.Text}");
											}
									}
									break;
								}
							case ("del"):
								{
									if (args.Length < 3)
									{
										await EditOrSendMessage(msg, "Ошибка при запросе: /admin del не применяется без доп. аргументов.", new InlineKeyboardButton[]
										{
											("Назад", "/admin")
										});
										throw new Exception($"No command args: {msg.Text}");
									}

									int index = 0, page = 0, placeSelectorPage = 0;
									if (args.Contains('|'))
									{
										if (!char.IsLetter(args[5]) || !int.TryParse(args[6..args.IndexOf('|')], out index)
											|| !int.TryParse(args[(args.IndexOf('|') + 1)..args.IndexOf('_')], out page) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
										{
											await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin del.", new InlineKeyboardButton[]
											{
												("Назад", "/admin")
											});
											throw new Exception($"Invalid command agrs: {msg.Text}");
										}
									}
									else if (!char.IsLetter(args[5]) || !int.TryParse(args[6..args.IndexOf('_')], out index)
										|| !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
									{
										await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin del.", new InlineKeyboardButton[]
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
									switch (args[5])
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
												await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin del.", new InlineKeyboardButton[]
												{
													("Назад", "/admin")
												});
												throw new Exception($"Invalid command agrs: {msg.Text}");
											}
									}

									int reviewCounter = reviews.Count;
									reviews = [.. reviews.OrderByDescending(x => x.Comment != null)];

									ReviewSort? sortType = null;
									switch (args[3])
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

									await EditOrSendMessage(msg, $"""
									Название: {placeName}
									Всего отзывов: {reviewCounter}
									Всего отзывов с комментариями: {reviews.Where(x => x.Comment != null).Count()}
									{(sortType != null ? $"Режим сортировки: {sortType}\n" : "")}
									{(reviews.Count > nowCounter ? $"№{nowCounter} | От @{(ObjectLists.Persons.TryGetValue(reviews[nowCounter].UserID, out Person? user1) ? user1.Username : "???")} | {reviews[nowCounter].Date} | {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment ?? ""}" : "")}
									{(reviews.Count > ++nowCounter ? $"№{nowCounter} | От @{(ObjectLists.Persons.TryGetValue(reviews[nowCounter].UserID, out Person? user2) ? user2.Username : "???")} | {reviews[nowCounter].Date} | {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment ?? ""}" : "")}
									{(reviews.Count > ++nowCounter ? $"№{nowCounter} | От @{(ObjectLists.Persons.TryGetValue(reviews[nowCounter].UserID, out Person? user3) ? user3.Username : "???")} | {reviews[nowCounter].Date} | {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment ?? ""}" : "")}
									{(reviews.Count > ++nowCounter ? $"№{nowCounter} | От @{(ObjectLists.Persons.TryGetValue(reviews[nowCounter].UserID, out Person? user4) ? user4.Username : "???")} | {reviews[nowCounter].Date} | {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment ?? ""}" : "")}
									{(reviews.Count > ++nowCounter ? $"№{nowCounter} | От @{(ObjectLists.Persons.TryGetValue(reviews[nowCounter].UserID, out Person? user5) ? user5.Username : "???")} | {reviews[nowCounter].Date} | {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment ?? ""}" : "")}

									Выберите номер удаляемого отзыва:
									""", new InlineKeyboardButton[][]
									{
										[(reviews.Count > nowCounter - 4 ? $"{nowCounter - 4}" : "", $"/admin cnf{args[3..6]}{index}_{nowCounter - 4}"), (reviews.Count > nowCounter - 3 ? $"{nowCounter - 3}" : "", $"/admin cnf{args[3..6]}{index}_{nowCounter - 3}")],
										[(reviews.Count > nowCounter - 2 ? $"{nowCounter - 2}" : "", $"/admin cnf{args[3..6]}{index}_{nowCounter - 2}"), (reviews.Count > nowCounter - 1 ? $"{nowCounter - 1}" : "", $"/admin cnf{args[3..6]}{index}_{nowCounter - 1}"), (reviews.Count > nowCounter ? $"{nowCounter}" : "", $"/admin cnf{args[3..6]}{index}_{nowCounter}")],

										[(sortType == ReviewSort.Upper ? "" : "Оценка ↑", $"/admin delU{args[4..6]}{index}_{placeSelectorPage}"), (sortType == ReviewSort.Lower ? "" : "Оценка ↓", $"/admin delL{args[4..6]}{index}_{placeSelectorPage}"),
										(sortType == ReviewSort.NewDate ? "" : "Новые", $"/admin delN{args[4..6]}{index}_{placeSelectorPage}"), (sortType == ReviewSort.OldDate ? "" : "Старые", $"/admin delO{args[4..6]}{index}_{placeSelectorPage}")],

										[((page != 0) ? "◀️" : "", $"/admin {args[..6]}{index}|{page - 1}_{placeSelectorPage}"), ("Назад", $"/info {args[4..6]}{index}_{placeSelectorPage}"), (reviews.Count > ++nowCounter ? "▶️" : "", $"/admin {args[..6]}{index}|{page + 1}_{placeSelectorPage}")]
									}, ParseMode.Html);
									break;
								}
							case ("cnf"):
								{
									if (args.Length < 3)
									{
										await EditOrSendMessage(msg, "Ошибка при запросе: /admin cnf не применяется без доп. аргументов.", new InlineKeyboardButton[]
										{
											("Назад", "/admin")
										});
										throw new Exception($"No command args: {msg.Text}");
									}

									if (!char.IsLetter(args[5]) || !int.TryParse(args[6..args.IndexOf('_')], out int index) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out int reviewIndex))
									{
										await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin cnf.", new InlineKeyboardButton[]
										{
											("Назад", "/places")
										});
										throw new Exception($"Invalid command agrs: {msg.Text}");
									}

									BasePlace basePlace;
									switch (args[5])
									{
										case ('C'):
											{
												basePlace = ObjectLists.Canteens.ElementAt(index);
												break;
											}
										case ('B'):
											{
												basePlace = ObjectLists.Buffets.ElementAt(index);
												break;
											}
										case ('G'):
											{
												basePlace = ObjectLists.Groceries.ElementAt(index);
												break;
											}
										default:
											{
												await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin cnf.", new InlineKeyboardButton[]
												{
													("Назад", "/admin")
												});
												throw new Exception($"Invalid command agrs: {msg.Text}");
											}
									}

									List<Review> reviews = basePlace.Reviews;
									switch (args[3])
									{
										case ('U'):
											{
												reviews = [.. reviews.OrderByDescending(x => x.Rating)];
												break;
											}
										case ('L'):
											{
												reviews = [.. reviews.OrderBy(x => x.Rating)];
												break;
											}
										case ('N'):
											{
												reviews = [.. reviews.OrderByDescending(x => x.Date)];
												break;
											}
										case ('O'):
											{
												reviews = [.. reviews.OrderBy(x => x.Date)];
												break;
											}
									}
									
									int realReviewIndex = basePlace.Reviews.IndexOf(reviews[reviewIndex]);
									await EditOrSendMessage(msg, $"""
									Вы уверены, что хотите удалить отзыв на {basePlace.Name} от @{(ObjectLists.Persons.TryGetValue(reviews[reviewIndex].UserID, out Person? user) ? user.Username : "???")}?
									Дата написания: {reviews[reviewIndex].Date}
									Оценка: {reviews[reviewIndex].Rating}⭐
									Комментарий: {reviews[reviewIndex].Comment ?? "Отсутствует"}
									""", new InlineKeyboardButton[][]
									{
										[("Удалить", $"#admin delR{args[5..args.IndexOf('_')]}_{realReviewIndex}")],
										[("Назад", $"/admin del{args[3..args.IndexOf('_')]}_0")]
									}, ParseMode.Html);
									break;
								}
							case ("ban"):
								{
									if (args.Length < 3)
									{
										await EditOrSendMessage(msg, "Ошибка при запросе: /admin ban не применяется без доп. аргументов.", new InlineKeyboardButton[]
										{
											("Назад", "/admin")
										});
										throw new Exception($"No command args: {msg.Text}");
									}

									if (args.Length == 3)
									{
										await EditOrSendMessage(msg, $"""
										Количесвто <b>активных</b> пользователей: {ObjectLists.Persons.Count - SecurityManager.BlockedUsers.Count}

										Количество заблокированных пользователей: {SecurityManager.BlockedUsers.Count}

										Пользователей с замедлением:
										- Лёгким: {SecurityManager.SuspiciousUsers.Where(x => x.Value.suspiciousClass == SuspiciousClass.Light).Count()}
										- Средним: {SecurityManager.SuspiciousUsers.Where(x => x.Value.suspiciousClass == SuspiciousClass.Medium).Count()}
										- Серьёзным: {SecurityManager.SuspiciousUsers.Where(x => x.Value.suspiciousClass == SuspiciousClass.High).Count()}
										""", new InlineKeyboardButton[][]
										{
											[("Выдать замедление", "/admin banS--_0"), ("Выдать блокировку", "/admin banB--_0")],
											[("Снять замедление", "/admin banSR_0"), ("Снять блокировку", "/admin banBR_0")],
											[("Назад", "/admin")]
										}, ParseMode.Html);
										break;
									}

									if (args.Length < 5)
									{
										await EditOrSendMessage(msg, "Ошибка при запросе: /admin ban не применяется без доп. аргументов.", new InlineKeyboardButton[]
										{
											("Назад", "/admin")
										});
										throw new Exception($"No command args: {msg.Text}");
									}

									if (!int.TryParse(args[(args.IndexOf('_') + 1)..], out int page))
									{
										await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin ban.", new InlineKeyboardButton[]
										{
											("Назад", "/admin ban")
										});
										throw new Exception($"Invalid command agrs: {msg.Text}");
									}

									if (page < 0)
										page = 0;
									int nowCounter = page * 10;

									switch (args[3])
									{
										case 'S' when args[4] == 'R':
											{
												var activePersons = SecurityManager.SuspiciousUsers.Where(x => !SecurityManager.BlockedUsers.ContainsKey(x.Key)).Select(x => new
												{
													userID = x.Key,
													x.Value.suspiciousClass,
													x.Value.time
												}).ToList();
											
												await EditOrSendMessage(msg, $"""
												С кого снять замедление?

												{(activePersons.Count > nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user0) ? user0.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user1) ? user1.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user2) ? user2.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user3) ? user3.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user4) ? user4.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user5) ? user5.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user6) ? user6.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user7) ? user7.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user8) ? user8.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user9) ? user9.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
												""", new InlineKeyboardButton[][]
												{
													[(activePersons.Count > (nowCounter - 9) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 9].userID, out Person? _user9) ? _user9.Username : "")}" : "", activePersons.Count > (nowCounter - 9) ? $"#admin susR{activePersons[nowCounter - 9].userID}" : "-"), (activePersons.Count > (nowCounter - 8) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 8].userID, out Person? _user8) ? _user8.Username : "")}" : "", activePersons.Count > (nowCounter - 8) ? $"#admin susR{activePersons[nowCounter - 8].userID}" : "-")],
													[(activePersons.Count > (nowCounter - 7) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 7].userID, out Person? _user7) ? _user7.Username : "")}" : "", activePersons.Count > (nowCounter - 7) ? $"#admin susR{activePersons[nowCounter - 7].userID}" : "-"), (activePersons.Count > (nowCounter - 6) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 6].userID, out Person? _user6) ? _user6.Username : "")}" : "", activePersons.Count > (nowCounter - 6) ? $"#admin susR{activePersons[nowCounter - 6].userID}" : "-")],
													[(activePersons.Count > (nowCounter - 5) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 5].userID, out Person? _user5) ? _user5.Username : "")}" : "", activePersons.Count > (nowCounter - 5) ? $"#admin susR{activePersons[nowCounter - 5].userID}" : "-"), (activePersons.Count > (nowCounter - 4) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 4].userID, out Person? _user4) ? _user4.Username : "")}" : "", activePersons.Count > (nowCounter - 4) ? $"#admin susR{activePersons[nowCounter - 4].userID}" : "-")],
													[(activePersons.Count > (nowCounter - 3) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 3].userID, out Person? _user3) ? _user3.Username : "")}" : "", activePersons.Count > (nowCounter - 3) ? $"#admin susR{activePersons[nowCounter - 3].userID}" : "-"), (activePersons.Count > (nowCounter - 2) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 2].userID, out Person? _user2) ? _user2.Username : "")}" : "", activePersons.Count > (nowCounter - 2) ? $"#admin susR{activePersons[nowCounter - 2].userID}" : "-")],
													[(activePersons.Count > (nowCounter - 1) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 1].userID, out Person? _user1) ? _user1.Username : "")}" : "", activePersons.Count > (nowCounter - 1) ? $"#admin susR{activePersons[nowCounter - 1].userID}" : "-"), (activePersons.Count > nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? _user0) ? _user0.Username : "")}" : "", activePersons.Count > nowCounter ? $"#admin susR{activePersons[nowCounter].userID}" : "-")],
													[("Назад", "/admin ban")]
												}, ParseMode.Html);
												break;
											}
										case 'B' when args[4] == 'R':
											{
												var activePersons = SecurityManager.BlockedUsers.Select(x => new
												{
													userID = x.Key,
													reason = x.Value
												}).ToList();

												await EditOrSendMessage(msg, $"""
												С кого снять блокировку?

												{(activePersons.Count > nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user0) ? user0.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user1) ? user1.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user2) ? user2.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user3) ? user3.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user4) ? user4.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user5) ? user5.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user6) ? user6.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user7) ? user7.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user8) ? user8.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user9) ? user9.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
												""", new InlineKeyboardButton[][]
												{
													[(activePersons.Count > (nowCounter - 9) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 9].userID, out Person? _user9) ? _user9.Username : "")}" : "", activePersons.Count > (nowCounter - 9) ? $"#admin banR{activePersons[nowCounter - 9].userID}" : "-"), (activePersons.Count > (nowCounter - 8) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 8].userID, out Person? _user8) ? _user8.Username : "")}" : "", activePersons.Count > (nowCounter - 8) ? $"#admin banR{activePersons[nowCounter - 8].userID}" : "-")],
													[(activePersons.Count > (nowCounter - 7) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 7].userID, out Person? _user7) ? _user7.Username : "")}" : "", activePersons.Count > (nowCounter - 7) ? $"#admin banR{activePersons[nowCounter - 7].userID}" : "-"), (activePersons.Count > (nowCounter - 6) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 6].userID, out Person? _user6) ? _user6.Username : "")}" : "", activePersons.Count > (nowCounter - 6) ? $"#admin banR{activePersons[nowCounter - 6].userID}" : "-")],
													[(activePersons.Count > (nowCounter - 5) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 5].userID, out Person? _user5) ? _user5.Username : "")}" : "", activePersons.Count > (nowCounter - 5) ? $"#admin banR{activePersons[nowCounter - 5].userID}" : "-"), (activePersons.Count > (nowCounter - 4) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 4].userID, out Person? _user4) ? _user4.Username : "")}" : "", activePersons.Count > (nowCounter - 4) ? $"#admin banR{activePersons[nowCounter - 4].userID}" : "-")],
													[(activePersons.Count > (nowCounter - 3) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 3].userID, out Person? _user3) ? _user3.Username : "")}" : "", activePersons.Count > (nowCounter - 3) ? $"#admin banR{activePersons[nowCounter - 3].userID}" : "-"), (activePersons.Count > (nowCounter - 2) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 2].userID, out Person? _user2) ? _user2.Username : "")}" : "", activePersons.Count > (nowCounter - 2) ? $"#admin banR{activePersons[nowCounter - 2].userID}" : "-")],
													[(activePersons.Count > (nowCounter - 1) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 1].userID, out Person? _user1) ? _user1.Username : "")}" : "", activePersons.Count > (nowCounter - 1) ? $"#admin banR{activePersons[nowCounter - 1].userID}" : "-"), (activePersons.Count > nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? _user0) ? _user0.Username : "")}" : "", activePersons.Count > nowCounter ? $"#admin banR{activePersons[nowCounter].userID}" : "-")],
													[("Назад", "/admin ban")]
												}, ParseMode.Html);
												break;
											}
										case 'S':
											{
												if (args[5] == '-')
												{
													await EditOrSendMessage(msg, $"""
													Выберите тип замедления:
													- Лёгкое (1 секунда задержки на 3 минуты без "нарушений")
													- Среднее (3 секунды задержки на 30 минуты без "нарушений", а после понижение до лёгкого)
													- Серьёзное (6 секунд задержки на 5 часов без "нарушений", а после понижение до среднего)

													<u>Уточнение:</u>
													Как правило, ручная выдача <i>не требуется</i> из-за авто-выдачи замедлений. В теории добропорядочный юзер, если постарается, сможет получить <b>максимум</b> лёгкое замедление, остальные - лишь методы защиты от спаммеров.
													""", new InlineKeyboardButton[][]
													{
														[("Лёгкое", "/admin banS-L_0"), ("Среднее", "/admin banS-M_0"), ("Серьёзное", "/admin banS-H_0")],
														[("Назад", "/admin ban")]
													}, ParseMode.Html);
													break;
												}

												string? selectedType = args[5] switch
												{
													'L' => "лёгкого",
													'M' => "среднего",
													'H' => "серьёзного",
													_ => null,
												};
												if (selectedType == null)
												{
													await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin banS.", new InlineKeyboardButton[]
													{
														("Назад", "/admin banS--_0")
													});
													throw new Exception($"Invalid command agrs: {msg.Text}");
												}


												List<Person> activePersons = [.. ObjectLists.Persons.Where(x => !SecurityManager.BlockedUsers.ContainsKey(x.Key)).Select(x => x.Value)];
												await EditOrSendMessage(msg, $"""
												Кому выдать замедление {selectedType} типа?

												{(activePersons.Count > nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												""", new InlineKeyboardButton[][]
												{
													[(activePersons.Count > (nowCounter - 9) ? $"@{activePersons[nowCounter - 9].Username}" : "", activePersons.Count > (nowCounter - 9) ? $"#admin susG{args[5]}{activePersons[nowCounter - 9].UserID}" : "-"), (activePersons.Count > (nowCounter - 8) ? $"@{activePersons[nowCounter - 8].Username}" : "", activePersons.Count > (nowCounter - 8) ? $"#admin susG{args[5]}{activePersons[nowCounter - 8].UserID}" : "-")],
													[(activePersons.Count > (nowCounter - 7) ? $"@{activePersons[nowCounter - 7].Username}" : "", activePersons.Count > (nowCounter - 7) ? $"#admin susG{args[5]}{activePersons[nowCounter - 7].UserID}" : "-"), (activePersons.Count > (nowCounter - 6) ? $"@{activePersons[nowCounter - 6].Username}" : "", activePersons.Count > (nowCounter - 6) ? $"#admin susG{args[5]}{activePersons[nowCounter - 6].UserID}" : "-")],
													[(activePersons.Count > (nowCounter - 5) ? $"@{activePersons[nowCounter - 5].Username}" : "", activePersons.Count > (nowCounter - 5) ? $"#admin susG{args[5]}{activePersons[nowCounter - 5].UserID}" : "-"), (activePersons.Count > (nowCounter - 4) ? $"@{activePersons[nowCounter - 4].Username}" : "", activePersons.Count > (nowCounter - 4) ? $"#admin susG{args[5]}{activePersons[nowCounter - 4].UserID}" : "-")],
													[(activePersons.Count > (nowCounter - 3) ? $"@{activePersons[nowCounter - 3].Username}" : "", activePersons.Count > (nowCounter - 3) ? $"#admin susG{args[5]}{activePersons[nowCounter - 3].UserID}" : "-"), (activePersons.Count > (nowCounter - 2) ? $"@{activePersons[nowCounter - 2].Username}" : "", activePersons.Count > (nowCounter - 2) ? $"#admin susG{args[5]}{activePersons[nowCounter - 2].UserID}" : "-")],
													[(activePersons.Count > (nowCounter - 1) ? $"@{activePersons[nowCounter - 1].Username}" : "", activePersons.Count > (nowCounter - 1) ? $"#admin susG{args[5]}{activePersons[nowCounter - 1].UserID}" : "-"), (activePersons.Count > nowCounter ? $"@{activePersons[nowCounter].Username}" : "", activePersons.Count > nowCounter ? $"#admin susG{args[5]}{activePersons[nowCounter].UserID}" : "-")],
													[("Назад", "/admin banS--_0")]
												}, ParseMode.Html);
												break;
											}
										case 'B':
											{
												if (args[5] == '-')
												{
													await EditOrSendMessage(msg, $"""
													Выберите причину блокировки:
													- 1 | Попытка совершить спам атаку
													- 2 | Написание отзыва, содержащего оскорбительный/спорный материал
													- 3 | Траблмейкинг

													<u>Уточнение:</u>
													<i><b>Попытка совершить спам атаку</b></i> - данную причину выдаёт автоматическая система защиты от спам атак, но по необходимости вы можете использовать её в ручном режиме.
													<i><b>Написание отзыва, содержащего оскорбительный/спорный материал</b></i> - данную причину стоит выдавать только за отзывы.
													<i><b>Траблмейкинг</b></i> - "общая" причина блокировки. Следует использовать только в случаях, когда причины выше неактуальны.
													""", new InlineKeyboardButton[][]
													{
														[("1 | Спам атака", "/admin banB-L_0")], 
														[("2 | Оскорбительный отзыв", "/admin banB-M_0")], 
														[("3 | Траблмейкинг", "/admin banB-H_0")],
														[("Назад", "/admin ban")]
													}, ParseMode.Html);
													break;
												}

												string? selectedType = args[5] switch
												{
													'L' => "Попытка совершить спам атаку",
													'M' => "Написание отзыва, содержащего оскорбительный/спорный материал",
													'H' => "Траблмейкинг",
													_ => null,
												};
												if (selectedType == null)
												{
													await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin banB.", new InlineKeyboardButton[]
													{
														("Назад", "/admin banB--_0")
													});
													throw new Exception($"Invalid command agrs: {msg.Text}");
												}


												List<Person> activePersons = [.. ObjectLists.Persons.Where(x => !SecurityManager.BlockedUsers.ContainsKey(x.Key)).Select(x => x.Value)];
												await EditOrSendMessage(msg, $"""
												Кому выдать блокировку по причине: {selectedType}

												{(activePersons.Count > nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												{(activePersons.Count > ++nowCounter ? $"@{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
												""", new InlineKeyboardButton[][]
												{
													[(activePersons.Count > (nowCounter - 9) ? $"@{activePersons[nowCounter - 9].Username}" : "", activePersons.Count > (nowCounter - 9) ? $"#admin banG{args[5]}{activePersons[nowCounter - 9].UserID}" : "-"), (activePersons.Count > (nowCounter - 8) ? $"@{activePersons[nowCounter - 8].Username}" : "", activePersons.Count > (nowCounter - 8) ? $"#admin banG{args[5]}{activePersons[nowCounter - 8].UserID}" : "-")],
													[(activePersons.Count > (nowCounter - 7) ? $"@{activePersons[nowCounter - 7].Username}" : "", activePersons.Count > (nowCounter - 7) ? $"#admin banG{args[5]}{activePersons[nowCounter - 7].UserID}" : "-"), (activePersons.Count > (nowCounter - 6) ? $"@{activePersons[nowCounter - 6].Username}" : "", activePersons.Count > (nowCounter - 6) ? $"#admin banG{args[5]}{activePersons[nowCounter - 6].UserID}" : "-")],
													[(activePersons.Count > (nowCounter - 5) ? $"@{activePersons[nowCounter - 5].Username}" : "", activePersons.Count > (nowCounter - 5) ? $"#admin banG{args[5]}{activePersons[nowCounter - 5].UserID}" : "-"), (activePersons.Count > (nowCounter - 4) ? $"@{activePersons[nowCounter - 4].Username}" : "", activePersons.Count > (nowCounter - 4) ? $"#admin banG{args[5]}{activePersons[nowCounter - 4].UserID}" : "-")],
													[(activePersons.Count > (nowCounter - 3) ? $"@{activePersons[nowCounter - 3].Username}" : "", activePersons.Count > (nowCounter - 3) ? $"#admin banG{args[5]}{activePersons[nowCounter - 3].UserID}" : "-"), (activePersons.Count > (nowCounter - 2) ? $"@{activePersons[nowCounter - 2].Username}" : "", activePersons.Count > (nowCounter - 2) ? $"#admin banG{args[5]}{activePersons[nowCounter - 2].UserID}" : "-")],
													[(activePersons.Count > (nowCounter - 1) ? $"@{activePersons[nowCounter - 1].Username}" : "", activePersons.Count > (nowCounter - 1) ? $"#admin banG{args[5]}{activePersons[nowCounter - 1].UserID}" : "-"), (activePersons.Count > nowCounter ? $"@{activePersons[nowCounter].Username}" : "", activePersons.Count > nowCounter ? $"#admin banG{args[5]}{activePersons[nowCounter].UserID}" : "-")],
													[("Назад", "/admin banB--_0")]
												}, ParseMode.Html);
												break;
											}
										default:
											{
												await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin ban.", new InlineKeyboardButton[]
													{
														("Назад", "/admin")
													});
												throw new Exception($"Invalid command agrs: {msg.Text}");
											}
									}
									break;
								}
							default:
								{
									await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin.", new InlineKeyboardButton[]
									{
										("Назад", "/admin")
									});
									throw new Exception($"Invalid command agrs: {msg.Text}");
								}
						}
						break;
					}
				default:
					{
						await EditOrSendMessage(msg, "Ошибка при запросе: неизвестная команда.", new InlineKeyboardButton[]
							{
								("Назад", "/places")
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
						await OnStandartCallbackQuery(query);
						break;
					}
				default:
					{
						Console.WriteLine($"Received unhandled update {update.Type}");
						break;
					}
			}
		}

        async Task OnStandartCallbackQuery(CallbackQuery callbackQuery)
		{
            ArgumentNullException.ThrowIfNull(callbackQuery.Data);
            ArgumentNullException.ThrowIfNull(callbackQuery.Message);

            ObjectLists.Persons.TryGetValue(callbackQuery.Message.Chat.Id, out Person? foundUser);
            if (foundUser != null)
            {
				if (SecurityManager.BlockedUsers.TryGetValue(foundUser.UserID, out string? reason))
				{
					await bot.SendMessage(callbackQuery.Message.Chat, $"Вы были заблокированы за: {reason ?? "Траблмейкинг"}.");
					return;
				}

				if (SecurityManager.SecurityCheck<CallbackQuery>(foundUser.UserID, callbackQuery))
                    return;
			}

			await OnDirectCallbackQuery(callbackQuery);
        }

        async Task OnDirectCallbackQuery(CallbackQuery callbackQuery)
		{
			ArgumentNullException.ThrowIfNull(callbackQuery.Data);
			ArgumentNullException.ThrowIfNull(callbackQuery.Message);

            ObjectLists.Persons.TryGetValue(callbackQuery.Message.Chat.Id, out Person? foundUser);
			if (foundUser == null && callbackQuery.Data != "/start")
				return;

            switch (callbackQuery.Data[0])
			{
				case ('/'):
					{
						try
						{
                            await bot.AnswerCallbackQuery(callbackQuery.Id);
                        }
						catch (Exception ex)
						{
							Console.WriteLine(ex);
							await bot.SendHtml(callbackQuery.Message.Chat, $"""
							Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

							<code>Код необработанного запроса: {callbackQuery.Data}</code>
							""");
						}
						var splitStr = callbackQuery.Data.Split(' ');
						if (splitStr.Length > 1)
							await OnCommand(splitStr[0], splitStr[1], callbackQuery.Message);
						else
							await OnCommand(splitStr[0], null, callbackQuery.Message);
						break;
					}
				case ('#'):
					{
						ArgumentNullException.ThrowIfNull(callbackQuery.Message);

						if (foundUser == null)
						{
							await EditOrSendMessage(callbackQuery.Message, "Вы не прошли регистрацию путём ввода /start, большая часть функций бота недоступна",
								new InlineKeyboardButton[] { ("Зарегистрироваться", "/start") });
							break;
						}

						var splitStr = callbackQuery.Data.Split(' ');
						if (splitStr.Length < 2)
						{
							await EditOrSendMessage(callbackQuery.Message, $"Ошибка при #{callbackQuery.Data} запросе: некорректный аргументов.", new InlineKeyboardButton[]
							{
											("Назад", "/places")
							});
							throw new Exception($"No command args: {callbackQuery.Message.Text}");
						}

						if (splitStr[0] == "#admin" && foundUser.Role == RoleType.Administrator)
						{
							switch (splitStr[1][..4])
							{
								case ("chkA"):
									{
										if (usersState[foundUser.UserID].Comment != null)
											AdminControl.SetReviewStatus(usersState[foundUser.UserID].Comment!);
										else
										{
											AdminControl.ReviewCollector[0].place.AddReview(AdminControl.ReviewCollector[0].review.UserID, AdminControl.ReviewCollector[0].review.Rating, null);
											AdminControl.SetReviewStatus();
										}

										try
										{
											await bot.AnswerCallbackQuery(callbackQuery.Id, "Отзыв с правками успешно оставлен!");
										}
										catch (Exception ex)
										{
											Console.WriteLine(ex);
											await bot.SendHtml(callbackQuery.Message.Chat, $"""
											Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

											<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
											""");
										}
										
										await OnCommand("/admin", "chk", callbackQuery.Message);
										break;
									}
								case ("chkM"):
									{
										AdminControl.SetReviewStatus(AutoMod.AddCensor(AdminControl.ReviewCollector[0].review.Comment!));

										try
										{
											await bot.AnswerCallbackQuery(callbackQuery.Id, "Отзыв после авто-мода успешно оставлен!");
										}
										catch (Exception ex)
										{
											Console.WriteLine(ex);
											await bot.SendHtml(callbackQuery.Message.Chat, $"""
											Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

											<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
											""");
										}
										
										await OnCommand("/admin", "chk", callbackQuery.Message);
										break;
									}
								case ("chkO"):
									{
										AdminControl.SetReviewStatus(true);

										try
										{
											await bot.AnswerCallbackQuery(callbackQuery.Id, "Оригинальный отзыв успешно оставлен!");
										}
										catch (Exception ex)
										{
											Console.WriteLine(ex);
											await bot.SendHtml(callbackQuery.Message.Chat, $"""
											Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

											<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
											""");
										}
										
										await OnCommand("/admin", "chk", callbackQuery.Message);
										break;
									}
								case ("delR"):
									{
										if (!char.IsLetter(splitStr[1][4]) || !int.TryParse(splitStr[1][5..splitStr[1].IndexOf('_')], out int locationReview) || !int.TryParse(splitStr[1][(splitStr[1].IndexOf('_') + 1)..], out int reviewIndex))
										{
											await EditOrSendMessage(callbackQuery.Message, "Ошибка при запросе: некорректный аргумент команды /admin cnf.", new InlineKeyboardButton[]
											{
											("Назад", "/places")
											});
											throw new Exception($"Invalid command agrs: {callbackQuery.Data}");
										}

										BasePlace placeOfReview;
										switch (splitStr[1][4])
										{
											case ('C'):
												{
													placeOfReview = ObjectLists.Canteens.ElementAt(locationReview);
													break;
												}
											case ('B'):
												{
													placeOfReview = ObjectLists.Buffets.ElementAt(locationReview);
													break;
												}
											case ('G'):
												{
													placeOfReview = ObjectLists.Groceries.ElementAt(locationReview);
													break;
												}
											default:
												{
													await EditOrSendMessage(callbackQuery.Message, $"Ошибка при #{callbackQuery.Data} запросе: некорректный аргументов.", new InlineKeyboardButton[]
													{
														("Назад", "/places")
													});
													throw new Exception($"Invalid command agrs: {callbackQuery.Message.Text}");
												}
										}

										if (placeOfReview.DeleteReview(placeOfReview.Reviews[reviewIndex].UserID))
										{
											try
											{
												await bot.AnswerCallbackQuery(callbackQuery.Id, $"Отзыв пользователя успешно удалён!");
											}
											catch (Exception ex)
											{
												Console.WriteLine(ex);
												await bot.SendHtml(callbackQuery.Message.Chat, $"""
												Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

												<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
												""");
											}

											await OnCommand("/admin", $"delN-{splitStr[1][4..splitStr[1].IndexOf('_')]}_0", callbackQuery.Message);
											break;
										}

										await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке удалить отзыв на {placeOfReview.Name}", new InlineKeyboardButton[]
										{
											("Назад", $"/info {splitStr[1]}")
										});
										throw new Exception($"Error while user {foundUser.UserID} trying to delete review on {placeOfReview.Name}");
									}
								case ("susG"):
									{
										SuspiciousClass selectedClass = splitStr[1][4] switch
										{
											'L' => SuspiciousClass.Light,
											'M' => SuspiciousClass.Medium,
											'H' => SuspiciousClass.High,
											_ => SuspiciousClass.Light,
										};
										if (!long.TryParse(splitStr[1][5..], out long userID))
										{
											await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке наложить замедление", new InlineKeyboardButton[]
											{
												("Назад", $"/admin banS--_0")
											});
											throw new Exception($"Error while user {foundUser.UserID} trying to slow user");
										}

										if (SecurityManager.UpdateSuspiciousUser(userID, selectedClass))
										{
											try
											{
												await bot.AnswerCallbackQuery(callbackQuery.Id, $"Пользователь успешно замедлен!");
											}
											catch (Exception ex)
											{
												Console.WriteLine(ex);
												await bot.SendHtml(callbackQuery.Message.Chat, $"""
												Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

												<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
												""");
											}

											await OnCommand("/admin", $"banS--_0", callbackQuery.Message);
											break;
										}

										await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке наложить замедление на {userID}", new InlineKeyboardButton[]
										{
											("Назад", $"/admin banS--_0")
										});
										throw new Exception($"Error while user {foundUser.UserID} trying to slow user {userID}");
									}
								case ("susR"):
									{
										if (!long.TryParse(splitStr[1][4..], out long userID))
										{
											await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке снять замедление", new InlineKeyboardButton[]
											{
												("Назад", $"/admin banSR_0")
											});
											throw new Exception($"Error while user {foundUser.UserID} trying remove slow from user");
										}

										if (SecurityManager.SuspiciousUsers.TryRemove(userID, out _))
										{
											try
											{
												await bot.AnswerCallbackQuery(callbackQuery.Id, $"Замедление успешно снято!");
											}
											catch (Exception ex)
											{
												Console.WriteLine(ex);
												await bot.SendHtml(callbackQuery.Message.Chat, $"""
												Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

												<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
												""");
											}

											await OnCommand("/admin", $"banSR_0", callbackQuery.Message);
											break;
										}

										await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке снять замедление с {userID}", new InlineKeyboardButton[]
										{
											("Назад", $"/admin banSR_0")
										});
										throw new Exception($"Error while user {foundUser.UserID} trying remove slow from user {userID}");
									}
								case ("banG"):
									{
										string? selectedReason = splitStr[1][4] switch
										{
											'L' => "Попытка совершить спам атаку",
											'M' => "Написание отзыва, содержащего оскорбительный/спорный материал",
											'H' => "Траблмейкинг",
											_ => "Траблмейкинг",
										};
										if (!long.TryParse(splitStr[1][5..], out long userID))
										{
											await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке заблокировать", new InlineKeyboardButton[]
											{
												("Назад", $"/admin banB--_0")
											});
											throw new Exception($"Error while user {foundUser.UserID} trying to slow user");
										}

										if (SecurityManager.BlockedUsers.TryAdd(userID, selectedReason))
										{
											try
											{
												await bot.AnswerCallbackQuery(callbackQuery.Id, $"Пользователь успешно заблокирован!");
											}
											catch (Exception ex)
											{
												Console.WriteLine(ex);
												await bot.SendHtml(callbackQuery.Message.Chat, $"""
												Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

												<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
												""");
											}

											await OnCommand("/admin", $"banB--_0", callbackQuery.Message);
											break;
										}

										await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке наложить замедление на {userID}", new InlineKeyboardButton[]
										{
											("Назад", $"/admin banS--_0")
										});
										throw new Exception($"Error while user {foundUser.UserID} trying to slow user {userID}");
									}
								case ("banR"):
									{
										if (!long.TryParse(splitStr[1][4..], out long userID))
										{
											await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке снять блокировку", new InlineKeyboardButton[]
											{
												("Назад", $"/admin banBR_0")
											});
											throw new Exception($"Error while user {foundUser.UserID} trying remove ban from user");
										}

										if (SecurityManager.BlockedUsers.TryRemove(userID, out _))
										{
											try
											{
												await bot.AnswerCallbackQuery(callbackQuery.Id, $"Блокировка успешно снята!");
											}
											catch (Exception ex)
											{
												Console.WriteLine(ex);
												await bot.SendHtml(callbackQuery.Message.Chat, $"""
												Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

												<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
												""");
											}

											await OnCommand("/admin", $"banBR_0", callbackQuery.Message);
											break;
										}

										await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке снять блокировку с {userID}", new InlineKeyboardButton[]
										{
											("Назад", $"/admin banBR_0")
										});
										throw new Exception($"Error while user {foundUser.UserID} trying remove ban from user {userID}");
									}
								default:
									{
										throw new Exception($"Invalid command agrs: {callbackQuery.Message.Text}");
									}
							}
							break;
						}

						if (!char.IsLetter(splitStr[1][1]) || !int.TryParse(splitStr[1][2..splitStr[1].IndexOf('_')], out int index))
						{
							await EditOrSendMessage(callbackQuery.Message, $"Ошибка при #{callbackQuery.Data} запросе: некорректный аргументов.", new InlineKeyboardButton[]
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
									await EditOrSendMessage(callbackQuery.Message, $"Ошибка при #{callbackQuery.Data} запросе: некорректный аргументов.", new InlineKeyboardButton[]
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
									if (AdminControl.AddReviewOnMod(place, foundUser.UserID, usersState[foundUser.UserID].Rating, usersState[foundUser.UserID].Comment) && usersState[foundUser.UserID].Action == UserAction.NoActiveRequest)
									{
										usersState[foundUser.UserID].Action = null;

										try
										{
											await bot.AnswerCallbackQuery(callbackQuery.Id, (usersState[foundUser.UserID].Comment == null) ? "Отзыв успешно оставлен!" : "Отзыв успешно оставлен! В течение суток он будет опубликован.");
										}
										catch (Exception ex)
										{
											Console.WriteLine(ex);
											await bot.SendHtml(callbackQuery.Message.Chat, $"""
											Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

											<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
											""");
										}
										
										await OnCommand("/info", usersState[foundUser.UserID].ReferenceToPlace, callbackQuery.Message);
									}
									else
									{
										await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке оставить отзыв: {usersState[foundUser.UserID].Rating}⭐️| {usersState[foundUser.UserID].Comment ?? "Комментарий отсутствует"}", new InlineKeyboardButton[]
										{
											("Назад", $"/info {usersState[foundUser.UserID].ReferenceToPlace}")
										});
										throw new Exception($"Ошибка при попытке оставить отзыв: {usersState[foundUser.UserID].ReferenceToPlace} - {usersState[foundUser.UserID].Rating} | {usersState[foundUser.UserID].Comment ?? "Комментарий отсутствует"}");
									}

									break;
								}
							case ("deleteReview"):
								{
									if (!place.Reviews.Where(x => x.UserID == foundUser.UserID).Any() && !AdminControl.ReviewCollector.Any(x => x.place == place && x.review.UserID == foundUser.UserID))
									{
										await EditOrSendMessage(callbackQuery.Message, $"""
										Вы не можете удалить отзыв на {place.Name}

										Причина: Ваш отзыв не существует в системе
										""", new InlineKeyboardButton[]
										{
											("Назад", $"/placeSelector {splitStr[1]}")
										}, ParseMode.Html);
										break;
									}

									if (place.DeleteReview(foundUser.UserID))	
									{
										try
										{
											await bot.AnswerCallbackQuery(callbackQuery.Id, "Отзыв успешно удалён!");
										}
										catch (Exception ex)
										{
											Console.WriteLine(ex);
											await bot.SendHtml(callbackQuery.Message.Chat, $"""
											Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

											<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
											""");
										}
										
										await OnCommand("/info", splitStr[1], callbackQuery.Message);
										break;
									}
									else if (AdminControl.ReviewCollector.Any(x => x.place == place && x.review.UserID == foundUser.UserID))
									{
										AdminControl.SetReviewStatus(false, AdminControl.ReviewCollector.FindIndex(x => x.place == place && x.review.UserID == foundUser.UserID));

										try
										{
											await bot.AnswerCallbackQuery(callbackQuery.Id, "Непроверенный отзыв успешно удалён!");
										}
										catch (Exception ex)
										{
											Console.WriteLine(ex);
											await bot.SendHtml(callbackQuery.Message.Chat, $"""
											Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

											<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
											""");
										}

										await OnCommand("/info", splitStr[1], callbackQuery.Message);
										break;
									}

									await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке удалить отзыв на {place.Name}", new InlineKeyboardButton[]
										{
											("Назад", $"/info {splitStr[1]}")
										});
									throw new Exception($"Error while user {foundUser.UserID} trying to delete review on {place.Name}");
								}
							case ("changeReview"):
								{
									if (usersState[foundUser.UserID].Action != null)
										break;

									place.DeleteReview(foundUser.UserID);
									if (AdminControl.ReviewCollector.Any(x => x.place == place && x.review.UserID == foundUser.UserID))
										AdminControl.SetReviewStatus(false, AdminControl.ReviewCollector.FindIndex(x => x.place == place && x.review.UserID == foundUser.UserID));

									AdminControl.AddReviewOnMod(place, foundUser.UserID, usersState[foundUser.UserID].Rating, usersState[foundUser.UserID].Comment);

									try
									{
										await bot.AnswerCallbackQuery(callbackQuery.Id, "Отзыв успешно изменён!");
									}
									catch (Exception ex)
									{
										Console.WriteLine(ex);
										await bot.SendHtml(callbackQuery.Message.Chat, $"""
										Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

										<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
										""");
									}

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
							try
							{
								await bot.AnswerCallbackQuery(callbackQuery.Id);
							}
							catch (Exception ex)
							{
								Console.WriteLine(ex);
								await bot.SendHtml(callbackQuery.Message.Chat, $"""
								Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

								<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
								""");
							}

							if (foundUser == null)
								await OnCommand("/start", null, callbackQuery.Message!);
							else
							{
								usersState[foundUser.UserID].Action = null;
								await OnCommand("/info", usersState[foundUser.UserID].ReferenceToPlace, callbackQuery.Message!);
							}
						}
						else
							Console.WriteLine($"Зафиксирован необработанный callbackQuery {callbackQuery.Data}");
						break;
					}
			}
		}
	}
}