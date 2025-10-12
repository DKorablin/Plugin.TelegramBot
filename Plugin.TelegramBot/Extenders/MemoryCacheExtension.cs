using System;
using System.Runtime.Caching;
using System.Collections.Generic;

namespace Plugin.TelegramBot
{
	/// <summary>Extension for local caching</summary>
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
				=> (this._slidingExpiration.HasValue ? this._slidingExpiration.Value.GetHashCode() : 0)
					^ (this._absoluteExpiration.HasValue ? this._absoluteExpiration.Value.GetHashCode() : 0);
		}

		private static readonly Object _policyLock = new Object();
		private static readonly Dictionary<ExpirationPolicy, CacheItemPolicy> _policy = new Dictionary<ExpirationPolicy, CacheItemPolicy>();

		/// <summary>Get object from cache; if it does not exist create it by invoking method.</summary>
		/// <typeparam name="T">Type of cached object</typeparam>
		/// <param name="cache">Cache instance</param>
		/// <param name="key">The cache key identifier.</param>
		/// <param name="slidingExpiration">Sliding expiration</param>
		/// <param name="absoluteExpiration">Absolute expiration</param>
		/// <param name="method">Method invoked if cache entry does not exist</param>
		/// <returns>Object from cache</returns>
		public static T GetFromCache<T>(this MemoryCache cache, String key, TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration, Func<T> method)
		{
			Object result = cache.Get(key);//Because the cache may store a struct
			if(result == null && method != null)
			{
				result = method();
				cache.InsertToCache(key, slidingExpiration, absoluteExpiration, (T)result);
			}
			return (T)result;
		}

		/// <summary>Insert value into cache with specified expiration policy.</summary>
		/// <typeparam name="T">Type of cached object</typeparam>
		/// <param name="cache">Cache instance</param>
		/// <param name="key">Key</param>
		/// <param name="slidingExpiration">Sliding expiration</param>
		/// <param name="absoluteExpiration">Absolute expiration</param>
		/// <param name="value">Value to put into cache</param>
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
				throw new ArgumentException($"{nameof(slidingExpiration)} or {nameof(absoluteExpiration)} should be not null");

			ExpirationPolicy expPolicy = new ExpirationPolicy(slidingExpiration, absoluteExpiration);
			if(!_policy.TryGetValue(expPolicy, out CacheItemPolicy result))
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