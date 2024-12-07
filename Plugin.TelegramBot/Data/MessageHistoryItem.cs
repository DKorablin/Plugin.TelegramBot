using System;
using System.Diagnostics;
using SAL.Interface.TelegramBot.Request;

namespace Plugin.TelegramBot.Data
{
	/// <summary>Элемент истории общения с клиентом</summary>
	[DebuggerDisplay("Message=\"{Message}\", Date=\"{MessageDate}\"")]
	internal class MessageHistoryItem
	{
		/// <summary>Сообщение клиента</summary>
		public Message Message { get; private set; }

		/// <summary>Идентификатор плагина, который ответил на сообщение клиента</summary>
		public String PluginId { get; private set; }

		/// <summary>Дата сообщения</summary>
		public DateTime MessageDate { get; private set; }

		public MessageHistoryItem(String pluginId, Message message)
		{
			this.Message = message;
			this.PluginId = pluginId;
			this.MessageDate = DateTime.Now;
		}
	}
}