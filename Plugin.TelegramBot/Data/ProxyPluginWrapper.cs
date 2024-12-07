using System;
using System.Net;
using SAL.Flatbed;

namespace Plugin.TelegramBot.Data
{
	/// <summary>Обёртка для плагина с рандомными проксями</summary>
	public class ProxyPluginWrapper
	{
		/// <summary>Plugin.RandomProxy</summary>
		private static class ProxyPlugin
		{
			/// <summary>Плагин хранения/загрузки и выдачи рандомных проксей</summary>
			public const String Name = "0dac4c53-ba8e-4b79-a4c3-b616470abc46";

			public static class Methods
			{
				/// <summary>Получить рандомный прокси</summary>
				public const String GetRandom = "GetRandom";
			}
		}

		private readonly IHost _host;
		private IPluginDescription _plugin;

		/// <summary>Найденный инстанс плагина в списке загруженных</summary>
		private IPluginDescription Plugin
		{
			get
			{
				return this._plugin == null
					? this._plugin = this._host.Plugins[ProxyPlugin.Name]
					: this._plugin;
			}
		}

		/// <summary>Создание инстанса фасада плагина с проксями</summary>
		/// <param name="host">Интерфейс хоста</param>
		internal ProxyPluginWrapper(IHost host)
		{
			if(host == null)
				throw new ArgumentNullException("host");

			this._host = host;
		}

		/// <summary>Получить рандомный прокси</summary>
		/// <returns>Рандомный прокси для использования в сервисах</returns>
		public IWebProxy GetRandomProxy()
		{
			if(this.Plugin == null)
				return null;

			return (IWebProxy)this.Plugin.Type.GetMember<IPluginMethodInfo>(ProxyPlugin.Methods.GetRandom).Invoke();
		}
	}
}