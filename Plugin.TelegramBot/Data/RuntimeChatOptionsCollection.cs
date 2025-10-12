using System;
using System.Collections.Concurrent;

namespace Plugin.TelegramBot.Data
{
	/// <summary>Runtime chats options</summary>
	internal class RuntimeChatOptionsCollection
	{
		private const Int32 TemporaryLockedMaxMinutes = 15;

		/// <summary>Runtime chat options</summary>
		internal class RuntimeChatOptions
		{
			/// <summary>This chat is temporary locked by Telegram system</summary>
			/// <remarks>For example: Too many requests</remarks>
			public DateTime? TemporaryLocked { get; set; }
		}

		private readonly ConcurrentDictionary<Int64, RuntimeChatOptions> _optionsCollection = new ConcurrentDictionary<Int64, RuntimeChatOptions>();
		
		/// <summary>Lock chat because too many messages to this chat</summary>
		/// <param name="chatId">ID of the chat which need to be temporary locked</param>
		public void TooManyRequestsLock(Int64 chatId)
		{
			RuntimeChatOptions options = this.GetOptions(chatId);
			options.TemporaryLocked = DateTime.Now.AddMinutes(TemporaryLockedMaxMinutes);
		}


		/// <summary>Check the chat for too many requests lock</summary>
		/// <param name="chatId">ID of the chat what we need to check for too many messages</param>
		/// <returns>Chat is locked</returns>
		public Boolean IsTooManyRequestsLock(Int64 chatId)
		{
			if(this._optionsCollection.TryGetValue(chatId, out RuntimeChatOptions options))
			{
				if(options.TemporaryLocked != null && options.TemporaryLocked > DateTime.Now)
					return true;
				else
					this._optionsCollection.TryRemove(chatId, out _);
			}
			return false;
		}

		private RuntimeChatOptions GetOptions(Int64 chatId)
			=> this._optionsCollection.GetOrAdd(chatId, (Int64) => new RuntimeChatOptions());
	}
}