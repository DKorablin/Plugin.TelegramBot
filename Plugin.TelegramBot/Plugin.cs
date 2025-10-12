using System;
using System.Diagnostics;
using System.IO;
using Plugin.TelegramBot.Data;
using Plugin.TelegramBot.Plugins;
using SAL.Flatbed;

namespace Plugin.TelegramBot
{
	/// <summary>Startup plugin logic instance.</summary>
	public class Plugin : IPlugin, IPluginSettings<PluginSettings>
	{
		private TraceSource _trace;
		private PluginSettings _settings;
		private ProxyPluginWrapper _proxyPlugin;
		private readonly IHost _host;
		private BotHost _botHost;

		/// <summary>The event is fired when plugin connects to Telegram servers.</summary>
		public event EventHandler<DataEventArgs> Connected;

		/// <summary>The event is fired when plugin disconnects from Telegram servers.</summary>
		public event EventHandler<DataEventArgs> Disconnected;

		internal TraceSource Trace { get => this._trace ?? (this._trace = Plugin.CreateTraceSource<Plugin>()); }

		/// <summary>Settings for interaction from the plugin</summary>
		public PluginSettings Settings
		{
			get
			{
				if(this._settings == null)
				{
					this._settings = new PluginSettings();
					this._host.Plugins.Settings(this).LoadAssemblyParameters(this._settings);
				}
				return this._settings;
			}
		}

		Object IPluginSettings.Settings { get => this.Settings; }

		internal ProxyPluginWrapper ProxyPlugin { get => this._proxyPlugin ?? (this._proxyPlugin = new ProxyPluginWrapper(this._host)); }

		internal BotStorage ChatPlugins { get; private set; }

		/// <summary>Create instance if <see cref="Plugin"/> with the reference to host instance.</summary>
		/// <param name="host">The host instance reference</param>
		/// <exception cref="ArgumentNullException">The host instance reference should be specified</exception>
		public Plugin(IHost host)
			=> this._host = host ?? throw new ArgumentNullException(nameof(host));

		Boolean IPlugin.OnConnection(ConnectMode mode)
		{
			if(String.IsNullOrEmpty(this.Settings.Token))
			{
				this.Trace.TraceEvent(TraceEventType.Error, 10, "TelegramBot -> Telegram token is required for bot connection");
				return false;
			}

			this.ChatPlugins = new BotStorage(this, this._host);

			//TODO: Check that all plugins are already loaded.
			this._botHost = new BotHost(this, this.Settings.Token);

			return true;
		}

		Boolean IPlugin.OnDisconnection(DisconnectMode mode)
		{
			this._botHost?.Stop();
			return true;
		}

		internal void OnConnected(Object sender, EventArgs e)
			=> this.Connected?.Invoke(this, DataEventArgs.Empty);

		internal void OnDisconnected(Object sender, EventArgs e)
			=> this.Disconnected?.Invoke(this, DataEventArgs.Empty);

		#region Bot Methods
		/// <summary>Check bot connection to Telegram server</summary>
		/// <returns>Bot is connected to Telegram server and receiving messages</returns>
		public Boolean IsConnected()
			=> this._botHost?.IsReceiving == true;

		/// <summary>Send message to client chat</summary>
		/// <param name="chatId">Chat identifier to send message to</param>
		/// <param name="message">Message to send to chat</param>
		/// <exception cref="InvalidOperationException">Bot disconnected from Telegram server</exception>
		public void SendMessageToChat(Int64 chatId, String message)
		{
			if(!this.IsConnected())
				throw new InvalidOperationException("Bot disconnected");

			this._botHost.SendMessageToChat(chatId, message);
		}

		/// <summary>Send file to client chat</summary>
		/// <param name="chatId">Chat identifier to send message to</param>
		/// <param name="name">Name of file to send</param>
		/// <param name="payload">File content</param>
		public void SendMessageToChat(Int64 chatId, String name, Byte[] payload)
		{
			if(!this.IsConnected())
				throw new InvalidOperationException("Bot disconnected");

			this._botHost.SendMessageToChat(chatId, name, payload);
		}

		/// <summary>Download document sent from client</summary>
		/// <param name="fileId">Client document identifier</param>
		/// <returns>Data stream from client</returns>
		public Stream DownloadDocument(String fileId)
		{
			if(!this.IsConnected())
				throw new InvalidOperationException("Bot disconnected");

			return this._botHost.DownloadDocument(fileId);
		}
		#endregion Bot Methods

		internal static TraceSource CreateTraceSource<T>(String name = null) where T : IPlugin
		{
			TraceSource result = new TraceSource(typeof(T).Assembly.GetName().Name + name);
			result.Switch.Level = SourceLevels.All;
			result.Listeners.Remove("Default");
			result.Listeners.AddRange(System.Diagnostics.Trace.Listeners);
			return result;
		}
	}
}