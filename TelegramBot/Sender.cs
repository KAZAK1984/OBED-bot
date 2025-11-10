using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;

namespace OBED.TelegramBot
{
	class SendingInfo(Message msg, string text, InlineKeyboardMarkup? markup = null, ParseMode parser = ParseMode.None, bool isForceReply = false)
	{
		public Message Msg { get; init; } = msg;
		public string Text { get; init; } = text;
		public InlineKeyboardMarkup? Markup { get; init; } = markup;
		public ParseMode Parser { get; init; } = parser;
		public bool IsForceReply { get; init; } = isForceReply;
	}
	static class Sender
	{
		public static async Task Send(SendingInfo info)
		{
			ArgumentNullException.ThrowIfNull(info.Msg.From);
			ArgumentNullException.ThrowIfNull(TelegramBot.Bot);
			ArgumentNullException.ThrowIfNull(TelegramBot.Cts);

			if (info.IsForceReply)
			{
				await TelegramBot.Bot.SendMessage(info.Msg.Chat, info.Text, info.Parser, replyMarkup: new ForceReplyMarkup());
				return;
			}
			await TelegramBot.Bot.SendMessage(info.Msg.Chat, info.Text, info.Parser, replyMarkup: info.Markup);
		}

		public static async Task Edit(SendingInfo info)
		{
			ArgumentNullException.ThrowIfNull(info.Msg.From);
			ArgumentNullException.ThrowIfNull(TelegramBot.Bot);
			ArgumentNullException.ThrowIfNull(TelegramBot.Cts);

			if (info.Msg.From.IsBot)
			{
				try
				{
					await TelegramBot.Bot.EditMessageText(info.Msg.Chat, info.Msg.Id, info.Text, info.Parser, replyMarkup: info.Markup);
				}
				catch (Exception ex)
				{
					if (ex is not Telegram.Bot.Exceptions.ApiRequestException)
					{
						Console.WriteLine(ex);
						await Task.Delay(2000, TelegramBot.Cts.Token);
					}
				}
			}
		}

		public static async Task EditOrSend(SendingInfo info)
		{
			ArgumentNullException.ThrowIfNull(info.Msg.From);
			ArgumentNullException.ThrowIfNull(TelegramBot.Bot);
			ArgumentNullException.ThrowIfNull(TelegramBot.Cts);

			if (info.IsForceReply)
			{
				await TelegramBot.Bot.SendMessage(info.Msg.Chat, info.Text, info.Parser, replyMarkup: new ForceReplyMarkup());
				return;
			}

			if (info.Msg.From.IsBot)
			{
				try
				{
					await TelegramBot.Bot.EditMessageText(info.Msg.Chat, info.Msg.Id, info.Text, info.Parser, replyMarkup: info.Markup);
				}
				catch (Exception ex)
				{
					if (ex is not Telegram.Bot.Exceptions.ApiRequestException)
					{
						Console.WriteLine(ex);
						await Task.Delay(2000, TelegramBot.Cts.Token);
					}
				}
			}
			else
				await TelegramBot.Bot.SendMessage(info.Msg.Chat, info.Text, info.Parser, replyMarkup: info.Markup);
		}
	}
}
