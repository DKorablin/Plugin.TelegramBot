using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Plugin.TelegramBot.Data;
using SAL.Flatbed;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Sal = SAL.Interface.TelegramBot;
using SalRequest = SAL.Interface.TelegramBot.Request;
using SalResponse = SAL.Interface.TelegramBot.Response;
#pragma warning disable 612, 618

namespace Plugin.TelegramBot
{
	internal class BotHost
	{
		private static TimeSpan DueTimeout = new TimeSpan(0, 0, 0, 0, -1);

		private readonly Timer _reconnectTimer;
		private readonly String _token;

		private Plugin Plugin { get; }

		private TelegramBotClient BotClient { get; set; }

		public Boolean IsRecieving { get => this.BotClient.IsReceiving; }

		public BotHost(Plugin plugin, String token)
		{//TODO: https://github.com/TelegramBots/Telegram.Bot.Extensions.Passport/blob/f101d81bea4833a16e2d169c98559d2d526c0f86/src/Quickstart/Program.cs
			if(String.IsNullOrEmpty(token))
				throw new ArgumentNullException(nameof(token));

			this._token = token;
			this.Plugin = plugin ?? throw new ArgumentException(nameof(plugin));
			this._reconnectTimer = new Timer(this.ReconnectTimer_InvokeTimer, null, BotHost.DueTimeout, this.Plugin.Settings.ReconnectTimeout);

			this.Start();
		}

		private void ReconnectTimer_InvokeTimer(Object state)
		{
			if(!this.IsRecieving)
			{
				this.Plugin.Trace.TraceEvent(TraceEventType.Verbose, 7, "TelegramBot -> Reconnecting...");
				this._reconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
				this.Start();
			}
		}

		/// <summary>Запуск бота</summary>
		private void Start()
		{
			this.Stop();

			IWebProxy proxy = this.Plugin.ProxyPlugin.GetRandomProxy();
			this.BotClient = proxy == null ? new TelegramBotClient(this._token) : new TelegramBotClient(this._token, proxy);

			this.BotClient.OnCallbackQuery += this.BotOnCallbackQueryReceived;
			this.BotClient.OnMessage += this.BotOnMessageReceived;
			this.BotClient.OnMessageEdited += this.BotOnMessageReceived;
			this.BotClient.OnInlineQuery += this.BotOnInlineQueryReceived;
			this.BotClient.OnInlineResultChosen += this.BotOnChosenInlineResultReceived;
			this.BotClient.OnReceiveError += this.BotOnReceiveError;
			this.BotClient.OnReceiveGeneralError += this.BotClient_OnReceiveGeneralError;

			Stopwatch sw = new Stopwatch();
			sw.Start();

			_ = this.BotClient.GetMeAsync().ContinueWith((t) =>
			{
				sw.Stop();
				if(t.IsCompleted && t.Exception == null)
					this.OnConnected(t.Result, sw.Elapsed);
				else
				{
					if(t.IsCanceled)
						this.Plugin.Trace.TraceEvent(TraceEventType.Stop, 0, "TelegramBot -> Запуск бота прерван ({0})", sw.Elapsed);
					else if(t.Exception != null)
					{
						Exception exc = t.Exception is AggregateException ? t.Exception.InnerException : t.Exception;
						if(proxy != null && proxy is WebProxy wProxy)
							exc.Data.Add("WebProxy", wProxy.Address);

						this.Plugin.Trace.TraceData(TraceEventType.Error, 10, exc);
						this._reconnectTimer.Change(BotHost.DueTimeout, this.Plugin.Settings.ReconnectTimeout);
					} else
						this.Plugin.Trace.TraceEvent(TraceEventType.Error, 10, "TelegramBot -> Не удалось подключиться к серверу Telegram ({0})", sw.Elapsed);
				}
			});
		}

		private void OnConnected(User user, TimeSpan elapsed)
		{
			this.BotClient.StartReceiving();
			this.Plugin.Trace.TraceEvent(TraceEventType.Start, 0, "TelegramBot -> Запущен под пользователем: {0} ({1})", user.Username, elapsed);
			this.Plugin.OnConnected(this, EventArgs.Empty);

		}

		private void BotClient_OnReceiveGeneralError(Object sender, ReceiveGeneralErrorEventArgs e)
		{
			this._reconnectTimer.Change(BotHost.DueTimeout,this.Plugin.Settings.ReconnectTimeout);
			this.Plugin.Trace.TraceData(TraceEventType.Error, 10, e.Exception);
			this.Plugin.OnDisconnected(this, EventArgs.Empty);
		}

		/// <summary>Остановка бота</summary>
		public void Stop()
		{
			if(this.BotClient == null)
				return;

			this._reconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
			if(this.BotClient.IsReceiving)
			{
				this.BotClient.StopReceiving();
				this.BotClient.OnCallbackQuery -= this.BotOnCallbackQueryReceived;
				this.BotClient.OnMessage -= this.BotOnMessageReceived;
				this.BotClient.OnMessageEdited -= this.BotOnMessageReceived;
				this.BotClient.OnInlineQuery -= this.BotOnInlineQueryReceived;
				this.BotClient.OnInlineResultChosen -= this.BotOnChosenInlineResultReceived;
				this.BotClient.OnReceiveError -= this.BotOnReceiveError;
				this.Plugin.Trace.TraceEvent(TraceEventType.Stop, 1, "TelegramBot -> Остановка бота");
			}
		}

		/// <summary>Отправить сообщение в чат с клиентом</summary>
		/// <param name="chatId">Идентификатор чата с получателем сообщения</param>
		/// <param name="message">Сообщение клиенту</param>
		public void SendMessageToChat(Int64 chatId, String message)
		{
			if(String.IsNullOrWhiteSpace(message))
				throw new ArgumentException("Необходимо указать текст для отправки в чат", "message");

			SalResponse.Reply reply = new SalResponse.Reply()
			{
				Title = message,
			};
			this.SendMessageToChat(chatId, reply);
		}

		/// <summary>Отправить файл в чат с клиентом</summary>
		/// <param name="chatId">Идентификатор чата с получателем сообщения</param>
		/// <param name="name">Наименование файла</param>
		/// <param name="payload">Содержимое файла</param>
		public void SendMessageToChat(Int64 chatId, String name, Byte[] payload)
		{
			if(payload == null || payload.Length == 0)
				throw new ArgumentNullException("payload", "Необходимо указать данные для передачи");

			SalResponse.Reply reply = new SalResponse.Reply()
			{
				Markup = new SalResponse.FileMarkup() { Name = name, Stream = new MemoryStream(payload), },
			};

			this.SendMessageToChat(chatId, reply);
		}

		public Stream DownloadDocument(String fileId)
		{
			MemoryStream stream = new MemoryStream();
			using(var task = this.BotClient.GetInfoAndDownloadFileAsync(fileId, stream))
				task.Wait();
			return stream;
		}

		private void SendMessageToChat(Int64 chatId, SalResponse.Reply reply)
		{
			Task<Message> response;
			try
			{
				String message;
				ParseMode parseMode = (ParseMode)(reply.ParseMode == Sal.ParseModeType.Default
					? this.Plugin.Settings.DefaultParseMode
					: reply.ParseMode);

				String[] messages = Utils.Split(reply.Title, 2048);//TODO: Тут возможно разделение сообщения, так что нужно более "умное" форматирование
				if(messages.Length > 1)
					for(Int32 loop = 0; loop < messages.Length - 1; loop++)
					{
						message = parseMode == ParseMode.Html
							? Utils.FormatHtml(messages[loop])
							: messages[loop];

						response = this.BotClient.SendTextMessageAsync(chatId, message);
						response.Wait();
					}

				message = parseMode == ParseMode.Html
					? Utils.FormatHtml(messages[messages.Length - 1])
					: messages[messages.Length - 1];

				response = this.SendMessageToChatAsync(chatId, reply, message, parseMode);
				response.Wait();
			} catch(ApiRequestException exc)
			{
				exc.Data.Add(nameof(chatId), chatId);
				switch(exc.ErrorCode)
				{
				case 403://Forbidden: bot was blocked by the user
					break;
				case 400:
					exc.Data.Add("Reply", JsonConvert.SerializeObject(reply));
					break;
				}
				this.Plugin.Trace.TraceData(TraceEventType.Error, exc.ErrorCode, exc);
			} catch(WebException exc)
			{
				exc.Data.Add(nameof(chatId), chatId);
				this.Plugin.Trace.TraceData(TraceEventType.Error, (Int32)exc.Status, exc);
			} catch(AggregateException exc)
			{
				if(exc.InnerException is ApiRequestException apiExc)
				{
					switch(apiExc.ErrorCode)
					{
					case 400://Forbidden: bot was blocked by the user
							 //this.Plugin.Trace.TraceData(TraceEventType.Error, 12, JsonConvert.SerializeObject(message));<-Trying to add message to body
						break;
					case 429://Too Many Requests: retry after 833
						this._chatOpitions.TooManyRequestsLock(chatId);
						break;
					}
					apiExc.Data.Add(nameof(chatId), chatId);
					apiExc.Data.Add("Reply", JsonConvert.SerializeObject(reply));
					this.Plugin.Trace.TraceData(TraceEventType.Error, apiExc.ErrorCode, apiExc);
				} else
				{
					exc.Data.Add(nameof(chatId), chatId);
					this.Plugin.Trace.TraceData(TraceEventType.Error, 12, exc);
				}
			} catch(Exception exc)
			{
				if(exc.IsFatal())
					throw;

				exc.Data.Add(nameof(chatId), chatId);
				this.Plugin.Trace.TraceData(TraceEventType.Error, 7, exc);
			}
		}

		private async Task<Message> SendMessageToChatAsync(Int64 chatId, SalResponse.Reply reply, String message, ParseMode parseMode)
		{
			if(this._chatOpitions.IsTooManyRequestsLock(chatId))
				throw new ArgumentException($"Chat {chatId} temporary locked", nameof(chatId));

			SalResponse.IReplyMarkup markup = reply.Markup;

			if(markup == null)
			{
				if(reply.EditMessageId == null)
					return await this.BotClient.SendTextMessageAsync(chatId, message, replyMarkup: new ReplyKeyboardRemove(), parseMode: parseMode, replyToMessageId: reply.ReplyToMessageId);
				else
					return await this.BotClient.EditMessageTextAsync(chatId, reply.EditMessageId.Value, message, parseMode: parseMode);
			} else if(markup is SalResponse.FileMarkup file)
			{
				await this.BotClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);

				using(Stream fs = file.Stream)
				{
					InputOnlineFile fts = new InputOnlineFile(fs, file.Name);
					return await this.BotClient.SendPhotoAsync(chatId, fts, message, replyMarkup: new ReplyKeyboardRemove(), replyToMessageId: reply.ReplyToMessageId);
				}
			} else if(markup is SalResponse.KeyboardMarkup keyboard)
			{
				ReplyKeyboardMarkup kb = Dto.Convert(keyboard);
				return await this.BotClient.SendTextMessageAsync(chatId, message, replyMarkup: kb, parseMode: parseMode, replyToMessageId: reply.ReplyToMessageId);

			} else if(markup is SalResponse.InlineKeyboardMarkup keyboard1)
			{
				InlineKeyboardMarkup kb = Dto.Convert(keyboard1);
				Int32? editMessageId = reply.EditMessageId ?? keyboard1.EditMessageId;
				if(editMessageId == null)
					return await this.BotClient.SendTextMessageAsync(chatId, message, replyMarkup: kb, parseMode: parseMode, replyToMessageId: reply.ReplyToMessageId);
				else
					return await this.BotClient.EditMessageTextAsync(chatId, editMessageId.Value, message, parseMode: parseMode, replyMarkup: kb);

				/*if(keyboard.EditMessageId != null)//TODO: По другому пока никак не переписать сообщение...
					await this.BotClient.DeleteMessageAsync(chatId, keyboard.EditMessageId.Value);*/

			} else if(markup is SalResponse.ForceReplyMarkup)
				return await this.BotClient.SendTextMessageAsync(chatId, message, replyMarkup: new ForceReplyMarkup(), parseMode: parseMode, replyToMessageId: reply.ReplyToMessageId);
			else if(markup is SalResponse.GeoMarkup geo)
			{
				if(message != null)
					await this.BotClient.SendTextMessageAsync(chatId, message, replyMarkup: new ReplyKeyboardRemove(), parseMode: parseMode, replyToMessageId: reply.ReplyToMessageId);
				return await this.BotClient.SendLocationAsync(chatId, geo.Latitude, geo.Longitude, replyMarkup: new ForceReplyMarkup(), replyToMessageId: reply.ReplyToMessageId);
			}

			throw new NotSupportedException("Unable to parse reply");
		}

		private readonly RuntimeChatOptionsCollection _chatOpitions = new RuntimeChatOptionsCollection();

		private void BotOnReceiveError(Object sender, ReceiveErrorEventArgs args)
		{
			this.Plugin.Trace.TraceData(TraceEventType.Error, 7, args.ApiRequestException);
			switch((HttpStatusCode)args.ApiRequestException.ErrorCode)
			{
			case HttpStatusCode.Conflict://Conflict: terminated by other getUpdates request; make sure that only one bot instance is running
				this.Stop();
				break;
			}
		}

		private async void BotOnCallbackQueryReceived(Object sender, CallbackQueryEventArgs args)
		{
			if(args.CallbackQuery.Message == null)
				return;

			try
			{
				SalRequest.Message iMessage = Dto.Convert(args.CallbackQuery);
				IEnumerable<SalResponse.Reply> reply = this.GetPluginReply(iMessage);

				if(reply == null)
				{//Записываем нераспознанное сообщение
					this.TraceUnknownMessage(iMessage);
					return;
				}

				try
				{
					foreach(SalResponse.Reply item in reply)
					{
						SalResponse.IReplyMarkup markup = item.Markup;
						ParseMode parseMode = (ParseMode)this.Plugin.Settings.DefaultParseMode;

						if(markup == null && item.Title.Length < 100)//HACK: Размер Callback не влезает определённый размер
							await this.BotClient.AnswerCallbackQueryAsync(args.CallbackQuery.Id, item.Title);
						else
							this.SendMessageToChat(args.CallbackQuery.Message.Chat.Id, item);
					}
				} catch(InvalidParameterException exc)
				{
					this.Plugin.Trace.TraceData(TraceEventType.Error, 6, exc);
				}
			}catch(Exception exc)
			{
				if(exc.IsFatal())
					throw;

				this.Plugin.Trace.TraceData(TraceEventType.Error, 5, exc);
			}
		}

		private void BotOnMessageReceived(Object sender, MessageEventArgs args)
		{
			try
			{
				Message message = args.Message;
				SalRequest.Message iMessage = Dto.Convert(message);
				/*if(message.Type == MessageType.Voice)
				{
					using(FileStream file = new FileStream("C:\\Temp\\Voice.ogg", FileMode.Create))
					using(Stream stream = this.DownloadDocument(message.Voice.FileId))
					{
						stream.Seek(0, SeekOrigin.Begin);
						stream.CopyTo(file);
					}
				}*/

				IEnumerable<SalResponse.Reply> reply = this.GetPluginReply(iMessage);

				if(reply != null)
				{
					foreach(SalResponse.Reply item in reply)
						if(item != SalResponse.Reply.Empty)//Пропускаем пустые ответы
							this.SendMessageToChat(iMessage.Chat.Id, item);
				} else//Невозможно обработать запрос
					this.Plugin.Trace.TraceEvent(TraceEventType.Warning, 7, "TelegramBot -> Empty reply");
			}catch(Exception exc)
			{
				if(exc.IsFatal())
					throw;

				this.Plugin.Trace.TraceData(TraceEventType.Error, 5, exc);
			}
		}

		private void TraceUnknownMessage(SalRequest.Message message)
		{//TODO: Тут может понадобиться история общения с клиентом
			this.Plugin.Trace.TraceData(TraceEventType.Warning, 7, JsonConvert.SerializeObject(message));
		}

		#region BotPlugins
		/// <summary>Обработать сообщение и получить ответ от плагина</summary>
		/// <param name="message">Обрабатываемое сообщение от телеграма</param>
		/// <remarks>
		/// TODO: Добавить роли для плагинов, при которых только определённые пользователи могут получить доступ к определённым плагинам (Спрятать в IPlugin)
		/// </remarks>
		/// <returns>Ответ от плагина</returns>
		private IEnumerable<SalResponse.Reply> GetPluginReply(SalRequest.Message message)
		{//TODO: Возможно придётся переделать на возврат нескольких ответов из одного плагина. В такой случае можно будет переделать на M(delegate<Message> msg)

			if(message.Type == SalRequest.MessageType.WebsiteConnected)
			{
				SalResponse.UsageReply[] reply = this.Plugin.ChatPlugins.GetPluginUsage(message).ToArray();
				//Формирую список шорткатов для пользователя
				this.BotClient.SetMyCommandsAsync(reply.Select(p => new BotCommand() { Command = p.Key, Description = p.Description }).ToArray())
					.ContinueWith(t => this.Plugin.Trace.TraceData(TraceEventType.Error, 7, t.Exception), TaskContinuationOptions.OnlyOnFaulted);
			} else
			{
				IEnumerable<SalResponse.Reply> result = this.Plugin.ChatPlugins.GetPluginReply(message);

				if(result == null)
				{//Получение описаний вариантов использования плагинов
					this.TraceUnknownMessage(message);
					result = this.GetUsageReply(message);
				}

				if(result != null)
					foreach(SalResponse.Reply reply in result)
					{
						if(message.Data != null && reply.Markup != null && this.Plugin.Settings.EditMessageOnClick)
							reply.EditMessageId = message.MessageId;
						yield return reply;
					}
			}
		}

		/// <summary>Получить варианты использования бота</summary>
		/// <param name="message">Прервоначальное сообщение клиента</param>
		/// <returns>Список возможных вариантов действия</returns>
		private IEnumerable<SalResponse.Reply> GetUsageReply(SalRequest.Message message)
		{
			List<String> usageMessage = new List<String>();
			foreach(SalResponse.UsageReply reply in this.Plugin.ChatPlugins.GetPluginUsage(message))
				usageMessage.Add(String.Join(" - ", reply.Key, reply.Description));

			if(usageMessage.Count == 0)
				yield break;
			else
			{
				if(!String.IsNullOrWhiteSpace(this.Plugin.Settings.UsageTitle))
					usageMessage.Insert(0, this.Plugin.Settings.UsageTitle);
				yield return new SalResponse.Reply() { ReplyToMessageId = message.MessageId, Title = String.Join(Environment.NewLine, usageMessage.ToArray()) };
			}
		}
		#endregion BotPlugins

		#region SampleData
		private void BotOnChosenInlineResultReceived(Object sender, ChosenInlineResultEventArgs args)
			=> this.Plugin.Trace.TraceEvent(TraceEventType.Verbose, 1, String.Format("Received choosen inline result: {0}", args.ChosenInlineResult.ResultId));

		private async void BotOnInlineQueryReceived(Object sender, InlineQueryEventArgs args)
		{
			InlineQueryResultBase[] results = {
				new InlineQueryResultLocation("1",40.7058316f,-74.2581888f,"New York")
				{
					InputMessageContent = new InputLocationMessageContent(40.7058316f,-74.2581888f) // message if result is selected
				},

				new InlineQueryResultLocation("2",52.507629f,13.1449577f,"Berlin")
				{
					InputMessageContent = new InputLocationMessageContent(13.1449577f,52.507629f) // message if result is selected
				}
			};

			await this.BotClient.AnswerInlineQueryAsync(args.InlineQuery.Id, results, isPersonal: true, cacheTime: 0);
		}
		#endregion SampleData
	}
}