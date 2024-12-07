using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using SAL.Interface.TelegramBot;
using SAL.Interface.TelegramBot.Request;
using SAL.Interface.TelegramBot.Response;
using SAL.Interface.TelegramBot.UI;
using Plugin.TelegramBot.Data;
using SAL.Flatbed;

namespace Plugin.TelegramBot.Plugins
{
	internal class BotChatFacade
	{
		private readonly IPluginDescription _botHost;
		private readonly Lazy<MemoryCache> _instanceChatCache = new Lazy<MemoryCache>(delegate { return new MemoryCache("Telegram.Bot.Chat"); });
		private readonly Lazy<MemoryCache> _instanceUserCache = new Lazy<MemoryCache>(delegate { return new MemoryCache("Telegram.Bot.User"); });

		private ConstructorInfo _ctor;
		private IChatMarker _instance;
		
		private Dictionary<String, MethodInfo> _chatMethods = new Dictionary<String, MethodInfo>();
		private Dictionary<Int32, MethodInfo> _chatReplyToMethods = new Dictionary<Int32, MethodInfo>();
		private Dictionary<Int32, MethodInfo> _callbackMethods = new Dictionary<Int32, MethodInfo>();

		/// <summary>Для инстанса найден конструтор и определна возможность для создания инстанса через конструктор</summary>
		public Boolean IsConnected { get { return this._ctor != null; } }

		/// <summary>Тип инстанса который используется для создания самого инстанса</summary>
		public Type Type { get; }

		/// <summary>Тип кеширования инстанса</summary>
		public LifeCycle LifeCycle { get; private set; }

		private Boolean _hasUsage;
		private Boolean _hasProcessor;

		public BotChatFacade(IPluginDescription botHost, Type instanceType)
		{
			this._botHost = botHost;
			this.Type = instanceType ?? throw new ArgumentNullException(nameof(instanceType));
			this._hasUsage = instanceType.GetInterfaces().Any(i => i == typeof(IChatUsage));
			this._hasProcessor = instanceType.GetInterfaces().Any(i => i == typeof(IChatProcessor));

			this.SpecifyCtor();
			if(this.IsConnected)
				this.CollectMethods();
		}

		public MethodInfo Find(MessageParser parser)
		{
			MethodInfo result;
			if(parser.MethodHash != null && this._callbackMethods.TryGetValue(parser.MethodHash.Value, out result))
				return result;
			else if(parser.MethodName != null && this._chatMethods.TryGetValue(parser.MethodName, out result))
				return result;
			else if(parser.ReplyToMessageHash != null && this._chatReplyToMethods.TryGetValue(parser.ReplyToMessageHash.Value, out result))
				return result;
			else
				return null;
		}

		public virtual IEnumerable<UsageReply> GetUsage(Message message)
		{
			if(this._hasUsage)
			{
				IChatUsage instance = this.GetChatInstance<IChatUsage>(message);
				IEnumerable<UsageReply> replies = instance.GetUsage(message);
				if(replies != null)
					foreach(UsageReply reply in replies)
						yield return reply;
			} else
				foreach(var chatMethod in this._chatMethods)
				{
					ChatShortcutAttribute chatShortcut = (ChatShortcutAttribute)chatMethod.Value.GetCustomAttribute(typeof(ChatShortcutAttribute));
					yield return new UsageReply(chatShortcut);
				}
		}

		public virtual Reply[] ProcessMessage(Message message)
		{
			if(this._hasProcessor)
			{
				IChatProcessor instance = this.GetChatInstance<IChatProcessor>(message);
				IEnumerable<Reply> replies = instance.ProcessMessage(message);
				return replies == null
					? null
					: replies.ToArray();
			} else
				return null;
		}

		public Reply[] Invoke(MessageParser parser,MethodInfo method)
		{
			return Invoke(parser.Message, method, parser.Args);
		}

		public Reply[] Invoke(Message message, MethodInfo method, String[] args = null)
		{
			return InvokeI(message, this.GetChatInstance<IChatMarker>(message), args, method);
		}

		private static Reply[] InvokeI(Message message, Object instance, String[] args, params MethodInfo[] methods)
		{
			foreach(MethodInfo method in methods)
			{
				ParameterInfo[] prmInfos = method.GetParameters();

				List<Object> parameters = new List<Object>(prmInfos.Length);
				Int32 loop = 0;
				foreach(ParameterInfo prm in prmInfos)
				{
					if(prm.ParameterType == typeof(Message))
						parameters.Add(message);
					else if(args != null && args.Length > loop)
					{
						Object value;
						if(Utils.TryChangeValue(args[loop++], prm, out value))
							parameters.Add(value);
					} else if(prm.HasDefaultValue)
						parameters.Add(prm.DefaultValue);
				}

				if(parameters.Count != prmInfos.Length)
					continue;

				return ((IEnumerable<Reply>)method.Invoke(instance, parameters.ToArray())).ToArray();
			}
			return null;
		}

		/// <summary>Получить инстанс чата исходя из сообщения</summary>
		/// <param name="message">Сообщение</param>
		/// <returns></returns>
		protected T GetChatInstance<T>(Message message) where T : IChatMarker
		{
			switch(this.LifeCycle)
			{
			case LifeCycle.Singleton:
				return (T)(this._instance ?? (this._instance = this.CreateChatInstance<T>(message)));
			case LifeCycle.ChatSingleton:
				return this._instanceChatCache.Value.GetFromCache(message.Chat.Id.ToString(),
					new TimeSpan(1, 0, 0),
					null,
					delegate
					{
						return this.CreateChatInstance<T>(message);
					});
			case LifeCycle.UserSingleton:
				return this._instanceUserCache.Value.GetFromCache(message.From.UserId.ToString(),
					new TimeSpan(0, 30, 0),
					null,
					delegate
					{
						return this.CreateChatInstance<T>(message);
					});
			case LifeCycle.Transient:
				return this.CreateChatInstance<T>(message);
			default:
				throw new NotImplementedException();
			}
		}

		protected virtual T CreateChatInstance<T>(Message message) where T : IChatMarker
		{
			ParameterInfo[] parameters = this._ctor.GetParameters();
			List<Object> values = new List<Object>(parameters.Length);
			foreach(ParameterInfo parameter in parameters)
				if(parameter.ParameterType == this._botHost.Instance.GetType())
					values.Add(this._botHost.Instance);//Интерфейс плагина
				else if(parameter.ParameterType == typeof(Chat))
					values.Add(message.Chat);//Идентификатор чата
				else if(parameter.ParameterType == typeof(User))
					values.Add(message.From);//Идентификатор пользователя
				else if(parameter.ParameterType == typeof(Message))
					values.Add(message);//Сообщение
				else throw new NotSupportedException();

			return (T)this._ctor.Invoke(values.ToArray());
		}

		/// <summary>Выбираем конструктор и регламент кеширования инстанса</summary>
		private void SpecifyCtor()
		{
			foreach(ConstructorInfo ctor in this.Type
				.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
				.OrderByDescending(p => p.GetParameters().Length))
			{
				//TODO: Нужен IoC контейнер с поддерживаемыми типами
				ParameterInfo[] parameters = ctor.GetParameters();
				if(parameters.Length == 0
					|| parameters.All(p => p.ParameterType == this._botHost.Instance.GetType()))
				{//Один инстанс чата на весь процесс
					this._ctor = ctor;
					this.LifeCycle = LifeCycle.Singleton;
					break;
				} else if(parameters.All(p => p.ParameterType == this._botHost.Instance.GetType()
					|| p.ParameterType == typeof(Chat)))
				{//Чат для всего чата целиком
					this._ctor = ctor;
					this.LifeCycle = LifeCycle.ChatSingleton;
					break;
				} else if(parameters.All(p => p.ParameterType == this._botHost.Instance.GetType()
					|| p.ParameterType == typeof(User)))
				{//Чат для конкретного пользователя
					this._ctor = ctor;
					this.LifeCycle = LifeCycle.UserSingleton;
				} else if(parameters.All(p => p.ParameterType == this._botHost.Instance.GetType()
					 || p.ParameterType == typeof(Message)))
				{//Чат для каждого сообщения
					this._ctor = ctor;
					this.LifeCycle = LifeCycle.Transient;
				}
			}
		}

		private void CollectMethods()
		{
			MethodInfo[] methods = this.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
			foreach(MethodInfo method in methods.Where(m => m.ReturnType == typeof(IEnumerable<Reply>) || m.ReturnType == typeof(Reply[]) || m.ReturnType == typeof(Reply)))
			{
				ChatShortcutAttribute chatShortcut = (ChatShortcutAttribute)method.GetCustomAttribute(typeof(ChatShortcutAttribute));
				if(chatShortcut != null)
				{
					if(chatShortcut.Key != null)
						this._chatMethods.Add(chatShortcut.Key, method);
					if(chatShortcut.ReplyToKey != null)
						this._chatReplyToMethods.Add(chatShortcut.ReplyToKey.GetHashCode(), method);
				}

				Int32 methodKey = MethodInvoker.GetMethodKey(method);
				this._callbackMethods.Add(methodKey, method);
			}
		}
	}
}