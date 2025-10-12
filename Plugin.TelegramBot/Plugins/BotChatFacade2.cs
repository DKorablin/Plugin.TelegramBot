using System;
using System.Linq;
using System.Reflection;
using SAL.Interface.TelegramBot;
using SAL.Interface.TelegramBot.Request;
using SAL.Interface.TelegramBot.Response;
using SAL.Flatbed;

namespace Plugin.TelegramBot.Plugins
{
	internal class BotChatFacade2 : BotChatFacade
	{
		private readonly Boolean _isProcessMessageOverridden;

		public BotChatFacade2(IPluginDescription botHost, Type instanceType)
			: base(botHost, instanceType)
		{
			if(instanceType.BaseType != typeof(ChatHandler))
				throw new InvalidOperationException($"Type must resolve {nameof(ChatHandler)} abstract class");

			MethodInfo baseMethod = typeof(ChatHandler).GetMethod(nameof(ProcessMessage));
			MethodInfo method = instanceType.GetMethod(baseMethod.Name,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly,
				null,
				baseMethod.GetParameters().Select(p => p.ParameterType).ToArray(),
				null);

			this._isProcessMessageOverridden = method != null;
		}

		protected override T CreateChatInstance<T>(Message message)
		{
			IChatMarker result = base.CreateChatInstance<T>(message);
			((ChatHandler)result).Message = message;
			return (T)result;
		}

		public override Reply[] ProcessMessage(Message message)
			=> this._isProcessMessageOverridden
				? base.ProcessMessage(message)
				: null;
	}
}