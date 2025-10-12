using System;
using System.Net;
using SAL.Flatbed;

namespace Plugin.TelegramBot.Data
{
	/// <summary>Wrapper for the plugin that provides random proxies</summary>
	public class ProxyPluginWrapper
	{
		/// <summary>Plugin.RandomProxy</summary>
		private static class ProxyPlugin
		{
			/// <summary>Plugin for storing/loading and providing random proxies</summary>
			public const String Name = "0dac4c53-ba8e-4b79-a4c3-b616470abc46";

			public static class Methods
			{
				/// <summary>Get a random proxy</summary>
				public const String GetRandom = "GetRandom";
			}
		}

		private readonly IHost _host;
		private IPluginDescription _plugin;

		/// <summary>The found plugin instance in the list of loaded plugins</summary>
		private IPluginDescription Plugin
		{
			get => this._plugin ?? (this._plugin = this._host.Plugins[ProxyPlugin.Name]);
		}

		/// <summary>Creating an instance of the plugin facade with proxies</summary>
		/// <param name="host">Host interface</param>
		internal ProxyPluginWrapper(IHost host)
			=> this._host = host ?? throw new ArgumentNullException(nameof(host));

		/// <summary>Get a random proxy</summary>
		/// <returns>Random proxy for use in services</returns>
		public IWebProxy GetRandomProxy()
			=> this.Plugin == null
				? null
				: (IWebProxy)this.Plugin.Type.GetMember<IPluginMethodInfo>(ProxyPlugin.Methods.GetRandom).Invoke();
	}
}