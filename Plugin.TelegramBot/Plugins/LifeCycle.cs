namespace Plugin.TelegramBot.Plugins
{
	internal enum LifeCycle
	{
		/// <summary>Single instance for entire process</summary>
		/// <remarks>.ctor()</remarks>
		Singleton,
		/// <summary>Single instance for a chat</summary>
		/// <remarks>.ctor(Int64 chatId)</remarks>
		ChatSingleton,
		/// <summary>Single instance for each user</summary>
		/// <remarks>.ctor(Int32 userId)</remarks>
		UserSingleton,
		/// <summary>Create instance for each request</summary>
		/// <remarks>.ctor(Message message)</remarks>
		Transient,
	}
}