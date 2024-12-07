using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using Sal = SAL.Interface.TelegramBot;

namespace Plugin.TelegramBot
{
	/// <summary>Настройки хоста бота</summary>
	public class PluginSettings : INotifyPropertyChanged
	{
		/// <summary>Строковые наименования своёств для INotifyPropertyChanged</summary>
		internal static class Properties
		{
			public const String Token = "Token";
			public const String UsageTitle = "UsageTitle";
			public const String EditMessageOnClick = "EditMessageOnClick";
			public const String Priority = "Priority";
			public const String DefaultParseMode = "DefaultParseMode";
			public const String ReconnectTimeout = "ReconnectTimeout";
		}

		private static TimeSpan MinTimeout = new TimeSpan(0, 1, 0);

		private String _token;
		private String _usageTitle;
		private Boolean _editMessageOnClick = true;
		private String _priority;
		private Sal.ParseModeType _parseMode = Sal.ParseModeType.Default;
		private TimeSpan _reconnectTimeout = MinTimeout;

		/// <summary>The token to use to connect to Telegam API</summary>
		[Category("General")]
		[Description("The token to use to connect to Telegam API")]
		public String Token
		{
			get => this._token;
			set => this.SetField(ref this._token, value, Properties.Token);
		}

		/// <summary>Response header before displaying the command list</summary>
		[Category("UI")]
		[Description("Response header before displaying the command list")]
		public String UsageTitle
		{
			get => this._usageTitle;
			set => this.SetField(ref this._usageTitle, value, Properties.UsageTitle);
		}

		/// <summary>Action when clicking on a button in a message:\r\ntrue - Rewrite message; false - Create a new message</summary>
		[Category("UI")]
		[Description("Action when clicking on a button in a message:\r\ntrue - Rewrite message; false - Create a new message")]
		[DefaultValue(true)]
		public Boolean EditMessageOnClick
		{
			get => this._editMessageOnClick;
			set => this.SetField(ref this._editMessageOnClick, value, Properties.EditMessageOnClick);
		}

		/// <summary>Basic priority of plugins when processing a new message</summary>
		[Category("General")]
		[DisplayName("Plugin Priority")]
		[Description("Basic priority of plugins when processing a new message")]
		[Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
		public String Priority
		{
			get => this._priority;
			set => this.SetField(ref this._priority, value, Properties.Priority);
		}

		/// <summary>Default formatting used if the plugin does not specify formatting in the response</summary>
		[Category("Text")]
		[Description("Default formatting used if the plugin does not specify formatting in the response")]
		[DefaultValue(Sal.ParseModeType.Default)]
		[CLSCompliant(false)]
		public Sal.ParseModeType DefaultParseMode
		{
			get => this._parseMode;
			set => this.SetField(ref this._parseMode, value, Properties.DefaultParseMode);
		}

		/// <summary>Timeout trying to connect to server when server is unavailable</summary>
		[Category("General")]
		[DisplayName("Reconnect Timeout")]
		[Description("Timeout trying to connect to server when server is unavailable")]
		public TimeSpan ReconnectTimeout
		{
			get => this._reconnectTimeout;
			set
			{
				this.SetField(ref this._reconnectTimeout,
					value == null || value < PluginSettings.MinTimeout
						? PluginSettings.MinTimeout
						: value,
					Properties.ReconnectTimeout);
			}
		}

		#region INotifyPropertyChanged
		/// <summary>The event to notify that settings property has been changed</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		private Boolean SetField<T>(ref T field, T value, String propertyName)
		{
			if(EqualityComparer<T>.Default.Equals(field, value))
				return false;

			field = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			return true;
		}
		#endregion INotifyPropertyChanged
	}
}