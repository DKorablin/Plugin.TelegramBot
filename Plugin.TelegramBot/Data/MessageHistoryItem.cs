using System;
using System.Diagnostics;
using SAL.Interface.TelegramBot.Request;

namespace Plugin.TelegramBot.Data
{
	/// <summary>Element of client communication history</summary>
	[DebuggerDisplay("Message=\"{Message}\", Date=\"{MessageDate}\"")]
	internal class MessageHistoryItem
	{
		/// <summary>The client message</summary>
		public Message Message { get; private set; }

		/// <summary>The ID of the plugin that responded to the client's message</summary>
		public String PluginId { get; private set; }

		/// <summary>Date of message</summary>
		public DateTime MessageDate { get; private set; }

		/// <summary>Create instance of <see cref="MessageHistoryItem"/> with required arguments.</summary>
		/// <param name="pluginId">The plugin identifier.</param>
		/// <param name="message">The users message.</param>
		public MessageHistoryItem(String pluginId, Message message)
		{
			this.Message = message;
			this.PluginId = pluginId;
			this.MessageDate = DateTime.Now;
		}
	}
}