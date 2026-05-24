using System.Net;

namespace zip_server.src.Server
{
    internal class WorkerPool
    {
        private readonly int workerCount;
        private readonly RequestQueue? queue;
        private readonly List<Task> workers = new();
        private readonly CancellationToken ctoken;

        public WorkerPool(RequestQueue queue, int workerCount, CancellationToken ctoken)
        {
            this.queue = queue;
            this.workerCount = workerCount;
            this.ctoken = ctoken;
        }

        public void Start()
        {
            for (int i = 0; i < workerCount; i++)
            {
                int id = i;
                Task worker = Task.Factory.StartNew(() => WorkerJob(id), ctoken, 
                    TaskCreationOptions.LongRunning, TaskScheduler.Default);
                workers.Add(worker);
            }
        }

        private async Task WorkerJob(int id)
        {
            while (!ctoken.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext? context = await queue!.DequeueRequestAsync(ctoken);
                    if (context == null)
                        continue;
                    await Task.Run(() => RequestHandler.HandleRequest(context), ctoken)
                        .ContinueWith(t =>
                        {
                            if (t.IsCanceled)
                                Logger.Log($"Worker {id} ugasen.");
                        });
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public void WaitAll()
        {
            Task.WaitAll(workers.ToArray());
        }
    }
}
