using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Plugin.TelegramBot
{
	internal static class ExceptionExtension
	{
		/// <summary>Фатальная ошибка, которую обрабатывать не надо</summary>
		/// <param name="exception">Ошибка для проверки</param>
		/// <returns>Ошибка фатальная и обрабатывать нет смысла</returns>
		public static Boolean IsFatal(this Exception exception)
		{
			while(exception != null)
			{
				if((exception is OutOfMemoryException && !(exception is InsufficientMemoryException))//Нет смысла занимать больше памяти
					|| exception is ThreadAbortException//Ошибка происходит при редиректе с одной страницы на другую
					|| exception is AccessViolationException
					|| exception is SEHException)
					return true;
				if(!(exception is TypeInitializationException) && !(exception is TargetInvocationException))
					break;
				exception = exception.InnerException;
			}
			return false;
		}
	}
}