using System;
using System.Runtime.Caching;
using System.Collections.Generic;

namespace Plugin.TelegramBot
{
	/// <summary>Расширение для локального кеширования</summary>
	internal static class MemoryCacheExtension
	{
		private struct ExpirationPolicy
		{
			public readonly TimeSpan? _slidingExpiration;
			public readonly DateTimeOffset? _absoluteExpiration;
			public ExpirationPolicy(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
			{
				this._slidingExpiration = slidingExpiration;
				this._absoluteExpiration = absoluteExpiration;
			}
			public override Int32 GetHashCode()
			{
				return (this._slidingExpiration.HasValue ? this._slidingExpiration.Value.GetHashCode() : 0)
					^ (this._absoluteExpiration.HasValue ? this._absoluteExpiration.Value.GetHashCode() : 0);
			}
		}
		private static Object _policyLock = new Object();
		private static Dictionary<ExpirationPolicy, CacheItemPolicy> _policy = new Dictionary<ExpirationPolicy, CacheItemPolicy>();

		/// <summary>Получить из кеша объект. Если объекта нет, то создать объект выполнив метод</summary>
		/// <typeparam name="T">Тип объекта в кеше</typeparam>
		/// <param name="cache">Объект кеширования</param>
		/// <param name="key">Ключ</param>
		/// <param name="slidingExpiration">Скользящий срок</param>
		/// <param name="absoluteExpiration">Абсолютный срок</param>
		/// <param name="method">Вызываемый метод, если в кеше ничего нет</param>
		/// <returns>Объект из кеша</returns>
		public static T GetFromCache<T>(this MemoryCache cache, String key, TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration, Func<T> method)
		{
			Object result = cache.Get(key);//Т.к. в кеше может лежать структура
			if(result == null && method != null)
			{
				result = method();
				cache.InsertToCache(key, slidingExpiration, absoluteExpiration, (T)result);
			}
			return (T)result;
		}

		/// <summary>banana banana banana</summary>
		/// <typeparam name="T">Тип объекта в кеше</typeparam>
		/// <param name="cache">Объект кеширования</param>
		/// <param name="key">Ключ</param>
		/// <param name="slidingExpiration">Скользящий срок</param>
		/// <param name="absoluteExpiration">Абсолютный срок</param>
		/// <param name="value">Значение, которое необходимо положить в кеш</param>
		public static void InsertToCache<T>(this MemoryCache cache, String key, TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration, T value)
		{
			if(value == null)
				cache.Remove(key);
			else
			{
				/*DateTimeOffset? offset=null;
				if(absoluteExpiration.HasValue)
					offset = DateTime.Now + absoluteExpiration.Value;*/

				CacheItemPolicy policy = MemoryCacheExtension.GetPolicy(slidingExpiration, absoluteExpiration);
				cache.Set(key, value, policy);
			}
		}
		private static CacheItemPolicy GetPolicy(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
		{
			if(slidingExpiration == null && absoluteExpiration == null)
				throw new ArgumentNullException();

			CacheItemPolicy result;
			ExpirationPolicy expPolicy = new ExpirationPolicy(slidingExpiration, absoluteExpiration);
			if(!_policy.TryGetValue(expPolicy, out result))
			{
				lock(_policyLock)
				{
					if(!_policy.TryGetValue(expPolicy, out result))
					{
						result = new CacheItemPolicy();

						if(expPolicy._slidingExpiration.HasValue)
							result.SlidingExpiration = expPolicy._slidingExpiration.Value;
						else if(expPolicy._absoluteExpiration.HasValue)
							result.AbsoluteExpiration = expPolicy._absoluteExpiration.Value;
						//result.RemovedCallback = new CacheEntryRemovedCallback(RemovedFromCacheCallback);
						_policy.Add(expPolicy, result);
					}
				}
			}
			return result;
		}
	}
}