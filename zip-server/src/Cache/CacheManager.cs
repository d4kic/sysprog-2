using System.Collections.Concurrent;

namespace zip_server.src.Cache
{
    internal static class CacheManager
    {
        private static readonly ConcurrentDictionary<string, CacheItem> cache = new();
        private static readonly TimeSpan ttl = TimeSpan.FromSeconds(30);
        private static readonly object cacheLock = new();
        private static readonly int maxItems = 100;

        public static bool TryGet(string key, out byte[] data)
        {
            data = null!;

            if (cache.TryGetValue(key, out var item))
            {
                if (DateTime.UtcNow - item.CreatedAt < ttl)
                {
                    item.LastAccessedAt = DateTime.UtcNow;
                    data = item.Data!;
                    return true;
                }
                else
                {
                    cache.TryRemove(key, out _);
                }
            }

            return false;
        }

        public static void Set(string key, byte[] data)
        {
            lock (cacheLock)
            {
                var now = DateTime.UtcNow;
                if (cache.Count >= maxItems)
                {
                    foreach (var kvp in cache)
                    {
                        if (now - kvp.Value.CreatedAt >= ttl)
                        {
                            cache.TryRemove(kvp.Key, out _);
                        }
                    }
                    var lru = cache.OrderBy(kvp => kvp.Value.LastAccessedAt).First();
                    cache.TryRemove(lru.Key, out _);
                }
                cache[key] = new CacheItem
                {
                    Data = data,
                    CreatedAt = now,
                    LastAccessedAt = now
                };
            }
        }
    }
}
