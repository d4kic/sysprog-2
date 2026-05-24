using System.Net;

namespace zip_server.src.Server
{
    internal class WebServer
    {
        private readonly HttpListener listener = new();
        private readonly RequestQueue queue = new();
        private readonly WorkerPool pool;
        private readonly CancellationTokenSource cts = new();

        public WebServer(int workerCount)
        {
            listener.Prefixes.Add("http://localhost:5050/");
            pool = new WorkerPool(queue, workerCount, cts.Token);
        }

        public async Task StartAsync()
        {
            listener.Start();
            pool.Start();
            Console.WriteLine($"[{DateTime.Now}] Server radi na http://localhost:5050/");
            Console.WriteLine("Dugme Z za shutdown...");

            new Thread(() =>
            {
                while (Console.ReadKey(true).Key != ConsoleKey.Z) { }
                Stop();
            })
            {
                IsBackground = true
            }.Start();

            while (true)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    queue.EnqueueRequest(context);
                }
                catch
                {
                    break;
                }
            }
        }

        private void Stop()
        {
            cts.Cancel();
            pool.WaitAll();
            listener.Stop();
            Logger.Log("Server uspesno ugasen.");
        }
    }
}
