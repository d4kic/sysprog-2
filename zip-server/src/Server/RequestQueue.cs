using System.Net;

namespace zip_server.src.Server
{
    internal class RequestQueue
    {
        private readonly Queue<HttpListenerContext> queue = new();
        private readonly object lockObj = new();

        public void EnqueueRequest(HttpListenerContext context)
        {
            lock (lockObj)
            {
                queue.Enqueue(context);
                Monitor.Pulse(lockObj);
            }
        }

        public HttpListenerContext DequeueRequest()
        {
            lock(lockObj)
            {
                while (queue.Count == 0)
                    Monitor.Wait(lockObj);
                return queue.Dequeue();
            }
        }
    }
}
