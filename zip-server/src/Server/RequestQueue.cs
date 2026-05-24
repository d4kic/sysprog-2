using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
            await sem.WaitAsync(ctoken);

            if (ctoken.IsCancellationRequested)
                return null;

            lock (lockObj)
            {
                return queue.Count > 0 ? queue.Dequeue() : null;
            }
        }
    }
}
