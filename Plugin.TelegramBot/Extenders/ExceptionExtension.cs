using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Plugin.TelegramBot
{
	internal static class ExceptionExtension
	{
		/// <summary>Fatal error that should not be processed</summary>
		/// <param name="exception">Error to check</param>
		/// <returns>The error is fatal and there is no point in handling it</returns>
		public static Boolean IsFatal(this Exception exception)
		{
			while(exception != null)
			{
				if((exception is OutOfMemoryException && !(exception is InsufficientMemoryException))//No point in allocating more memory
					|| exception is ThreadAbortException//Error occurs during redirect from one page to another
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