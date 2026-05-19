using System.Net;

namespace zip_server.src.Server
{
    internal class WorkerPool
    {
        private readonly int workerCount;
        private readonly RequestQueue? queue;

        public WorkerPool(RequestQueue queue, int workerCount)
        {
            this.queue = queue;
            this.workerCount = workerCount;
        }

        public void Start()
        {
            for (int i = 0; i < workerCount; i++)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    while (true)
                    {
                        HttpListenerContext? context = queue?.DequeueRequest();
                        if (context != null) 
                            RequestHandler.HandleRequest(context);
                    }
                });
            }
        }
    }
}
