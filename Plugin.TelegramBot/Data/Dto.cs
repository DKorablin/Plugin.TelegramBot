using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using SalRequest = SAL.Interface.TelegramBot.Request;
using SalResponse = SAL.Interface.TelegramBot.Response;

namespace Plugin.TelegramBot.Data
{
	/// <summary>Telegram message to internal message converter</summary>
	internal static class Dto
	{
		/// <summary>Converts Telegram callbackQuery to internal message</summary>
		/// <param name="query">Callbal query</param>
		/// <returns>internal message</returns>
		public static SalRequest.Message Convert(CallbackQuery query)
		{
			SalRequest.Message result = Dto.Convert(query.Message);
			result.Data = query.Data;

			// Переписываю свойство From, ибо при CallbackQuery свойство From соответствует идентификатору бота
			result.From = new SalRequest.User() { UserId = query.From.Id, UserName = query.From.Username, FirstName = query.From.FirstName, LastName = query.From.LastName, };
			result.Type = SalRequest.MessageType.CallbackQuery;//TODO: Тут надо проверить чтобы не приходили другие типы в инлайне
			return result;
		}

		/// <summary>Converts Telegram message to internal message</summary>
		/// <param name="message">Telegram message</param>
		/// <returns>Internal message</returns>
		public static SalRequest.Message Convert(Message message)
		{
			SalRequest.Message result = new SalRequest.Message()
			{
				From = new SalRequest.User() { UserId = message.From.Id, UserName = message.From.Username, FirstName = message.From.FirstName, LastName = message.From.LastName, },
				Chat = new SalRequest.Chat() { Id = message.Chat.Id, FirstName = message.Chat.FirstName, LastName = message.Chat.LastName, Title = message.Chat.Title, UserName = message.Chat.Username, },
				Date = message.Date,
				MessageId = message.MessageId,
				Text = message.Text,
				Type = (SalRequest.MessageType)message.Type,
			};

			if(message.Contact != null)
				result.Contact = new SalRequest.Contact()
				{
					FirstName = message.Contact.FirstName,
					LastName = message.Contact.LastName,
					PhoneNumber = message.Contact.PhoneNumber,
					UserId = message.Contact.UserId,
				};
			if(message.Location != null)
				result.Location = new SalRequest.Location()
				{
					Latitude = message.Location.Latitude,
					Longitude = message.Location.Longitude,
				};
			if(message.Document != null)
				result.Document = new SalRequest.Document()
				{
					FileId = message.Document.FileId,
					FileSize = message.Document.FileSize,
					MimeType = message.Document.MimeType,
					FileName = message.Document.FileName,
				};

			if(message.Audio != null)
				result.Audio = new SalRequest.Audio()
				{
					FileId = message.Audio.FileId,
					FileSize = message.Audio.FileSize,
					MimeType = message.Audio.MimeType,
					Title = message.Audio.Title,
					Duration = message.Audio.Duration,
				};
			if(message.Voice != null)
				result.Voice = new SalRequest.Voice()
				{
					FileId = message.Voice.FileId,
					FileSize = message.Voice.FileSize,
					MimeType = message.Voice.MimeType,
					Duration = message.Voice.Duration,
				};

			if(message.ReplyToMessage != null)
				result.ReplyToMessage = Dto.Convert(message.ReplyToMessage);

			return result;
		}

		/// <summary>Converts Telegram keyboard markup to internal markup</summary>
		/// <param name="markup">Telegram keyboard markyp</param>
		/// <returns>Internal keyboard markup</returns>
		public static ReplyKeyboardMarkup Convert(SalResponse.KeyboardMarkup markup)
		{
			KeyboardButton[][] buttons = markup.Keyboard
				.Select(p => p.Select(n => new KeyboardButton(n.Text) { RequestContact = n.RequestContact, RequestLocation = n.RequestLocation }).ToArray())
				.ToArray();

			return new ReplyKeyboardMarkup(buttons, oneTimeKeyboard: markup.OneTimeKeybord);
		}

		/// <summary>Converts internal keyboard markup to Telegram keyboard markup</summary>
		/// <param name="markup">Internal keyboard markup</param>
		/// <returns>Telegram keyboard markup</returns>
		public static InlineKeyboardMarkup Convert(SalResponse.InlineKeyboardMarkup markup)
		{
			InlineKeyboardButton[][] buttons = markup.Keyboard
				.Select(p => p.Select(n => new InlineKeyboardButton() { Text = n.Text, CallbackData = n.CallbackData, Url = n.Url }).ToArray())
				.ToArray();

			return new InlineKeyboardMarkup(buttons);
		}
	}
}