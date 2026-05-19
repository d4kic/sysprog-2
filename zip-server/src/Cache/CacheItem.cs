namespace zip_server.src.Cache
{
    internal class CacheItem
    {
        public byte[]? Data { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
    }
}
