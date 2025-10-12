using System;
using SAL.Interface.TelegramBot.Request;

namespace Plugin.TelegramBot.Data
{
	internal class MessageParser
	{
		public String Command { get; private set; }

		public String MethodName { get; private set; }

		public Int32? MethodHash { get; private set; }

		public Int32? ReplyToMessageHash { get; private set; }

		public String[] Args { get; private set; }

		public Message Message { get; }

		public MessageParser(Message message)
		{
			this.Message = message ?? throw new ArgumentNullException(nameof(message));
			this.Initialize(message);
		}

		private void Initialize(Message message)
		{
			if(message.Data != null && message.Data.StartsWith("/"))
				this.Command = message.Data;
			else if(message.Text != null && message.Text.StartsWith("/"))
				this.Command = message.Text;

			if(this.Command != null)
			{
				Int32 methodEnd = this.Command.IndexOf(':');
				if(methodEnd > -1)
				{
					this.MethodName = this.Command.Substring(1, methodEnd - 1);
					if(Int32.TryParse(this.MethodName, out Int32 methodId))
					{
						this.MethodHash = methodId;
						if(this.Command.Length > methodEnd + 1)
							this.Args = this.Command.Substring(methodEnd + 1).Split('&');
					}
				}

				if(this.MethodHash == null)
				{
					methodEnd = this.Command.IndexOfAny(new Char[] { ' ', });
					if(methodEnd == -1)
						methodEnd = this.Command.IndexOfAny(new Char[] { '_' });
					if(methodEnd > -1)
					{
						this.MethodName = this.Command.Substring(0, methodEnd);
						this.Args = this.Command.Substring(methodEnd + 1).Split('_');
					} else//If the command has no arguments
						this.MethodName = this.Command;
				}
			} else if(message.ReplyToMessage != null && message.ReplyToMessage.Text != null)
				this.ReplyToMessageHash = message.ReplyToMessage.Text.GetHashCode();
		}
	}
}