using Microsoft.Data.Sqlite;
using OBED.Include;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

static class Program
{
	private static string dbConnectionString = "Data Source=OBED_DB.db";
	static async Task Main()
	{
		using var cts = new CancellationTokenSource();
		var token = Environment.GetEnvironmentVariable("TOKEN");
		var bot = new TelegramBotClient(token!, cancellationToken: cts.Token);

		using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
		{
			connection.Open();
			using(SqliteCommand command = new SqliteCommand())
			{
				command.Connection = connection;
				CreateTableTGUsers(command);
				CreateTablePlaces(command);
				Product.CreateTableProducts(command);
				BasePlace.CreateTableReviews(command);
				SecurityManager.CreateBlockedUsersTable(command);
			}
		}
		SecurityManager.LoadBlockedUsersBD();
		Person.LoadPersonsFromBD();
		BasePlace.LoadAllPlaces(2);
		BasePlace.LoadAllPlaces(1);
		BasePlace.LoadAllPlaces(3);

		ConcurrentDictionary<long, UserState> usersState = [];
		foreach(var x in ObjectLists.Persons)
		{
			usersState.TryAdd(x.Key, new());
		}

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

		async Task EditOrSendMessage(Message msg, string text, InlineKeyboardMarkup? markup = null, ParseMode parser = ParseMode.Html, bool isForceReply = false)
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
					await bot.SendMessage(msg.Chat, $"🚫 Вы были заблокированы за: {reason ?? "Траблмейкинг"}.");
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
						if (mType == MessageType.Text && msg.Text![0] == '/')
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
							await EditOrSendMessage(msg, """
								💀 Упс, регистрация не была пройдена
								""",
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
										await EditOrSendMessage(msg, $"🪶 Введи текст отзыва или откажись от сообщения, отправив -", null, ParseMode.None, true);
										break;
									}

									await EditOrSendMessage(msg, $"""
										💀 Упс, ошибка при обработке! 
										❕ Убедись, что твоё сообщение содержит только цифры, а также они должны входить в промежуток от 1 до 10 включительно
										""", null, ParseMode.Html, true);
									break;
								}
							case (UserAction.RatingChange):
								{
									if (int.TryParse(msg.Text, out int rating) && (rating > 0 && rating < 11))
									{
										usersState[foundUser.UserID].Rating = rating;
										usersState[foundUser.UserID].Comment = "saved_mark";
										usersState[foundUser.UserID].Action = UserAction.NoActiveChange;
										await OnCommand("/changeReview", $"-{usersState[foundUser.UserID].ActionArguments}", msg);
										break;
									}
										await EditOrSendMessage(msg, $"""
											💀 Упс, ошибка при обработке! 
											❕ Убедись, что твоё сообщение содержит только цифры, а также они должны входить в промежуток от 1 до 10 включительно
											""", null, ParseMode.Html, true);
									break;
								}
							case (UserAction.CommentRequest):
								{
									if (string.IsNullOrWhiteSpace(msg.Text))
									{
										await EditOrSendMessage(msg, $"""
											💀 Упс, ошибка при обработке! 
											❕ Убедись, что твоё сообщение не содержит файлов, стикеров и т.д. или же откажись от отправки введя -
											""", null, ParseMode.Html, true);
									break;
									}

									if (msg.Text.Length > 720)
									{
										await EditOrSendMessage(msg, $"""
											💀 Упс, ошибка при обработке!
											😭 Комментарий не может быть больше 720 символов. Текущая длина сообщения: {msg.Text.Length}.
											❕ Убедись, что твоё сообщение не содержит файлов, стикеров и т.д. или же откажись от отправки введя -
											""", null, ParseMode.Html, true);
										break;
									}
									usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
									if (usersState[foundUser.UserID].Comment == "-")
										usersState[foundUser.UserID].Comment = null;

									usersState[foundUser.UserID].Action = UserAction.NoActiveRequest;
									await EditOrSendMessage(msg, $"""
										❕Твой отзыв:
									
										💠 Оценка: {usersState[foundUser.UserID].Rating}
										💠	Комментарий: {usersState[foundUser.UserID].Comment ?? "Отсутствует"}
									
										❓ Всё так?
										""", new InlineKeyboardButton[][]
										{
											[("Да", $"#sendReview {usersState[foundUser.UserID].ActionArguments}"), ("Нет", $"callback_resetAction")] 
										});
									break;
								}
							case (UserAction.CommentChange):
								{
									if (string.IsNullOrWhiteSpace(msg.Text))
									{
										await EditOrSendMessage(msg, $"""
											💀 Упс, ошибка при обработке! 
											❕ Убедись, что твоё сообщение не содержит файлов, стикеров и т.д. или же откажись от отправки введя -
											""", null, ParseMode.Html, true);
									break;
									}

									usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
									usersState[foundUser.UserID].Rating = 0;
									usersState[foundUser.UserID].Action = UserAction.NoActiveChange;
									await OnCommand("/changeReview", $"-{usersState[foundUser.UserID].ActionArguments}", msg);
									break;
								}
							case (UserAction.ReportRequest):
								{
                                    if (string.IsNullOrWhiteSpace(msg.Text))
                                    {
                                        await EditOrSendMessage(msg, $"""
											💀 Упс, ошибка при обработке! 
											❕ Убедись, что твое сообщение содержит текст или откажись от сообщения отправив -
											""", null, ParseMode.None, true);
											
                                        break;
                                    }

                                    usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
                                    usersState[foundUser.UserID].Action = UserAction.NoActiveReport;
                                    await OnCommand("/sendReport", $"{usersState[foundUser.UserID].ActionArguments}", msg);
                                    break;
								}
							case (UserAction.ReportChange):
                                {
                                    if (string.IsNullOrWhiteSpace(msg.Text))
                                    {
                                        await EditOrSendMessage(msg, $"""
											💀 Упс, ошибка при обработке!
											❕ Убедись, что твое сообщение содержит текст или откажись от сообщения отправив -
											""", null, ParseMode.None, true);
											
                                        break;
                                    }

                                    usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
                                    usersState[foundUser.UserID].Action = UserAction.NoActiveChange;
                                    await OnCommand("/changeReport", $"{usersState[foundUser.UserID].ActionArguments}", msg);
                                    break;
                                }
                            case (UserAction.ReportResponse):
                                {
                                    if (string.IsNullOrWhiteSpace(msg.Text))
                                    {
                                        await EditOrSendMessage(msg, $"""
											💀 Упс, ошибка при обработке!
											Убедись, что твое сообщение содержит текст или откажись от сообщения отправив -
											""", null, ParseMode.None, true);
											
                                        break;
                                    }

                                    usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
                                    usersState[foundUser.UserID].Action = UserAction.NoActiveReportResponse;

                                    await OnCommand("/admin", $"resA", msg);
                                    break;
                                }
                            case (UserAction.ReportSetTegs):
                                {
                                    if (string.IsNullOrWhiteSpace(msg.Text))
                                    {
                                        await EditOrSendMessage(msg, $"""
											💀 Упс, ошибка при обработке!
											❕ УУбедись, что твое сообщение содержит текст или откажись от сообщения отправив -
											""", null, ParseMode.None, true);
											
                                        break;
                                    }

                                    usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
                                    usersState[foundUser.UserID].Action = UserAction.NoActiveReportSetTegs;

                                    await OnCommand("/admin", $"resT", msg);
                                    break;
                                }
                            case (UserAction.Moderation):
                                {
                                    if (string.IsNullOrWhiteSpace(msg.Text))
                                    {
                                        await EditOrSendMessage(msg, $"""
											💀 Упс, ошибка при обработке! 
											❕ Убедись, что твоё сообщение не содержит файлов, стикеров и т.д. или же откажись от отправки введя -
											""", null, ParseMode.Html, true);
                                        break;
                                    }

									usersState[foundUser.UserID].Comment = HtmlEscape(msg.Text).Trim();
									usersState[foundUser.UserID].Action = UserAction.NoActiveModeration;
									await OnCommand("/admin", "chkA", msg);
									break;
								}
							case (UserAction.PlaceNameRequest):
								{
									await OnCommand("/admin", "add", msg);
									break;
								}
							case (UserAction.CorpusRequest):
								{
									await OnCommand("/admin", "add", msg);
									break;
								}
							case (UserAction.FloorRequest):
								{
									await OnCommand("/admin", "add", msg);
									break;
								}
							case (UserAction.DescriptionRequest):
								{
									await OnCommand("/admin", "add", msg);
									break;
								}
							case (UserAction.TypeRequest):
								{
									await OnCommand("/admin", "add", msg);
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
				await EditOrSendMessage(msg, """
					💀 Упс, регистрация не была пройдена
					""",
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
							if (AddUserToDatabase(msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName), msg.Chat.Id, "CommonUser"))
							{
								Console.WriteLine("Добавлен новый пользователь");
							}
							Console.WriteLine($"REG: {msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName)}");
							ObjectLists.Persons.TryAdd(msg.Chat.Id, new Person(msg.Chat.Username ?? (msg.Chat.FirstName + msg.Chat.LastName), msg.Chat.Id, RoleType.CommonUser));
							usersState.TryAdd(msg.Chat.Id, new());
							ObjectLists.Persons.TryGetValue(msg.Chat.Id, out foundUser);
						}

                        await EditOrSendMessage(msg, """
							👋 Добро пожаловать!

							OBED - твой помощник в поиске столовых, буфетов и магазинов на или около территории университета. Я помогу тебе найти лучшие места для обеда, ознакомиться с меню и оставить отзывы о посещённых точках питания ✌
							
							Выбери действие ниже 👇
							""", new InlineKeyboardButton[][]
							{
								[("Точки питания", "/places")],
								[("Профиль", "/person")],
								[("Помощь", "/help"), ("Обратная связь", "/report")],
								[(foundUser!.Role == RoleType.Administrator ? "Админ панель" : "", "/admin")]
							});
						break;
					}
				case ("/person"):
					{
						await EditOrSendMessage(msg, $"""
							📑 Ваше имя: {foundUser!.Username} ({foundUser!.UserID})
							💼 Ваш статус: {checkUserRole(foundUser!.UserID)}
							""", new InlineKeyboardButton[]
							{
								("Назад","/start")
							});
						break;
					}
				case ("/help"):
					{
						await EditOrSendMessage(msg, """
							💼 Вот что я умею:

							📚 Показывать точки питания на территории университета и рядом с ним
							💠 Для этого выбери "Точки питания" в главном меню!

							🏢 Удобно сортировать точки питания по корпусам и рейтингу
							💠 Уже после выбора типа точек питания ты сможешь дополнительно выбрать и местоположение!

							🛒 Просматривать меню точек питания
							💠 После выбора точки питания ты сможешь ознакомиться с её меню!

							🪶 Оставлять отзывы о точках питания
							💠 После выбора точки питания ты сможешь оставить отзыв о ней, чтобы помочь другим пользователям!
							💠 В случае чего, ты в любой момент можешь удалить или изменить через тоже меню.
							❕ Мы просим воздержаться от оскорбительных и нецензурных выражений в отзывах, чтобы поддерживать дружелюбную атмосферу в нашем сообществе. Проверка отзывов с комментариями в среднем занимает до 24 часов.

							✨ Если вдруг у тебя есть вопросы не только про обеды, но и в целом про жизнь нашего вуза, то можем предложить отличный ресурс от наших партнёров: @FirstUni_bot
							👍 OBED team ремоендует!

							❓ Если у тебя остались вопросы или ты хочешь предложить свои уникальные идеи, не стесняйся обращаться в поддержку через кнопку "Обратная связь" в главном меню.
							""", new InlineKeyboardButton[][]
							{
								[("Назад","/start")]
							});
						break;
					}
				case ("/report"):
					{
                        // TODO: Сообщать нам только о тех ошибках, которые реально мешают юзерам, а не о фантомных стикерах
                        await EditOrSendMessage(msg, """
							💖 Спасибо, что хочешь оставить обратную связь, она помогает сделать OBED ещё лучше!

							🗣️ Мы будем рады выслушать твои предложения, идеи или пожелания по поводу бота!

							🪲 Если что-то ведёт себя не как надо мы тоже будем рады узнать об этом, чтобы исправить проблему как можно скорее.
							""", new InlineKeyboardButton[][]
							{
								[("Сообщить об ошибке","/sendReport B")],
								[("Оставить отзыв о боте","/sendReport R")],
								[("Изменить отчет", "/pickReport")],
								[("Назад","/start")]
							});
						break;
					}
				case ("/sendReport"):
					{
                        if (args == null)
                        {
                            await EditOrSendMessage(msg, """
								💀 Упс, ошибка при запросе: /sendReport не применяется без аргументов.
								""", new InlineKeyboardButton[]
									{
										("Назад", "/report")
									});
                            throw new ArgumentException($"No command args: {msg.Text}");
                        }

                        switch (usersState[foundUser!.UserID].Action)
                        {
                            case (null):
                                {
                                    usersState[foundUser.UserID].Action = UserAction.ReportRequest;
                                    usersState[foundUser.UserID].ActionArguments = args;

									switch(args)
									{
										case ("B"):
											{
                                                await EditOrSendMessage(msg, $"🪶 Введи сообщение об ошибке, указав в чем была проблема, условия возникновения бага, и ожидаемое поведение", null, ParseMode.None, true);
                                                break;
											}
                                        case ("R"):
                                            {
                                                await EditOrSendMessage(msg, $"🪶 Напишит, что ты думаешь об этом боте, или какие у тебя есть предложения по его улучшению", null, ParseMode.None, true);
                                                break;
                                            }
										default:
											{
                                                await EditOrSendMessage(msg, """
													💀 Упс, ошибка при запросе: некорректный аргумент команды /sendReport.
													""", new InlineKeyboardButton[]
													{
														("Назад", "/report")
													});
                                                throw new ArgumentException($"Invalid command agrs: {msg.Text}");
											}
                                    }
                                    break;
                                }
                            case (UserAction.NoActiveReport):
                                {
                                    usersState[foundUser.UserID].Action = null;

                                    string message = "";
                                    switch (usersState[foundUser.UserID].ActionArguments)
                                    {
										case ("B"):
                                            {
                                                message = "🪲 Сообщение об ошибке:";
                                                break;
                                            }
                                        case ("R"):
                                            {
                                                message = "✨ Ваш отзыв на бота:";
                                                break;
                                            }
										default:
											{
                                                await EditOrSendMessage(msg, """
													💀 Упс, ошибка при запросе: некорректный аргумент команды /sendReport.
													""", new InlineKeyboardButton[]
													{
														("Назад", "/report")
													});
												throw new ArgumentException($"Invalid command agrs: {msg.Text}");
											}
                                    }

                                    await EditOrSendMessage(msg, $"""
										{message}

										💠 {usersState[foundUser.UserID].Comment}
									
										❓ Всё верно?
										""", new InlineKeyboardButton[][]
										{
											[("Да", $"#sendReport {usersState[foundUser.UserID].ActionArguments}"), ("Нет", $"/sendReport {usersState[foundUser.UserID].ActionArguments}")],
											[("Назад", $"/report")]
										}, ParseMode.Html);

                                    break;
                                }
                            default:
                                {
                                    await EditOrSendMessage(msg, $"❕ Зафиксирована попытка приступить к редактированию другого репорта или отзыва на точку. Сброс ранее введённой информации...");
                                    usersState[foundUser.UserID].Action = null;
                                    await OnCommand("/sendReport", args, msg);
                                    break;
                                }
                        }

                        break;
                    }
                case ("/pickReport"):
                    {
                        List<FeedbackReport> reports = [.. ObjectLists.FeedbackReports.Where(x => x.UserID == foundUser.UserID)];

                        if (!reports.Any())
                        {
                            await EditOrSendMessage(msg, $"😭 Вы не оставили ни одного репорта", new InlineKeyboardButton[]
								{
									("Назад", $"/report")
								}, ParseMode.Html);
                            break;
                        }

                        int page = 0;

                        if (!string.IsNullOrEmpty(args) && !int.TryParse(args, out page))
						{
                            await EditOrSendMessage(msg, """
								💀 Упс, ошибка при запросе: некорректный аргумент команды /pickReport.
								""", new InlineKeyboardButton[]
								{
									("Назад", "/report")
								});
                            throw new ArgumentException($"Invalid command agrs: {msg.Text}");
                        }

                        if (page < 0)
                            page = 0;
						if (page >= reports.Count)
							page = reports.Count - 1;

                        await EditOrSendMessage(msg, $"""
							📕 Репорт:
							    "{reports[page].Comment}".

							⌚ Дата отправки на рассмотрение: 
							    {reports[page].Date}.
							
							🗣️ Ответ:
							    {reports[page].Answer ?? "Отсутствует"}.
							""", new InlineKeyboardButton[][]
							{
								[((page != 0) ? "◀️" : "", $"/pickReport {page - 1}"), ("Редактировать", $"/changeReport {page}"), ((reports.Count - 1)> page ? "▶️" : "", $"/pickReport {page + 1}")],
								[("Назад", $"/report")]
							}, ParseMode.Html);
                        break;
                    }
                case ("/changeReport"):
                    {
                        if (args == null || !int.TryParse(args, out int reportIndex) || reportIndex < 0)
                        {
                            await EditOrSendMessage(msg, """
								💀 Упс, ошибка при запросе: /changeReport не применяется без аргументов.
								""", new InlineKeyboardButton[]	
								{
									("Назад", "/report")
								});
                            throw new ArgumentException($"No command args: {msg.Text}");
                        }

                        switch (usersState[foundUser!.UserID].Action)
                        {
                            case (null):
                                {
                                    usersState[foundUser.UserID].Action = UserAction.ReportChange;
                                    usersState[foundUser.UserID].ActionArguments = args;
                                    await EditOrSendMessage(msg, $"🪶 Введите НОВЫЙ текст отчета или удалите его отправив -", null, ParseMode.None, true);

                                    break;
                                }
                            case (UserAction.NoActiveChange):
                                {
                                    usersState[foundUser!.UserID].Action = null;

                                    await EditOrSendMessage(msg, $"""
										📗 Ваш НОВЫЙ отчет:
									
										💠 {(usersState[foundUser!.UserID].Comment != "-" ? usersState[foundUser!.UserID].Comment : "Удален")}
									
										❓ Всё верно?
										""", new InlineKeyboardButton[][]
										{
											[("Да", $"#changeReport {usersState[foundUser!.UserID].ActionArguments}"), ("Нет", $"/changeReport {usersState[foundUser!.UserID].ActionArguments}")],
											[("Назад", $"/report")]
										}, ParseMode.Html);
                                    break;
                                }
                            default:
                                {
                                    await EditOrSendMessage(msg, $"❕Зафиксирована попытка приступить к редактированию другого репорта или отзыва на точку. Сброс ранее введённой информации...");
                                    usersState[foundUser.UserID].Action = null;
                                    await OnCommand("/changeReport", args, msg);
                                    break;
                                }
                        }
                        break;
                    }
                case ("/places"):
					{
						await EditOrSendMessage(msg, """
							📍Выбери интересующий тип точки питания:
							""", new InlineKeyboardButton[][]
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
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: /placeSelector не применяется без аргументов.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("No command args", nameof(args));
						}

						int page = 0;
						if (!char.IsLetter(args[1]) || (args.Length > 2 && !int.TryParse(args[2..], out page)))
						{
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /placeSelector.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("Invalid command args", nameof(args));
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
									await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /placeSelector.", new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
									throw new ArgumentException("Invalid command args", nameof(args));
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

						await EditOrSendMessage(msg, """
							🏢 Выбери точку питания из списка ниже:

							📑 По необходимости ты можешь отсортировать точки питания по корпусу университета, нажав на соответствующую кнопку ниже.

							❕ Используй кнопки "◀️" и "▶️" для навигации по страницам.
							""", new InlineKeyboardButton[][]
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
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: /buildingNumberSelector не применяется без аргументов.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
								throw new ArgumentException("No command args", nameof(args));
						}

						await EditOrSendMessage(msg, """
							❓ Выбери корпус университета, чтобы убрать лишние точки питания:
							""", new InlineKeyboardButton[][]
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
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: /info не применяется без аргументов.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("No command args", nameof(args));
						}

						int index = 0, placeSelectorPage = 0;
						if (args.Contains('_'))
						{
							if (!char.IsLetter(args[1]) || !int.TryParse(args[2..args.IndexOf('_')], out index) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
							{
								await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /info.", new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
								throw new ArgumentException("Invalid command args", nameof(args));
							}
						}
						else if (!char.IsLetter(args[1]) || !int.TryParse(args[2..], out index))
						{

							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /info.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("Invalid command args", nameof(args));
						}

						BasePlace place;
						switch (args[1])
						{
							case ('C'):
								{
									place = ObjectLists.Canteens[index];
									break;
								}
							case ('B'):
								{
									place = ObjectLists.Buffets[index];
									break;
								}
							case ('G'):
								{
									place = ObjectLists.Groceries[index];
									break;
								}
							default:
								{
									await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /info.", new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
									throw new ArgumentException("Invalid command args", nameof(args));
								}
						}

						await EditOrSendMessage(msg, $"""
							🍽️ Название точки: {place.Name}
							✨ Средний рейтинг: {(place.Reviews.Count != 0 ? $"{Math.Round((double)place.Reviews.Sum(x => x.Rating) / place.Reviews.Count, 2)}⭐" : "Отзывы не найдены")}
							📗 Всего отзывов: {place.Reviews.Count}
							🪶 Последний отзыв с комментарием: {((place.Reviews.Count != 0 && place.Reviews.Any(x => x.Comment != null)) ? ($"{place.Reviews.Last(x => x.Comment != null).Rating} ⭐️| {place.Reviews.Last(x => x.Comment != null).Comment}") : "Отзывы с комментариями не найдены")}
							""", new InlineKeyboardButton[][]
							{
								[("Меню", $"/menu -{args}")],
								[("Оставить отзыв", $"/sendReview {args}"), ("Отзывы", $"/reviews N{args}")],
								[((foundUser!.Role == RoleType.Administrator && place.Reviews.Count != 0) ? "Панель удаления" : "", $"/admin delN{args}")],
								[("Назад", $"/placeSelector {args[..2]}{placeSelectorPage}")]
							});
						break;
					}
				case ("/menu"):
					{
						if (args == null)
						{
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: /menu не применяется без аргументов.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("No command args", nameof(args));
						}

						int index = 0, page = 0, placeSelectorPage = 0;
						if (args.Contains('|'))
						{
							if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('|')], out index)
								|| !int.TryParse(args[(args.IndexOf('|') + 1)..args.IndexOf('_')], out page) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
							{
								await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /menu.", new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
								throw new ArgumentException("Invalid command args", nameof(args));
							}
						}
						else if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('_')], out index)
							|| !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
						{
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /menu.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("Invalid command args", nameof(args));
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
									placeName = ObjectLists.Canteens[index].Name;
									menu = ObjectLists.Canteens[index].Menu;
									break;
								}
							case ('B'):
								{
									placeName = ObjectLists.Buffets[index].Name;
									menu = ObjectLists.Buffets[index].Menu;
									break;
								}
							case ('G'):
								{
									placeName = ObjectLists.Groceries[index].Name;
									menu = ObjectLists.Groceries[index].Menu;
									break;
								}
							default:
								{
									await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /menu.", new InlineKeyboardButton[]
									   {
										   ("Назад", "/places")
									   });
									throw new ArgumentException("Invalid command args", nameof(args));
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
							🍽️ Название точки: {placeName}
							📕 Всего позиций в меню: {$"{menu.Count}"}

							{(menu.Count > nowCounter ? $"💠 {menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : $"{(productType == null ? $"Меню у {placeName} не обнаружено" : $"Позиций по тегу {productType} не обнаружено")}")}
							{(menu.Count > ++nowCounter ? $"💠 {menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
							{(menu.Count > ++nowCounter ? $"💠 {menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
							{(menu.Count > ++nowCounter ? $"💠 {menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
							{(menu.Count > ++nowCounter ? $"💠 {menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
							{(menu.Count > ++nowCounter ? $"💠 {menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
							{(menu.Count > ++nowCounter ? $"💠 {menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
							{(menu.Count > ++nowCounter ? $"💠 {menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
							{(menu.Count > ++nowCounter ? $"💠 {menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}
							{(menu.Count > ++nowCounter ? $"💠 {menu[nowCounter].Name} | {menu[nowCounter].Price.value} за {(menu[nowCounter].Price.perGram ? "100 грамм" : "порцию")}" : "")}

							❓ Ты можешь выбрать тип сортировки, чтобы удобно просматривать меню!
							❕ Используй кнопки "◀️" и "▶️" для навигации по страницам.
							""", new InlineKeyboardButton[][]
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
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: /reviews не применяется без аргументов.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("No command args", nameof(args));
						}

						int index = 0, page = 0, placeSelectorPage = 0;
						if (args.Contains('|'))
						{
							if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('|')], out index)
								|| !int.TryParse(args[(args.IndexOf('|') + 1)..args.IndexOf('_')], out page) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
							{
								await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /reviews.", new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
								throw new ArgumentException("Invalid command args", nameof(args));
							}
						}
						else if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('_')], out index)
							|| !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
						{
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /reviews.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("Invalid command args", nameof(args));
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
									placeName = ObjectLists.Canteens[index].Name;
									reviews = ObjectLists.Canteens[index].Reviews;
									break;
								}
							case ('B'):
								{
									placeName = ObjectLists.Buffets[index].Name;
									reviews = ObjectLists.Buffets[index].Reviews;
									break;
								}
							case ('G'):
								{
									placeName = ObjectLists.Groceries[index].Name;
									reviews = ObjectLists.Groceries[index].Reviews;
									break;
								}
							default:
								{
									await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /reviews.", new InlineKeyboardButton[]
										{
											("Назад", "/places")
										});
									throw new ArgumentException("Invalid command args", nameof(args));
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
							🍽️ Название точки: {placeName}
							✨ Всего отзывов: {$"{reviewCounter}"}
							📗 Всего отзывов с комментариями: {$"{reviews.Count}"}

							{(reviews.Count > nowCounter ? $"💠 {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : $"{(sortType == null ? $"Развёрнутые отзывы на {placeName} не обнаружено" : $"Развёрнутых отзывов по тегу {sortType} не обнаружено")}")}
							{(reviews.Count > ++nowCounter ? $"💠 {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}
							{(reviews.Count > ++nowCounter ? $"💠 {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}
							{(reviews.Count > ++nowCounter ? $"💠 {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}
							{(reviews.Count > ++nowCounter ? $"💠 {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment}" : "")}

							❓ Ты можешь выбрать тип сортировки, чтобы удобно просматривать список отзывов!
							❕ Используй кнопки "◀️" и "▶️" для навигации по страницам.
							""", new InlineKeyboardButton[][]
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
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: /sendReview не применяется без аргументов.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("No command args", nameof(args));
						}

						int index = 0, placeSelectorPage = 0;
						if (args.Contains('_'))
						{
							if (!char.IsLetter(args[1]) || !int.TryParse(args[2..args.IndexOf('_')], out index) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
							{
								await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /sendReview.", new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
								throw new ArgumentException("Invalid command args", nameof(args));
							}
						}
						else if (!char.IsLetter(args[1]) || !int.TryParse(args[2..], out index))
						{

							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /sendReview.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("Invalid command args", nameof(args));
						}

						BasePlace place;
						switch (args[1])
						{
							case ('C'):
								{
									place = ObjectLists.Canteens[index];
									break;
								}
							case ('B'):
								{
									place = ObjectLists.Buffets[index];
									break;
								}
							case ('G'):
								{
									place = ObjectLists.Groceries[index];
									break;
								}
							default:
								{
									await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /sendReview.", new InlineKeyboardButton[]
										{
											("Назад", "/places")
										});
									throw new ArgumentException("Invalid command args", nameof(args));
								}
						}

						if (place.Reviews.Any(x => x.UserID == foundUser!.UserID) || AdminControl.ReviewCollector.Any(x => x.place == place && x.review.UserID == foundUser!.UserID))
						{
							if (place.Reviews.Any(x => x.UserID == foundUser!.UserID))
								await EditOrSendMessage(msg, $"""
									😅 Ты уже оставил отзыв на {place.Name}

									💠 Оценка: {place.Reviews.First(x => x.UserID == foundUser!.UserID).Rating}
									💠 Комментарий: {place.Reviews.First(x => x.UserID == foundUser!.UserID).Comment ?? "Отсутствует"}

									❕ Ты можешь оставить только один отзыв на точку питания, но можешь изменить прошлый!
									""", new InlineKeyboardButton[][]
									{
										[("Изменить", $"/changeReview -{args}"), ("Удалить", $"#deleteReview {args}")],
										[("Назад", $"/info {args}")]
									});
							else
								await EditOrSendMessage(msg, $"""
									😅 Ты уже оставил отзыв на {place.Name}

									💠 Оценка: {AdminControl.ReviewCollector.First(x => x.place == place && x.review.UserID == foundUser!.UserID).review.Rating}
									💠 Комментарий: {AdminControl.ReviewCollector.First(x => x.place == place && x.review.UserID == foundUser!.UserID).review.Comment}

									❕ Ты можешь оставить только один отзыв на точку питания, но можешь изменить прошлый!
									""", new InlineKeyboardButton[][]
									{
										[("Изменить", $"/changeReview -{args}"), ("Удалить", $"#deleteReview {args}")],
										[("Назад", $"/info {args}")]
									});
							break;
						}

						switch (usersState[foundUser!.UserID].Action)
						{
							case (null):
								{
									usersState[foundUser.UserID].Action = UserAction.RatingRequest;
									usersState[foundUser.UserID].ActionArguments = args;

									await EditOrSendMessage(msg, $"✨ Введи оценку точки от 1⭐️ до 10⭐️", null, ParseMode.None, true);
									break;
								}
							default:
								{
									await EditOrSendMessage(msg, $"💀 Упс, зафиксирована попытка оставить отзыв на другую точку. Сброс ранее введённой информации...");
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
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: /changeReview не применяется без аргументов.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("No command args", nameof(args));
						}

						int index = 0, placeSelectorPage = 0;
						if (args.Contains('_'))
						{
							if (!char.IsLetter(args[2]) || !int.TryParse(args[3..args.IndexOf('_')], out index) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
							{
								await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /sendReview.", new InlineKeyboardButton[]
									{
										("Назад", "/places")
									});
								throw new ArgumentException("Invalid command args", nameof(args));
							}
						}
						else if (!char.IsLetter(args[2]) || !int.TryParse(args[3..], out index))
						{
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /sendReview.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							throw new ArgumentException("Invalid command args", nameof(args));
						}

						BasePlace place;
						switch (args[2])
						{
							case ('C'):
								{
									place = ObjectLists.Canteens[index];
									break;
								}
							case ('B'):
								{
									place = ObjectLists.Buffets[index];
									break;
								}
							case ('G'):
								{
									place = ObjectLists.Groceries[index];
									break;
								}
							default:
								{
									await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /changeReview.", new InlineKeyboardButton[]
									   {
										   ("Назад", "/places")
									   });
									throw new ArgumentException("Invalid command args", nameof(args));
								}
						}

						if (!place.Reviews.Any(x => x.UserID == foundUser!.UserID) && !AdminControl.ReviewCollector.Any(x => x.place == place && x.review.UserID == foundUser!.UserID))
						{
							await EditOrSendMessage(msg, $"""
								💀 Упс, ты не можешь изменить отзыв на {place.Name}

								📖 Причина: Твой отзыв не существует в системе
								""", new InlineKeyboardButton[]
								{
									("Назад", $"/info {args[1..]}")
								});
							break;
						}

						switch (args[0])
						{
							case ('R'):
								{
									usersState[foundUser.UserID].Action = UserAction.RatingChange;
									usersState[foundUser.UserID].ActionArguments = args[1..];

									await EditOrSendMessage(msg, $"✨ Введи НОВУЮ оценку на точку от 1⭐️ до 10⭐️", null, ParseMode.None, true);
									break;
								}
							case ('C'):
								{
									usersState[foundUser.UserID].Action = UserAction.CommentChange;
									usersState[foundUser.UserID].ActionArguments = args[1..];

									await EditOrSendMessage(msg, $"🪶 Введи НОВЫЙ текст отзыва на точку или удали его, отправив -", null, ParseMode.None, true);
									break;
								}
						}

						switch (usersState[foundUser!.UserID].Action)
						{
							case (null):
								{
									await EditOrSendMessage(msg, $"""
										❓ Что именно ты хочешь изменить в отзыве на {place.Name}?
										""", new InlineKeyboardButton[][]
										{
											[("Оценку", $"/changeReview R{args[1..]}"), ("Комментарий", $"/changeReview C{args[1..]}")],
											[("Назад", $"/info {args[1..]}")]
										});
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
										❕Твой НОВЫЙ отзыв:
									
										💠 Оценка: {usersState[foundUser!.UserID].Rating}
										💠 Комментарий: {usersState[foundUser!.UserID].Comment ?? "Отсутствует"}
									
									❓ Всё так?
									""", new InlineKeyboardButton[][]
									{
										[("Да", $"#changeReview {usersState[foundUser!.UserID].ActionArguments}"), ("Нет", $"/changeReview -{usersState[foundUser!.UserID].ActionArguments}")],
										[("Назад", $"/info {args[1..]}")]
									});
									break;
								}
						}
						break;
					}
				case ("/admin"):
					{
						if (checkUserRole(foundUser!.UserID) != RoleType.Administrator)
						{
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: неизвестная команда.", new InlineKeyboardButton[]
								{
									("Назад", "/places")
								});
							break;
						}

						if (args == null)
						{
							await EditOrSendMessage(msg, $"""
							👋 Доброго времени, адмеместратор {foundUser!.Username}
							
							📝 Количество отзывов, ожидающих проверку: {AdminControl.ReviewCollector.Count}
							""", new InlineKeyboardButton[][]
							{
                                [(AdminControl.ReviewCollector.Count > 0 ? "Начать проверку" : "", $"/admin chk")],
                                [(ObjectLists.FeedbackReports.Count() > 0 ? "Начать проверку репортов" : "", $"/admin res")],
                                [("Меню блокировок", "/admin ban")],
                                [("Обновить админ-меню", "/admin ref")],
                                [("Добавить точку питания", "/admin add")],
                                [("Добавить продукт к точке питания","/admin adm")],
                                [("Назад", $"/start")]
                            }, ParseMode.Html);
							break;
						}

						if (args.Length < 3)
						{
							await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /admin.", new InlineKeyboardButton[]
								{
									("Назад", "/admin")
								});
							throw new ArgumentException("Invalid command args", nameof(args));
						}

						switch (args[..3])
						{
							case ("ref"):
								{
									// Это НЕ такое же сообщение, как в главном мену, у "Доброго" английские о, чтобы не выдавались ошибки из-за смены на одно и то же
									await EditOrSendMessage(msg, $"""	
									👋 Доброго времени, адмеместратор {foundUser!.Username}
									
									📝 Количество отзывов, ожидающих проверку: {AdminControl.ReviewCollector.Count}
									""", new InlineKeyboardButton[][]
									{
										[(AdminControl.ReviewCollector.Count > 0 ? "Начать проверку" : "", $"/admin chk")],
										[("Меню блокировок", "/admin ban")],
										[("Обновить админ-меню", "/admin")],
										[("Добавить точку питания","/admin add")],
										[("Добавить продукт к точке питания","/admin adm")],
										[("Назад", $"/start")]
									}, ParseMode.Html);
									break;
								}
							case ("adm"):
								{
									await EditOrSendMessage(msg, "TODO: /admin adm", new InlineKeyboardButton[][]
										{
										  [("Назад","/admin")]
										});
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
												📖 Отзыв ({AdminControl.ReviewCollector[0].review.Rating}⭐) от @{writer.Username}, {writer.Role} на {AdminControl.ReviewCollector[0].place.Name}.
												⌚ Дата отправки на модерацию: {AdminControl.ReviewCollector[0].review.Date}
											
												📗 Оригинал: {AutoMod.BoldCandidateCensor(AdminControl.ReviewCollector[0].review.Comment!)}
											
												📕 Авто-мод: {AutoMod.AddCensor(AdminControl.ReviewCollector[0].review.Comment!)}
												""", new InlineKeyboardButton[][]
												{
													[("Изменить вручную", $"/admin chkA")],
													[("Принять авто-мод", $"#admin chkM"), ("Принять оригинал", $"#admin chkO")],
													[("Назад", $"/admin")]
												});
										}
										else
											await EditOrSendMessage(msg, $"""	
												💖 Отзывов на проверку не осталось, отличная работа!
												""", new InlineKeyboardButton[][]
												{
													[("Назад", $"/admin")]
												});
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
															await EditOrSendMessage(msg, $"🪶 Введи ОТРЕДАКТИРОВАННЫЙ текст отзыва или удали его, отправив -", null, ParseMode.None, true);
															break;
														}
													case (UserAction.NoActiveModeration):
														{
															if (usersState[foundUser!.UserID].Comment == "-")
																usersState[foundUser!.UserID].Comment = null;

															usersState[foundUser!.UserID].Action = null;
															await EditOrSendMessage(msg, $"""
																❕ ОТРЕДАКТИРОВАННЫЙ отзыв:
									
																	💠 Оценка: {AdminControl.ReviewCollector[0].review.Rating}
																	💠 Комментарий: {usersState[foundUser!.UserID].Comment ?? "Удалён"}
									
																❓ Всё так?
																""", new InlineKeyboardButton[][]
																{
																	[("Да", $"#admin chkA"), ("Нет", "/admin chkA")],
																	[("Назад", "/admin chk")]
																});
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
                                                await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /admin chk...", new InlineKeyboardButton[]
                                                    {
                                                        ("Назад", "/admin chk")
                                                    });
                                                throw new ArgumentException("Invalid command args", nameof(args));
                                            }
									}
									break;
								}
							case ("res"):
								{
                                    if (args.Length < 4)
                                    {
                                        if (ObjectLists.FeedbackReports.Count() > 0)
                                        {
                                            ObjectLists.Persons.TryGetValue(ObjectLists.FeedbackReports[0].UserID, out Person? writer);
											ArgumentNullException.ThrowIfNull(writer);

                                            await EditOrSendMessage(msg, $"""
											Пользователь: @{writer.Username}, {writer.Role}.

											Репорт: 
												"{ObjectLists.FeedbackReports[0].Comment}".

											Дата отправки на рассмотрение: {ObjectLists.FeedbackReports[0].Date}.

											Ответ:
												{ObjectLists.FeedbackReports[0].Answer ?? "Отсутствует"}.

											Теги:
												{string.Join(", ", ObjectLists.FeedbackReports[0].Tegs)}.
											""", new InlineKeyboardButton[][]
                                                {
                                                [("Ответить", $"/admin resA")],
                                                [("Добавить теги", $"/admin resT")],
                                                [("Пропустить", $"#admin resS")],
                                                [("Назад", $"/admin")]
                                                }, ParseMode.Html);
                                        }
                                        else
                                            await EditOrSendMessage(msg, $"""	
											Репортов на проверку не осталось, отличная работа!
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
                                                            usersState[foundUser!.UserID].Action = UserAction.ReportResponse;
                                                            await EditOrSendMessage(msg, $"""
																Сообщение пользователя:
																	"{ObjectLists.FeedbackReports[0].Comment}"
																
																Введите ответ на данный репорт
																""", null, ParseMode.None, true);
																
                                                            break;
                                                        }
                                                    case (UserAction.NoActiveReportResponse):
                                                        {
                                                            usersState[foundUser!.UserID].Action = null;
                                                            await EditOrSendMessage(msg, $"""
															Сообщение пользователя:
																"{ObjectLists.FeedbackReports[0].Comment}"

															Ваш ответ:
																{usersState[foundUser!.UserID].Comment ?? "Удалён"}
									
															Всё верно?
															""", new InlineKeyboardButton[][]
                                                            {
                                                                [("Да", $"#admin resA"), ("Нет", "/admin resA")],
                                                                [("Назад", "/admin res")]
                                                            }, ParseMode.Html);
                                                            break;
                                                        }
                                                    default:
                                                        {
                                                            await EditOrSendMessage(msg, $"Зафиксирована попытка приступить к обрабортке другого репорта. Сброс ранее введённой информации...");
                                                            usersState[foundUser!.UserID].Action = null;
                                                            await OnCommand("/admin", args, msg);
                                                            break;
                                                        }
                                                }
                                                break;
                                            }
										case ('T'):
											{
                                                switch (usersState[foundUser!.UserID].Action)
                                                {
                                                    case (null):
                                                        {
                                                            usersState[foundUser!.UserID].Action = UserAction.ReportSetTegs;
                                                            await EditOrSendMessage(msg, $"""
																Сообщение пользователя:
																	"{ObjectLists.FeedbackReports[0].Comment}"
																
																Введите список тегов для этого репорта через пробел с маленькой буквы
																""", null, ParseMode.None, true);

                                                            break;
                                                        }
                                                    case (UserAction.NoActiveReportSetTegs):
                                                        {
                                                            usersState[foundUser!.UserID].Action = null;
                                                            await EditOrSendMessage(msg, $"""
															Сообщение пользователя:
																"{ObjectLists.FeedbackReports[0].Comment}"

															Теги:
																{usersState[foundUser!.UserID].Comment}
									
															Всё верно?
															""", new InlineKeyboardButton[][]
                                                            {
                                                                [("Да", $"#admin resT"), ("Нет", "/admin resT")],
                                                                [("Назад", "/admin res")]
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
                                                await EditOrSendMessage(msg, "Ошибка при запросе: некорректный аргумент команды /admin res...", new InlineKeyboardButton[]
                                                {
                                                    ("Назад", "/admin res")
                                                });
                                                throw new Exception($"Invalid command agrs: {msg.Text}");
                                            }
                                    }
                                    break;
								}
							case ("add"):
								{

									switch (usersState[foundUser!.UserID].Action)
									{
										case (null):
											{
												usersState[foundUser!.UserID].Action = UserAction.PlaceNameRequest;
												usersState[foundUser!.UserID].TempData = new PlaceData();
												await EditOrSendMessage(msg, "Введите название");
												break;
											}
										case (UserAction.PlaceNameRequest):
											{
												if (string.IsNullOrWhiteSpace(msg.Text))
												{
													await EditOrSendMessage(msg, "Ошибка! Название не может быть пустым", new InlineKeyboardButton[]
													{
														("Назад","/admin add")
													});
													usersState[foundUser.UserID].Action = null;
													break;
												}
												usersState[foundUser.UserID].TempData.Name = msg.Text.Trim();
												usersState[foundUser.UserID].Action = UserAction.CorpusRequest;
												await EditOrSendMessage(msg, "Введите корпус");
												break;
											}
										case (UserAction.CorpusRequest):
											{
												if (!int.TryParse(msg.Text?.Trim(), out int corpus))
												{
													await EditOrSendMessage(msg, "Ошибка при парсинге данных! Убедитесь что вы ввели исключительно цифры.", new InlineKeyboardButton[]
													{
														("Назад","/admin add")
													});
													usersState[foundUser.UserID].Action = null;
													break;
												}
												usersState[foundUser.UserID].TempData.Corpus = corpus;
												usersState[foundUser.UserID].Action = UserAction.FloorRequest;
												await EditOrSendMessage(msg, "Введите этаж");
												break;
											}
										case (UserAction.FloorRequest):
											{
												if (!int.TryParse(msg.Text?.Trim(), out int floor))
												{
													await EditOrSendMessage(msg, "Ошибка при парсинге данных! Убедитесь что вы ввели исключительно цифры.", new InlineKeyboardButton[]
													{
														("Назад","/admin add")
													});
													usersState[foundUser.UserID].Action = null;
													break;
												}

												usersState[foundUser.UserID].TempData.Floor = floor;
												usersState[foundUser.UserID].Action = UserAction.DescriptionRequest;
												await EditOrSendMessage(msg, "Введите описание");
												break;
											}
										case (UserAction.DescriptionRequest):
											{
												if (string.IsNullOrWhiteSpace(msg.Text))
												{
													await EditOrSendMessage(msg, "Ошибка! Описание не может быть пустым", new InlineKeyboardButton[]
													{
														("Назад","/admin add")
													});
													usersState[foundUser.UserID].Action = null;
													break;
												}
												usersState[foundUser.UserID].TempData.Description = msg.Text.Trim();
												usersState[foundUser.UserID].Action = UserAction.TypeRequest;
												await EditOrSendMessage(msg, "Введите тип точки питания:\n1 - буфет\n2 - столовая\n3 - продуктовый");
												break;
											}
										case (UserAction.TypeRequest):
											{
												if (!int.TryParse(msg.Text?.Trim(), out int type) || type < 1 || type > 3)
												{
													await EditOrSendMessage(msg, "Ошибка! Введите число от 1 до 3, где 1 - Буфет, 2 - Столовая, 3 - Продуктовый", new InlineKeyboardButton[]
													{
														("Назад","/admin add")
													});
													usersState[foundUser.UserID].Action = null;
													break;
												}

												var placeData = usersState[foundUser.UserID].TempData;
												Console.WriteLine($"{placeData.Name},{placeData.Corpus},{placeData.Floor},{placeData.Description},{type}");
												var lastid = AddNewPlace(placeData.Name, placeData.Corpus, placeData.Floor, placeData.Description, type);
												if (lastid.HasValue)
												{
													switch (type)
													{
														case 1:
															{
																ObjectLists.AddRangeList<Buffet>([new(lastid.Value, placeData.Name, placeData.Corpus, placeData.Floor, placeData.Description)]);
																break;
															}
														case 2:
															{
																ObjectLists.AddRangeList<Canteen>([new(lastid.Value, placeData.Name, placeData.Corpus, placeData.Floor, placeData.Description)]);
																break;
															}
														case 3:
															{
																ObjectLists.AddRangeList<Grocery>([new(lastid.Value, placeData.Name, placeData.Description)]);
																break;
															}
													}
													Console.WriteLine("Таблица создана");
												}
												usersState[foundUser!.UserID].Action = null;
												await EditOrSendMessage(msg, "Ну вроде сохранил", new InlineKeyboardButton[][]
												{
													[("Назад", "/admin")]
												}, ParseMode.Html);
												break;
											}
									}
									break;
								}
							case ("del"):
								{
									if (args.Length < 3)
									{
										await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: /admin del не применяется без доп. аргументов.", new InlineKeyboardButton[]
											{
												("Назад", "/admin")
											});
										throw new ArgumentException("No command args", nameof(args));
									}

									int index = 0, page = 0, placeSelectorPage = 0;
									if (args.Contains('|'))
									{
										if (!char.IsLetter(args[5]) || !int.TryParse(args[6..args.IndexOf('|')], out index)
											|| !int.TryParse(args[(args.IndexOf('|') + 1)..args.IndexOf('_')], out page) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
										{
											await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /admin del.", new InlineKeyboardButton[]
												{
													("Назад", "/admin")
												});
											throw new ArgumentException("Invalid command args", nameof(args));
										}
									}
									else if (!char.IsLetter(args[5]) || !int.TryParse(args[6..args.IndexOf('_')], out index)
										|| !int.TryParse(args[(args.IndexOf('_') + 1)..], out placeSelectorPage))
									{
										await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /admin del.", new InlineKeyboardButton[]
											{
												("Назад", "/places")
											});
										throw new ArgumentException("Invalid command args", nameof(args));
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
												placeName = ObjectLists.Canteens[index].Name;
												reviews = ObjectLists.Canteens[index].Reviews;
												break;
											}
										case ('B'):
											{
												placeName = ObjectLists.Buffets[index].Name;
												reviews = ObjectLists.Buffets[index].Reviews;
												break;
											}
										case ('G'):
											{
												placeName = ObjectLists.Groceries[index].Name;
												reviews = ObjectLists.Groceries[index].Reviews;
												break;
											}
										default:
											{
												await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /admin del.", new InlineKeyboardButton[]
													{
														("Назад", "/admin")
													});
												throw new ArgumentException("Invalid command args", nameof(args));
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
										🍽️ Название: {placeName}
										✨ Всего отзывов: {reviewCounter}
										📗 Всего отзывов с комментариями: {reviews.Count(x => x.Comment != null)}

										{(reviews.Count > nowCounter ? $"💠 №{nowCounter} | От @{(ObjectLists.Persons.TryGetValue(reviews[nowCounter].UserID, out Person? user1) ? user1.Username : "???")} | {reviews[nowCounter].Date} | {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment ?? ""}" : "")}
										{(reviews.Count > ++nowCounter ? $"💠 №{nowCounter} | От @{(ObjectLists.Persons.TryGetValue(reviews[nowCounter].UserID, out Person? user2) ? user2.Username : "???")} | {reviews[nowCounter].Date} | {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment ?? ""}" : "")}
										{(reviews.Count > ++nowCounter ? $"💠 №{nowCounter} | От @{(ObjectLists.Persons.TryGetValue(reviews[nowCounter].UserID, out Person? user3) ? user3.Username : "???")} | {reviews[nowCounter].Date} | {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment ?? ""}" : "")}
										{(reviews.Count > ++nowCounter ? $"💠 №{nowCounter} | От @{(ObjectLists.Persons.TryGetValue(reviews[nowCounter].UserID, out Person? user4) ? user4.Username : "???")} | {reviews[nowCounter].Date} | {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment ?? ""}" : "")}
										{(reviews.Count > ++nowCounter ? $"💠 №{nowCounter} | От @{(ObjectLists.Persons.TryGetValue(reviews[nowCounter].UserID, out Person? user5) ? user5.Username : "???")} | {reviews[nowCounter].Date} | {reviews[nowCounter].Rating}⭐ | {reviews[nowCounter].Comment ?? ""}" : "")}

										📝 Выбери номер удаляемого отзыва:
										""", new InlineKeyboardButton[][]
										{
											[(reviews.Count > nowCounter - 4 ? $"{nowCounter - 4}" : "", $"/admin cnf{args[3..6]}{index}_{nowCounter - 4}"), (reviews.Count > nowCounter - 3 ? $"{nowCounter - 3}" : "", $"/admin cnf{args[3..6]}{index}_{nowCounter - 3}")],
											[(reviews.Count > nowCounter - 2 ? $"{nowCounter - 2}" : "", $"/admin cnf{args[3..6]}{index}_{nowCounter - 2}"), (reviews.Count > nowCounter - 1 ? $"{nowCounter - 1}" : "", $"/admin cnf{args[3..6]}{index}_{nowCounter - 1}"), (reviews.Count > nowCounter ? $"{nowCounter}" : "", $"/admin cnf{args[3..6]}{index}_{nowCounter}")],

											[(sortType == ReviewSort.Upper ? "" : "Оценка ↑", $"/admin delU{args[4..6]}{index}_{placeSelectorPage}"), (sortType == ReviewSort.Lower ? "" : "Оценка ↓", $"/admin delL{args[4..6]}{index}_{placeSelectorPage}"),
											(sortType == ReviewSort.NewDate ? "" : "Новые", $"/admin delN{args[4..6]}{index}_{placeSelectorPage}"), (sortType == ReviewSort.OldDate ? "" : "Старые", $"/admin delO{args[4..6]}{index}_{placeSelectorPage}")],

											[((page != 0) ? "◀️" : "", $"/admin {args[..6]}{index}|{page - 1}_{placeSelectorPage}"), ("Назад", $"/info {args[4..6]}{index}_{placeSelectorPage}"), (reviews.Count > ++nowCounter ? "▶️" : "", $"/admin {args[..6]}{index}|{page + 1}_{placeSelectorPage}")]
										});
									break;
								}
							case ("cnf"):
								{
									if (args.Length < 3)
									{
										await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: /admin cnf не применяется без доп. аргументов.", new InlineKeyboardButton[]
											{
												("Назад", "/admin")
											});
										throw new ArgumentException("No command args", nameof(args));
									}

									if (!char.IsLetter(args[5]) || !int.TryParse(args[6..args.IndexOf('_')], out int index) || !int.TryParse(args[(args.IndexOf('_') + 1)..], out int reviewIndex))
									{
										await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /admin cnf.", new InlineKeyboardButton[]
											{
												("Назад", "/places")
											});
										throw new ArgumentException("Invalid command args", nameof(args));
									}

									BasePlace basePlace;
									switch (args[5])
									{
										case ('C'):
											{
												basePlace = ObjectLists.Canteens[index];
												break;
											}
										case ('B'):
											{
												basePlace = ObjectLists.Buffets[index];
												break;
											}
										case ('G'):
											{
												basePlace = ObjectLists.Groceries[index];
												break;
											}
										default:
											{
												await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /admin cnf.", new InlineKeyboardButton[]
													{
														("Назад", "/admin")
													});
												throw new ArgumentException("Invalid command args", nameof(args));
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
										❓ Вы уверены, что хотите удалить отзыв на {basePlace.Name} от @{(ObjectLists.Persons.TryGetValue(reviews[reviewIndex].UserID, out Person? user) ? user.Username : "???")}?
										⌚ Дата написания: {reviews[reviewIndex].Date}
										✨ Оценка: {reviews[reviewIndex].Rating}⭐
										🪶 Комментарий: {reviews[reviewIndex].Comment ?? "Отсутствует"}
										""", new InlineKeyboardButton[][]
										{
											[("Удалить", $"#admin delR{args[5..args.IndexOf('_')]}_{realReviewIndex}")],
											[("Назад", $"/admin del{args[3..args.IndexOf('_')]}_0")]
										});
									break;
								}
							case ("ban"):
								{
									if (args.Length < 3)
									{
										await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: /admin ban не применяется без доп. аргументов.", new InlineKeyboardButton[]
											{
												("Назад", "/admin")
											});
										throw new ArgumentException("No command args", nameof(args));
									}

									if (args.Length == 3)
									{
										await EditOrSendMessage(msg, $"""
											📗 Количесвто <b>активных</b> пользователей: {ObjectLists.Persons.Count - SecurityManager.BlockedUsers.Count}

											📕 Количество заблокированных пользователей: {SecurityManager.BlockedUsers.Count}

											⌚ Пользователей с замедлением:
											💠 Лёгким: {SecurityManager.SuspiciousUsers.Count(x => x.Value.suspiciousClass == SuspiciousClass.Light)}
											💠 Средним: {SecurityManager.SuspiciousUsers.Count(x => x.Value.suspiciousClass == SuspiciousClass.Medium)}
											💠 Серьёзным: {SecurityManager.SuspiciousUsers.Count(x => x.Value.suspiciousClass == SuspiciousClass.High)}
											""", new InlineKeyboardButton[][]
											{
												[("Выдать замедление", "/admin banS--_0"), ("Выдать блокировку", "/admin banB--_0")],
												[("Снять замедление", "/admin banSR_0"), ("Снять блокировку", "/admin banBR_0")],
												[("Назад", "/admin")]
											});
										break;
									}

									if (args.Length < 5)
									{
										await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: /admin ban не применяется без доп. аргументов.", new InlineKeyboardButton[]
											{
												("Назад", "/admin")
											});
										throw new ArgumentException("No command args", nameof(args));
									}

									if (!int.TryParse(args[(args.IndexOf('_') + 1)..], out int page))
									{
										await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /admin ban.", new InlineKeyboardButton[]
											{
												("Назад", "/admin ban")
											});
										throw new ArgumentException("Invalid command args", nameof(args));
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
													❓ С кого снять замедление?

													{(activePersons.Count > nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user0) ? user0.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user1) ? user1.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user2) ? user2.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user3) ? user3.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user4) ? user4.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user5) ? user5.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user6) ? user6.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user7) ? user7.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user8) ? user8.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user9) ? user9.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].suspiciousClass}" : "")}
													""", new InlineKeyboardButton[][]
													{
														[(activePersons.Count > (nowCounter - 9) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 9].userID, out Person? _user9) ? _user9.Username : "")}" : "", activePersons.Count > (nowCounter - 9) ? $"#admin susR{activePersons[nowCounter - 9].userID}" : "-"), (activePersons.Count > (nowCounter - 8) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 8].userID, out Person? _user8) ? _user8.Username : "")}" : "", activePersons.Count > (nowCounter - 8) ? $"#admin susR{activePersons[nowCounter - 8].userID}" : "-")],
														[(activePersons.Count > (nowCounter - 7) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 7].userID, out Person? _user7) ? _user7.Username : "")}" : "", activePersons.Count > (nowCounter - 7) ? $"#admin susR{activePersons[nowCounter - 7].userID}" : "-"), (activePersons.Count > (nowCounter - 6) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 6].userID, out Person? _user6) ? _user6.Username : "")}" : "", activePersons.Count > (nowCounter - 6) ? $"#admin susR{activePersons[nowCounter - 6].userID}" : "-")],
														[(activePersons.Count > (nowCounter - 5) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 5].userID, out Person? _user5) ? _user5.Username : "")}" : "", activePersons.Count > (nowCounter - 5) ? $"#admin susR{activePersons[nowCounter - 5].userID}" : "-"), (activePersons.Count > (nowCounter - 4) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 4].userID, out Person? _user4) ? _user4.Username : "")}" : "", activePersons.Count > (nowCounter - 4) ? $"#admin susR{activePersons[nowCounter - 4].userID}" : "-")],
														[(activePersons.Count > (nowCounter - 3) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 3].userID, out Person? _user3) ? _user3.Username : "")}" : "", activePersons.Count > (nowCounter - 3) ? $"#admin susR{activePersons[nowCounter - 3].userID}" : "-"), (activePersons.Count > (nowCounter - 2) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 2].userID, out Person? _user2) ? _user2.Username : "")}" : "", activePersons.Count > (nowCounter - 2) ? $"#admin susR{activePersons[nowCounter - 2].userID}" : "-")],
														[(activePersons.Count > (nowCounter - 1) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 1].userID, out Person? _user1) ? _user1.Username : "")}" : "", activePersons.Count > (nowCounter - 1) ? $"#admin susR{activePersons[nowCounter - 1].userID}" : "-"), (activePersons.Count > nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? _user0) ? _user0.Username : "")}" : "", activePersons.Count > nowCounter ? $"#admin susR{activePersons[nowCounter].userID}" : "-")],
														[("Назад", "/admin ban")]
													});
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
													❓ С кого снять блокировку?

													{(activePersons.Count > nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user0) ? user0.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user1) ? user1.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user2) ? user2.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user3) ? user3.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user4) ? user4.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user5) ? user5.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user6) ? user6.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user7) ? user7.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user8) ? user8.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? user9) ? user9.Username : "???")} ({activePersons[nowCounter].userID}) | {activePersons[nowCounter].reason}" : "")}
													""", new InlineKeyboardButton[][]
													{
														[(activePersons.Count > (nowCounter - 9) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 9].userID, out Person? _user9) ? _user9.Username : "")}" : "", activePersons.Count > (nowCounter - 9) ? $"#admin banR{activePersons[nowCounter - 9].userID}" : "-"), (activePersons.Count > (nowCounter - 8) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 8].userID, out Person? _user8) ? _user8.Username : "")}" : "", activePersons.Count > (nowCounter - 8) ? $"#admin banR{activePersons[nowCounter - 8].userID}" : "-")],
														[(activePersons.Count > (nowCounter - 7) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 7].userID, out Person? _user7) ? _user7.Username : "")}" : "", activePersons.Count > (nowCounter - 7) ? $"#admin banR{activePersons[nowCounter - 7].userID}" : "-"), (activePersons.Count > (nowCounter - 6) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 6].userID, out Person? _user6) ? _user6.Username : "")}" : "", activePersons.Count > (nowCounter - 6) ? $"#admin banR{activePersons[nowCounter - 6].userID}" : "-")],
														[(activePersons.Count > (nowCounter - 5) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 5].userID, out Person? _user5) ? _user5.Username : "")}" : "", activePersons.Count > (nowCounter - 5) ? $"#admin banR{activePersons[nowCounter - 5].userID}" : "-"), (activePersons.Count > (nowCounter - 4) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 4].userID, out Person? _user4) ? _user4.Username : "")}" : "", activePersons.Count > (nowCounter - 4) ? $"#admin banR{activePersons[nowCounter - 4].userID}" : "-")],
														[(activePersons.Count > (nowCounter - 3) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 3].userID, out Person? _user3) ? _user3.Username : "")}" : "", activePersons.Count > (nowCounter - 3) ? $"#admin banR{activePersons[nowCounter - 3].userID}" : "-"), (activePersons.Count > (nowCounter - 2) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 2].userID, out Person? _user2) ? _user2.Username : "")}" : "", activePersons.Count > (nowCounter - 2) ? $"#admin banR{activePersons[nowCounter - 2].userID}" : "-")],
														[(activePersons.Count > (nowCounter - 1) ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter - 1].userID, out Person? _user1) ? _user1.Username : "")}" : "", activePersons.Count > (nowCounter - 1) ? $"#admin banR{activePersons[nowCounter - 1].userID}" : "-"), (activePersons.Count > nowCounter ? $"@{(ObjectLists.Persons.TryGetValue(activePersons[nowCounter].userID, out Person? _user0) ? _user0.Username : "")}" : "", activePersons.Count > nowCounter ? $"#admin banR{activePersons[nowCounter].userID}" : "-")],
														[("Назад", "/admin ban")]
													});
												break;
											}
										case 'S':
											{
												if (args[5] == '-')
												{
													await EditOrSendMessage(msg, $"""
														⌚ Выберите тип замедления:
														💠 Лёгкое (1 секунда задержки на 3 минуты без "нарушений")
														💠 Среднее (3 секунды задержки на 30 минуты без "нарушений", а после понижение до лёгкого)
														💠 Серьёзное (6 секунд задержки на 5 часов без "нарушений", а после понижение до среднего)

														❕<u>Уточнение:</u>
														💠 Как правило, ручная выдача <i>не требуется</i> из-за авто-выдачи замедлений. В теории добропорядочный юзер, если постарается, сможет получить <b>максимум</b> лёгкое замедление, остальные - лишь методы защиты от спаммеров.
														""", new InlineKeyboardButton[][]
														{
															[("Лёгкое", "/admin banS-L_0"), ("Среднее", "/admin banS-M_0"), ("Серьёзное", "/admin banS-H_0")],
															[("Назад", "/admin ban")]
														});
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
													await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /admin banS.", new InlineKeyboardButton[]
														{
															("Назад", "/admin banS--_0")
														});
													throw new ArgumentException("Invalid command args", nameof(args));
												}


												List<Person> activePersons = [.. ObjectLists.Persons.Where(x => !SecurityManager.BlockedUsers.ContainsKey(x.Key)).Select(x => x.Value)];
												await EditOrSendMessage(msg, $"""
													❓ Кому выдать замедление {selectedType} типа?

													{(activePersons.Count > nowCounter ? $"💠 @{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
													{(activePersons.Count > ++nowCounter ? $"💠 @{activePersons[nowCounter].Username} ({activePersons[nowCounter].UserID}) | {activePersons[nowCounter].Role}" : "")}
													""", new InlineKeyboardButton[][]
													{
														[(activePersons.Count > (nowCounter - 9) ? $"@{activePersons[nowCounter - 9].Username}" : "", activePersons.Count > (nowCounter - 9) ? $"#admin susG{args[5]}{activePersons[nowCounter - 9].UserID}" : "-"), (activePersons.Count > (nowCounter - 8) ? $"@{activePersons[nowCounter - 8].Username}" : "", activePersons.Count > (nowCounter - 8) ? $"#admin susG{args[5]}{activePersons[nowCounter - 8].UserID}" : "-")],
														[(activePersons.Count > (nowCounter - 7) ? $"@{activePersons[nowCounter - 7].Username}" : "", activePersons.Count > (nowCounter - 7) ? $"#admin susG{args[5]}{activePersons[nowCounter - 7].UserID}" : "-"), (activePersons.Count > (nowCounter - 6) ? $"@{activePersons[nowCounter - 6].Username}" : "", activePersons.Count > (nowCounter - 6) ? $"#admin susG{args[5]}{activePersons[nowCounter - 6].UserID}" : "-")],
														[(activePersons.Count > (nowCounter - 5) ? $"@{activePersons[nowCounter - 5].Username}" : "", activePersons.Count > (nowCounter - 5) ? $"#admin susG{args[5]}{activePersons[nowCounter - 5].UserID}" : "-"), (activePersons.Count > (nowCounter - 4) ? $"@{activePersons[nowCounter - 4].Username}" : "", activePersons.Count > (nowCounter - 4) ? $"#admin susG{args[5]}{activePersons[nowCounter - 4].UserID}" : "-")],
														[(activePersons.Count > (nowCounter - 3) ? $"@{activePersons[nowCounter - 3].Username}" : "", activePersons.Count > (nowCounter - 3) ? $"#admin susG{args[5]}{activePersons[nowCounter - 3].UserID}" : "-"), (activePersons.Count > (nowCounter - 2) ? $"@{activePersons[nowCounter - 2].Username}" : "", activePersons.Count > (nowCounter - 2) ? $"#admin susG{args[5]}{activePersons[nowCounter - 2].UserID}" : "-")],
														[(activePersons.Count > (nowCounter - 1) ? $"@{activePersons[nowCounter - 1].Username}" : "", activePersons.Count > (nowCounter - 1) ? $"#admin susG{args[5]}{activePersons[nowCounter - 1].UserID}" : "-"), (activePersons.Count > nowCounter ? $"@{activePersons[nowCounter].Username}" : "", activePersons.Count > nowCounter ? $"#admin susG{args[5]}{activePersons[nowCounter].UserID}" : "-")],
														[("Назад", "/admin banS--_0")]
													});
												break;
											}
										case 'B':
											{
												if (args[5] == '-')
												{
													await EditOrSendMessage(msg, $"""
														❓ Выберите причину блокировки:
														💠 1 | Попытка совершить спам атаку
														💠 2 | Написание отзыва, содержащего оскорбительный/спорный материал
														💠 3 | Траблмейкинг

														❕<u>Уточнение:</u>
														💠 <i><b>Попытка совершить спам атаку</b></i> - данную причину выдаёт автоматическая система защиты от спам атак, но по необходимости вы можете использовать её в ручном режиме.
														💠 <i><b>Написание отзыва, содержащего оскорбительный/спорный материал</b></i> - данную причину стоит выдавать только за отзывы.
														💠 <i><b>Траблмейкинг</b></i> - "общая" причина блокировки. Следует использовать только в случаях, когда причины выше неактуальны.
														""", new InlineKeyboardButton[][]
														{
															[("1 | Спам атака", "/admin banB-L_0")], 
															[("2 | Оскорбительный отзыв", "/admin banB-M_0")], 
															[("3 | Траблмейкинг", "/admin banB-H_0")],
															[("Назад", "/admin ban")]
														});
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
													throw new ArgumentException("Invalid command args", nameof(args));
												}


												List<Person> activePersons = [.. ObjectLists.Persons.Where(x => !SecurityManager.BlockedUsers.ContainsKey(x.Key)).Select(x => x.Value)];
												await EditOrSendMessage(msg, $"""
													❓ Кому выдать блокировку по причине: {selectedType}

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
													});
												break;
											}
										default:
											{
												await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /admin ban.", new InlineKeyboardButton[]
													{
														("Назад", "/admin")
													});
												throw new ArgumentException("Invalid command args", nameof(args));
											}
									}
									break;
								}
							default:
								{
									await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: некорректный аргумент команды /admin.", new InlineKeyboardButton[]
										{
											("Назад", "/admin")
										});
									throw new ArgumentException("Invalid command args", nameof(args));
								}
						}
						break;
					}
				default:
					{
						await EditOrSendMessage(msg, "💀 Упс, ошибка при запросе: неизвестная команда.", new InlineKeyboardButton[]
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
					await bot.SendMessage(callbackQuery.Message.Chat, $"🚫 Вы были заблокированы за: {reason ?? "Траблмейкинг"}.");
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
								😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

								<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
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
							await EditOrSendMessage(callbackQuery.Message, """
								💀 Упс, регистрация не была пройдена
								""",
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
							throw new ArgumentException($"No command args: {callbackQuery.Message.Text}");
						}

                        if (splitStr[0] == "#sendReport")
						{
                            if (usersState[foundUser.UserID].Action != null)
							{
								usersState[foundUser.UserID].Action = null;
                                await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке отправить репорт", new InlineKeyboardButton[]
                                {
                                        ("Назад", $"/report")
                                });
                                throw new ArgumentException($"Error while user {foundUser.UserID} trying to send report");
                            }

							if (usersState[foundUser.UserID].Comment == null)
							{
								await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке отправить пустой репорт", new InlineKeyboardButton[]
								{
										("Назад", $"/report")
								});
								throw new ArgumentException($"Error while user {foundUser.UserID} trying to send empty report");

							}

							switch (splitStr[1])
							{
								case ("B"):
									{
										ObjectLists.FeedbackReports.Add(new FeedbackReport(foundUser.UserID, usersState[foundUser.UserID].Comment ?? "", [ReportTeg.Bug]));
										await bot.AnswerCallbackQuery(callbackQuery.Id, "Отчет о баге успешно добавлен!");
										break;
									}
								case ("R"):
									{
										ObjectLists.FeedbackReports.Add(new FeedbackReport(foundUser.UserID, usersState[foundUser.UserID].Comment ?? "", [ReportTeg.Suggestion])); // TODO
										await bot.AnswerCallbackQuery(callbackQuery.Id, "Отзыв о боте успешно добавлен!");
										break;
									}
								default:
									{
										await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке отправить репорт", new InlineKeyboardButton[]
										{
												("Назад", $"/report")
										});
										throw new ArgumentException($"Error while user {foundUser.UserID} trying to send report");
									}
							}

							await OnCommand("/report", null, callbackQuery.Message);
							break;
						}
                        if (splitStr[0] == "#changeReport")
                        {
                            if (usersState[foundUser.UserID].Action != null)
                                break;

                            if (!int.TryParse(splitStr[1], out int reportIndex) || reportIndex < 0)
                            {
                                await EditOrSendMessage(callbackQuery.Message, "Ошибка при запросе: некорректный аргумент команды #changeReport.", new InlineKeyboardButton[]
                                {
                                    ("Назад", "/report")
                                });
                                throw new Exception($"Invalid command agrs: {callbackQuery.Message.Text}");
                            }

                            var existingReport = ObjectLists.FeedbackReports.Where(x => x.UserID == foundUser.UserID).ElementAtOrDefault(reportIndex);

                            if (existingReport == null)
                            {
                                await EditOrSendMessage(callbackQuery.Message, "Ошибка: репорт не найден или был удалён.", new InlineKeyboardButton[]
                                {
                                        ("Назад", "/report")
                                });
                                throw new Exception($"Report not found for user {foundUser.UserID} at index {reportIndex}");
                            }
                            else
                            {
                                if (usersState[foundUser.UserID].Comment != null)
                                {
                                    if (usersState[foundUser.UserID].Comment == "-")
                                        ObjectLists.FeedbackReports.Remove(existingReport);
                                    else
                                        existingReport.ChangeComment(usersState[foundUser.UserID].Comment!);
                                }
                                else
                                {
                                    await EditOrSendMessage(callbackQuery.Message, $"Ошибка при попытке отправить пустой репорт", new InlineKeyboardButton[]
                                    {
                                            ("Назад", $"/report")
                                    });
                                    throw new Exception($"Error while user {foundUser.UserID} trying to send empty report");
                                }
                            }

                            try
                            {
                                await bot.AnswerCallbackQuery(callbackQuery.Id, "Репорт успешно изменён!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                                await bot.SendHtml(callbackQuery.Message.Chat, $"""
									Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

									<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
									""");
                            }

                            await OnCommand("/report", null, callbackQuery.Message);
                            break;
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
											AdminControl.ReviewCollector[0].place.AddReview(AdminControl.ReviewCollector[0].place.Place_id, AdminControl.ReviewCollector[0].review.UserID, AdminControl.ReviewCollector[0].review.Rating, null, 0);
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
												😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

												<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
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
												😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

												<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
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
												😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

												<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
												""");
										}

										await OnCommand("/admin", "chk", callbackQuery.Message);
										break;
									}
								case ("resA"):
									{
										if (usersState[foundUser.UserID].Comment != null)
										{
											ObjectLists.FeedbackReports[0].Answer = usersState[foundUser.UserID].Comment!;
                                        }
										else
										{
                                            await EditOrSendMessage(callbackQuery.Message, "Ошибка при запросе: некорректный аргумент команды /admin resA.", new InlineKeyboardButton[]
                                            {
                                            ("Назад", "/admin res")
                                            });
                                            throw new Exception($"Invalid command agrs: {callbackQuery.Data}");
                                        }

										try
										{
											await bot.AnswerCallbackQuery(callbackQuery.Id, "Ответ на репорт успешно сохранён!");
										}
										catch (Exception ex)
										{
											Console.WriteLine(ex);
											await bot.SendHtml(callbackQuery.Message.Chat, $"""
										Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

										<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
										""");
										}

                                        await OnCommand("/admin", "res", callbackQuery.Message);

                                        break;
									}
                                case ("resT"):
                                    {
                                        if (usersState[foundUser.UserID].Comment != null)
                                        {
                                            var splitTegs = usersState[foundUser.UserID].Comment!.Split(' ');

											ObjectLists.FeedbackReports[0].Tegs.Clear();

                                            foreach (var teg in splitTegs)
											{
												switch (teg)
												{
													case ("bug"):
														{
															ObjectLists.FeedbackReports[0].Tegs.Add(ReportTeg.Bug);
															break;
														}
													case ("outdatedinfo"):
														{
                                                            ObjectLists.FeedbackReports[0].Tegs.Add(ReportTeg.OutdatedInfo);
                                                            break;
														}
													case ("wronginfo"):
														{
                                                            ObjectLists.FeedbackReports[0].Tegs.Add(ReportTeg.WrongInfo);
                                                            break;
														}
													case ("suggestion"):
														{
                                                            ObjectLists.FeedbackReports[0].Tegs.Add(ReportTeg.Suggestion);
                                                            break;
														}
													default:
														{
                                                            await EditOrSendMessage(callbackQuery.Message, $"Неизвестный тег: {teg}. Допустимые теги: bug, outdatedinfo, wronginfo, suggestion", new InlineKeyboardButton[]
															{
                                                                ("Назад", "/admin res")
                                                            });
                                                            throw new Exception($"Invalid report teg: {teg}");
														}
                                                }
											}
                                        }
                                        else
                                        {
                                            await EditOrSendMessage(callbackQuery.Message, "Ошибка при запросе: некорректный аргумент команды /admin resT.", new InlineKeyboardButton[]
                                            {
                                            ("Назад", "/admin res")
                                            });
                                            throw new Exception($"Invalid command agrs: {callbackQuery.Data}");
                                        }

                                        try
                                        {
                                            await bot.AnswerCallbackQuery(callbackQuery.Id, "Теги репорта успешно обновлены!");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex);
                                            await bot.SendHtml(callbackQuery.Message.Chat, $"""
										Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

										<tg-spoiler><code>Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
										""");
                                        }

                                        await OnCommand("/admin", "res", callbackQuery.Message);

                                        break;
                                    }
                                case ("resS"):
                                    {
										if (ObjectLists.FeedbackReports.Count > 0)
										{
											FeedbackReport report = ObjectLists.FeedbackReports[0];
											ObjectLists.FeedbackReports.Add(report);
											ObjectLists.FeedbackReports.RemoveAt(0);
										}

                                        await OnCommand("/admin", "res", callbackQuery.Message);
                                        break;
                                    }
                                case ("delR"):
									{
										if (!char.IsLetter(splitStr[1][4]) || !int.TryParse(splitStr[1][5..splitStr[1].IndexOf('_')], out int locationReview) || !int.TryParse(splitStr[1][(splitStr[1].IndexOf('_') + 1)..], out int reviewIndex))
										{
											await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: некорректный аргумент команды /admin cnf.", new InlineKeyboardButton[]
												{
													("Назад", "/places")
												});
											throw new ArgumentException($"Invalid command args: {splitStr[1]}");
										}

										BasePlace placeOfReview;
										switch (splitStr[1][4])
										{
											case ('C'):
												{
													placeOfReview = ObjectLists.Canteens[locationReview];
													break;
												}
											case ('B'):
												{
													placeOfReview = ObjectLists.Buffets[locationReview];
													break;
												}
											case ('G'):
												{
													placeOfReview = ObjectLists.Groceries[locationReview];
													break;
												}
											default:
												{
													await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: некорректный аргумент #{callbackQuery.Data}.", new InlineKeyboardButton[]
														{
															("Назад", "/places")
														});
													throw new ArgumentException($"Invalid command args: {splitStr[1]}");
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
													😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

													<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
													""");
											}

											await OnCommand("/admin", $"delN-{splitStr[1][4..splitStr[1].IndexOf('_')]}_0", callbackQuery.Message);
											break;
										}
										await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: не удалось удалить отзыв на {placeOfReview.Name}.", new InlineKeyboardButton[]
											{
												("Назад", $"/info {splitStr[1]}")
											});
										throw new ArgumentException($"Error while user {foundUser.UserID} trying to delete review on {placeOfReview.Name}");
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
											await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: не удалось наложить замедление.", new InlineKeyboardButton[]
												{
													("Назад", $"/admin banS--_0")
												});
											throw new ArgumentException($"Error while user {foundUser.UserID} trying to slow user");
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
													😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

													<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
													""");
											}

											await OnCommand("/admin", $"banS--_0", callbackQuery.Message);
											break;
										}

										await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: не удалось наложить замедление на {userID}.", new InlineKeyboardButton[]
											{
												("Назад", $"/admin banS--_0")
											});
										throw new ArgumentException($"Error while user {foundUser.UserID} trying to slow user {userID}");
									}
								case ("susR"):
									{
										if (!long.TryParse(splitStr[1][4..], out long userID))
										{
											await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: не удалось снять замедление.", new InlineKeyboardButton[]
												{
													("Назад", $"/admin banSR_0")
												});
											throw new ArgumentException($"Error while user {foundUser.UserID} trying remove slow from user");
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
													😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

													<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
													""");
											}

											await OnCommand("/admin", $"banSR_0", callbackQuery.Message);
											break;
										}

										await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: не удалось снять замедление с {userID}.", new InlineKeyboardButton[]
											{
												("Назад", $"/admin banSR_0")
											});
										throw new ArgumentException($"Error while user {foundUser.UserID} trying remove slow from user {userID}");
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
											await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: не удалось заблокировать пользователя.", new InlineKeyboardButton[]
												{
													("Назад", $"/admin banB--_0")
												});
											throw new ArgumentException($"Error while user {foundUser.UserID} trying to slow user");
										}

										if (SecurityManager.BlockedUsers.TryAdd(userID, selectedReason))
										{
											try
											{
												SecurityManager.UpdateOnBanBD(userID, 1,selectedReason);
												await bot.AnswerCallbackQuery(callbackQuery.Id, $"Пользователь успешно заблокирован!");
											}
											catch (Exception ex)
											{
												Console.WriteLine(ex);
												await bot.SendHtml(callbackQuery.Message.Chat, $"""
													😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

													<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
													""");
											}

											await OnCommand("/admin", $"banB--_0", callbackQuery.Message);
											break;
										}

										await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: не удалось наложить замедление на {userID}", new InlineKeyboardButton[]
										{
											("Назад", $"/admin banS--_0")
										});
										throw new ArgumentException($"Error while user {foundUser.UserID} trying to slow user {userID}");
									}
								case ("banR"):
									{
										if (!long.TryParse(splitStr[1][4..], out long userID))
										{
											await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: не удалось снять блокировку", new InlineKeyboardButton[]
											{
												("Назад", $"/admin banBR_0")
											});
											throw new ArgumentException($"Error while user {foundUser.UserID} trying remove ban from user");
										}

										if (SecurityManager.BlockedUsers.TryRemove(userID, out _))
										{
											try
											{
												SecurityManager.UpdateOnBanBD(userID, 0);
												await bot.AnswerCallbackQuery(callbackQuery.Id, $"Блокировка успешно снята!");
											}
											catch (Exception ex)
											{
												Console.WriteLine(ex);
												await bot.SendHtml(callbackQuery.Message.Chat, $"""
													😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

													<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
													""");
											}

											await OnCommand("/admin", $"banBR_0", callbackQuery.Message);
											break;
										}

										await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: не удалось снять блокировку с {userID}.", new InlineKeyboardButton[]
												{
													("Назад", $"/admin banBR_0")
												});
										throw new ArgumentException($"Error while user {foundUser.UserID} trying remove ban from user {userID}");
									}
								default:
									{
										throw new ArgumentException($"Invalid command args: {splitStr[1]}");
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
							throw new ArgumentException($"Invalid command agrs: {callbackQuery.Message.Text}");
						}

						BasePlace place;
						switch (splitStr[1][1])
						{
							case ('C'):
								{
									place = ObjectLists.Canteens[index];
									break;
								}
							case ('B'):
								{
									place = ObjectLists.Buffets[index];
									break;
								}
							case ('G'):
								{
									place = ObjectLists.Groceries[index];
									break;
								}
							default:
								{
									await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: некорректный аргумент #{callbackQuery.Data}.", new InlineKeyboardButton[]
										{
											("Назад", "/places")
										});
									throw new ArgumentException($"Invalid command args: {splitStr[1]}");
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
												😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

												<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
												""");
										}
										
										await OnCommand("/info", usersState[foundUser.UserID].ActionArguments, callbackQuery.Message);
									}
									else
									{
										await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при запросе: не удалось оставить отзыв: {usersState[foundUser.UserID].Rating}⭐️| {usersState[foundUser.UserID].Comment ?? "Комментарий отсутствует"}", new InlineKeyboardButton[]
										{
											("Назад", $"/info {usersState[foundUser.UserID].ActionArguments}")
										});
										throw new ArgumentException($"Ошибка при попытке оставить отзыв: {usersState[foundUser.UserID].ActionArguments} - {usersState[foundUser.UserID].Rating} | {usersState[foundUser.UserID].Comment ?? "Комментарий отсутствует"}");
									}

									break;
								}
							case ("deleteReview"):
								{
									if (!place.Reviews.Any(x => x.UserID == foundUser.UserID) && !AdminControl.ReviewCollector.Any(x => x.place == place && x.review.UserID == foundUser.UserID))
									{
										await EditOrSendMessage(callbackQuery.Message, $"""
										💀 Упс, ошибка! Не удалось уджалить отзыв на {place.Name}

										📑 Причина: отзыв не существует в системе
										""", new InlineKeyboardButton[]
										{
											("Назад", $"/placeSelector {splitStr[1]}")
										});
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
												😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

												<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
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
												😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

												<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
												""");
										}

										await OnCommand("/info", splitStr[1], callbackQuery.Message);
										break;
									}

									await EditOrSendMessage(callbackQuery.Message, $"💀 Упс, ошибка при попытке удалить отзыв на {place.Name}", new InlineKeyboardButton[]
										{
											("Назад", $"/info {splitStr[1]}")
										});
									throw new ArgumentException($"Error while user {foundUser.UserID} trying to delete review on {place.Name}");
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
											😭 Превышено время ожидания ответа на запрос. Пожалуйста, повтори попытку чуть позже.

											<code>🔨 Код необработанного запроса: {callbackQuery.Data}</code>
											""");
									}

									await OnCommand("/info", usersState[foundUser.UserID].ActionArguments, callbackQuery.Message);
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
								😭 Превышено время ожидания ответа на запрос. Пожалуйста, повторите попытку чуть позже.

								<tg-spoiler><code>🔨 Код необработанного запроса: {callbackQuery.Data}</code></tg-spoiler>
								""");
							}

							if (foundUser == null)
								await OnCommand("/start", null, callbackQuery.Message!);
							else
							{
								usersState[foundUser.UserID].Action = null;
								await OnCommand("/info", usersState[foundUser.UserID].ActionArguments, callbackQuery.Message!);
							}
						}
						else
							Console.WriteLine($"Зафиксирован необработанный callbackQuery {callbackQuery.Data}");
						break;
					}
			}
		}
	}
	private static void GetUsers()
	{
		using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
		{
			var users = new List<string>();
			connection.Open();
			SqliteCommand command = new SqliteCommand();
			command.Connection = connection;
			command.CommandText = $"SELECT * FROM TG_Users";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				users.Add(reader.GetString(0));
			}
			foreach (var user in users)
			{
				Console.WriteLine($"{user}");
			}
		}
	}
	private static bool AddUserToDatabase(string username, long TG_id, string role)
	{
		using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
		{
			connection.Open();

			// проверка на наличие таблицы
			using (SqliteCommand command = new SqliteCommand())
			{
				command.Connection = connection;
				CreateTableTGUsers(command);

				if (ifUserExists(TG_id))
				{
					return false;
				}
				else
				{
					//добавление юзера
					command.CommandText = @"INSERT INTO TG_Users(Name,TG_id,Role) VALUES (@username,@TG_id,@role)";
					command.Parameters.Add(new SqliteParameter("@username", username));
					command.Parameters.Add(new SqliteParameter("@TG_id", TG_id));
					command.Parameters.Add(new SqliteParameter("@role", role));
					command.ExecuteNonQuery();
					return true;
				}
			}
		}
	}

	private static void CreateTableTGUsers(SqliteCommand command)
	{

		command.CommandText =
					@"CREATE TABLE IF NOT EXISTS TG_Users (
										List_id	INTEGER,
									    Name	TEXT DEFAULT 'Unknown',
										TG_id	INTEGER NOT NULL UNIQUE,
										Role	TEXT NOT NULL DEFAULT 'CommonUser',
										OnBan INTEGER DEFAULT 0,
										PRIMARY KEY(""List_id"" AUTOINCREMENT)
										);";
		command.ExecuteNonQuery();
	}

	private static bool ifUserExists(long TG_id)
	{
		using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
		{
			connection.Open();
			var command = new SqliteCommand();
			command.Connection = connection;
			command.CommandText = $"SELECT 1 FROM TG_Users WHERE TG_id = @tgid";
			command.Parameters.Add(new SqliteParameter("@tgid", TG_id));
			return command.ExecuteScalar() != null;
		}
	}

	private static RoleType checkUserRole(long UserID)
	{
		using (var connection = new SqliteConnection(dbConnectionString))
		{
			connection.Open();
			var command = new SqliteCommand();
			command.Connection = connection;
			command.CommandText = $@"SELECT Role FROM TG_Users WHERE TG_id = @UserID";
			command.Parameters.Add(new SqliteParameter("@UserID", UserID));
			using (SqliteDataReader reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					string role = reader.GetString(0);
					switch (role)
					{
						case ("CommonUser"):
							{
								return RoleType.CommonUser;
							}
						case ("VipUser"):
							{
								return RoleType.VipUser;
							}
						case ("Administrator"):
							{
								return RoleType.Administrator;
							}
					}
				}
			}
		}
		return RoleType.CommonUser;
	}

	private static long? AddNewPlace(string name, int corpus, int floor, string description, int type)
	{
		using (var connection = new SqliteConnection(dbConnectionString))
		{
			connection.Open();
			var command = new SqliteCommand();
			command.Connection = connection;
			command.CommandText =
				@"CREATE TABLE IF NOT EXISTS ""Places"" (
                	""Place_id""	INTEGER,
                	""Name""	TEXT NOT NULL DEFAULT 'UnknownPlace',
                	""Type""	INTEGER,
                	""Corpus""	INTEGER,
                	""Description""	TEXT NOT NULL DEFAULT 'Description',
                	""Floor""	INTEGER,
                	PRIMARY KEY(""Place_id"" AUTOINCREMENT)
                );";
			command.ExecuteNonQuery();
			if (ifPlaceExists(corpus, floor, name,connection))
			{
				return null;
			}
			command.CommandText =
				@"INSERT INTO Places(Name,Type,Corpus,Description,Floor) VALUES (@name,@type,@corpus,@description,@floor)";
			command.Parameters.Add(new SqliteParameter("@name", name));
			command.Parameters.Add(new SqliteParameter("@corpus", corpus));
			command.Parameters.Add(new SqliteParameter("@floor", floor));
			command.Parameters.Add(new SqliteParameter("@description", description));
			command.Parameters.Add(new SqliteParameter("@type", type));
			int number = command.ExecuteNonQuery();
			Console.WriteLine($"Кол-во добавленных элементов: {number}");
			command.CommandText = "SELECT last_insert_rowid()";
			long placeid = (long)command.ExecuteScalar();
			return placeid;
		}
	}
	private static void CreateTablePlaces(SqliteCommand command)
	{
		command.CommandText =
				@"CREATE TABLE IF NOT EXISTS ""Places"" (
                	""Place_id""	INTEGER,
                	""Name""	TEXT NOT NULL DEFAULT 'UnknownPlace',
                	""Type""	INTEGER,
                	""Corpus""	INTEGER,
                	""Description""	TEXT NOT NULL DEFAULT 'Description',
                	""Floor""	INTEGER,
                	PRIMARY KEY(""Place_id"" AUTOINCREMENT)
                );";
		command.ExecuteNonQuery();
	}

	private static bool ifPlaceExists(int corpus, int floor, string name,SqliteConnection connection)
	{
		using (var command = new SqliteCommand())
		{
			command.Connection = connection;
			command.CommandText = $@"SELECT 1 FROM Places WHERE ""Corpus"" = @corpus AND ""Floor"" = @floor AND ""Name"" = @name";
			command.Parameters.Add(new SqliteParameter("@corpus", corpus));
			command.Parameters.Add(new SqliteParameter("@floor", floor));
			command.Parameters.Add(new SqliteParameter("@name", name));
			return command.ExecuteScalar() != null;
		}
	}
}