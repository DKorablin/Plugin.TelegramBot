using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugin.TelegramBot
{
	internal static class EnumerableExtension
	{
		public static Boolean AnyWithoutNull<T>(this IEnumerable<T> source)
			=> source != null && source.Any();
	}
}