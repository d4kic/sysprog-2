using System.Net;

namespace zip_server.src.Server
{
    internal class RequestQueue
    {
        private readonly Queue<HttpListenerContext> queue = new();
        private readonly object lockObj = new();
        private readonly SemaphoreSlim sem = new SemaphoreSlim(0);

        public void EnqueueRequest(HttpListenerContext context)
        {
            lock (lockObj)
            {
                queue.Enqueue(context);
            }
            sem.Release();
        }

        public async Task<HttpListenerContext?> DequeueRequestAsync(CancellationToken ctoken)
        {
            try
            {
                await sem.WaitAsync(ctoken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            lock (lockObj)
            {
                return queue.Count > 0 ? queue.Dequeue() : null;
            }
        }
    }
}
