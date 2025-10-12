using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SAL.Interface.TelegramBot;
using SAL.Flatbed;

namespace Plugin.TelegramBot.Plugins
{
	/// <summary>Resolver of all chat handlers in the plugin</summary>
	internal class BotPluginResolver
	{
		private static readonly Type ChatMarkerType = typeof(IChatMarker);
		private static readonly Type ChatHandlerType = typeof(ChatHandler);
		private readonly BotChatFacade[] _instances;

		/// <summary>Plugin interface which is the source of all handlers</summary>
		public IPluginDescription BotPlugin { get; private set; }

		/// <summary>Array of facades for all plugin chats</summary>
		public IEnumerable<BotChatFacade> Instances
		{
			get
			{
				foreach(BotChatFacade instance in this._instances)
					yield return instance;
			}
		}

		/// <summary>Number of loaded facades</summary>
		public Int32 Count { get => this._instances.Length; }

		/// <summary>Create class instance, find all instances and wrap them into facades</summary>
		/// <param name="plugin">Plugin in which we search all instances</param>
		public BotPluginResolver(IPluginDescription plugin)
		{
			this.BotPlugin = plugin;
			Assembly botAssembly = this.BotPlugin.Instance.GetType().Assembly;

			List<BotChatFacade> instances = new List<BotChatFacade>();
			if(botAssembly.GetReferencedAssemblies().Any(p => p.FullName == BotPluginResolver.ChatMarkerType.Assembly.GetName().FullName))
			{//TODO: Need to check that assemblies bind correctly with BindingRedirect (i.e. if config redirects 1.0 to 2.0 then code reference is also 2.0)
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