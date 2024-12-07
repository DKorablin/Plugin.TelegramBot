using System;

namespace Plugin.TelegramBot.Plugins
{
	internal enum LifeCycle
	{
		/// <summary>Один экземпляр на процесс</summary>
		/// <remarks>.ctor()</remarks>
		Singleton,
		/// <summary>Один экземпляр на чат</summary>
		/// <remarks>.ctor(Int64 chatId)</remarks>
		ChatSingleton,
		/// <summary>Один экземпляр на пользователя</summary>
		/// <remarks>.ctor(Int32 userId)</remarks>
		UserSingleton,
		/// <summary>Создание объекта при каждом запросе</summary>
		/// <remarks>.ctor(Message message)</remarks>
		Transient,
	}
}