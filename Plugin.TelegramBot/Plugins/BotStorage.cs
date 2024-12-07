using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Caching;
using SAL.Interface.TelegramBot;
using SAL.Interface.TelegramBot.Request;
using SAL.Interface.TelegramBot.Response;
using Plugin.TelegramBot.Data;
using SAL.Flatbed;

namespace Plugin.TelegramBot.Plugins
{
	/// <summary>Хранилище чатов</summary>
	internal class BotStorage
	{
		private readonly IHost _host;
		private readonly Plugin _plugin;
		private readonly Lazy<MemoryCache> _chatCache = new Lazy<MemoryCache>(delegate { return new MemoryCache("Plugin.TelegramBot"); });

		private readonly Object _chatPluginsLock = new Object();
		private BotPluginResolver[] _chatPlugins;

		internal MemoryCache ChatCache { get { return _chatCache.Value; } }

		public BotStorage(Plugin plugin, IHost host)
		{
			this._host = host;
			this._plugin = plugin;
		}

		private BotPluginResolver[] ResolveBotPlugins()
		{
			if(this._chatPlugins == null)
				lock(_chatPluginsLock)
					if(this._chatPlugins == null)
					{
						Stopwatch sw = new Stopwatch();
						sw.Start();
						List<BotPluginResolver> plugins = new List<BotPluginResolver>();// (this._host.Plugins.FindPluginType<IBotMarker>());
						foreach(IPluginDescription plugin in this._host.Plugins)
						{
							BotPluginResolver resolver = new BotPluginResolver(plugin);
							if(resolver.Count > 0)
								plugins.Add(resolver);
						}

						String[] priority = (this._plugin.Settings.Priority ?? String.Empty).Split(new String[] { Environment.NewLine, }, StringSplitOptions.RemoveEmptyEntries);
						Int32 priorityIndex = 0;

						if(priority.Length > 0)
							for(Int32 loop = 0; loop < priority.Length; loop++)
							{//TODO: Если в приоритете плагин не найдётся, то иерархия сломается
								String pluginKey = priority[loop];
								for(Int32 innerLoop = 0; innerLoop < plugins.Count; innerLoop++)
								{
									BotPluginResolver resolver = plugins[innerLoop];
									if(resolver.BotPlugin.ID == pluginKey || resolver.BotPlugin.Name == pluginKey)
									{
										if(innerLoop != priorityIndex)
										{
											plugins.RemoveAt(innerLoop);
											plugins.Insert(priorityIndex, resolver);
										}
										priorityIndex++;
										break;
									}
								}
							}

						sw.Stop();
						List<String> message = new List<String>()
						{
							$"Loaded {plugins.Count} chat plugins at: {sw.Elapsed}",
						};
						foreach(BotPluginResolver resolver in plugins)
						{
							message.Add($"\tPlugin: {resolver.BotPlugin.Name}");
							foreach(var instance in resolver.Instances)
								message.Add($"\t\tInstance: {instance.Type.Name} ({instance.LifeCycle})");
						}
						this._plugin.Trace.TraceEvent(TraceEventType.Start, 2, String.Join(Environment.NewLine, message.ToArray()));
						this._chatPlugins = plugins.ToArray();
					}

			return this._chatPlugins;
		}

		/// <summary>Обработать сообщение и получить ответ от плагина</summary>
		/// <param name="message">Обрабатываемое сообщение от телеграма</param>
		/// <returns>Ответ от плагина</returns>
		public IEnumerable<Reply> GetPluginReply(Message message)
		{
			MessageParser parsedMessage = new MessageParser(message);
			Reply[] replies = this.GetPluginReply(delegate (BotChatFacade instance)
			{
				MethodInfo method = instance.Find(parsedMessage);
				return method == null
					? null
					: instance.Invoke(parsedMessage, method);
			});

			if(replies != null && replies.Length > 0)
				return replies;

			replies = this.GetPluginReply(delegate (BotChatFacade instance)
			{
				return instance.ProcessMessage(parsedMessage.Message);
			});

			if(replies != null && replies.Length > 0)
				return replies;

			return null;
		}

		private Reply[] GetPluginReply(Func<BotChatFacade,Reply[]> callback)
		{
			foreach(BotPluginResolver resolver in this.ResolveBotPlugins())
				foreach(BotChatFacade instance in resolver.Instances)
				{
					Reply[] replies = callback(instance);
					if(replies != null && replies.Length > 0)
						return replies;
				}
			return null;
		}

		/// <summary>Получить информацию о вариантах использования плагинов</summary>
		/// <param name="message">Обрабатываемое сообщение от телеграмма</param>
		/// <returns>Список использования плагинов</returns>
		public IEnumerable<UsageReply> GetPluginUsage(Message message)
		{
			foreach(BotPluginResolver resolver in this.ResolveBotPlugins())
				foreach(BotChatFacade instance in resolver.Instances)
					foreach(UsageReply reply in instance.GetUsage(message))
						yield return reply;
		}
	}
}