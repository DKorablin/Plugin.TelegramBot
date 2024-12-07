using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SAL.Interface.TelegramBot;
using SAL.Flatbed;

namespace Plugin.TelegramBot.Plugins
{
	/// <summary>Резолвер всех хендлеров чата в плагине</summary>
	internal class BotPluginResolver
	{
		private static readonly Type ChatMarkerType = typeof(IChatMarker);
		private static readonly Type ChatHandlerType = typeof(ChatHandler);
		private readonly BotChatFacade[] _instances;

		/// <summary>Интерфейс плагина который является первоисточником всех хендлеров</summary>
		public IPluginDescription BotPlugin { get; private set; }

		/// <summary>Массив фасадов всех чатов плагина</summary>
		public IEnumerable<BotChatFacade> Instances
		{
			get
			{
				foreach(BotChatFacade instance in this._instances)
					yield return instance;
			}
		}

		/// <summary>Кол-во загруженных фасадов</summary>
		public Int32 Count { get => this._instances.Length; }

		/// <summary>Создание экземпляра класса с поиском всех инстансов и обёртка их в фасад</summary>
		/// <param name="plugin">Плагин в котором ищем все инстансы</param>
		public BotPluginResolver(IPluginDescription plugin)
		{
			this.BotPlugin = plugin;
			Assembly botAssembly = this.BotPlugin.Instance.GetType().Assembly;

			List<BotChatFacade> instances = new List<BotChatFacade>();
			if(botAssembly.GetReferencedAssemblies().Any(p => p.FullName == BotPluginResolver.ChatMarkerType.Assembly.GetName().FullName))
			{//TODO: Надо проверить что при BindingRedirect сборки цепляются верно (Т.е. если в конфиге редирект с 1.0 на 2.0, то и в коде reference будет на 2.0)
				foreach(Type t in botAssembly.GetTypes())
				{
					if(t.BaseType == BotPluginResolver.ChatHandlerType)
						instances.Add(new BotChatFacade2(plugin, t));
					else
					{
						Type[] interfaces = t.GetInterfaces();
						if(interfaces.Any(i => i == BotPluginResolver.ChatMarkerType))
							instances.Add(new BotChatFacade(plugin, t));
					}
				}
			}
			this._instances = instances.ToArray();
		}
	}
}