using ICSharpCode.SharpZipLib.Zip;
using System.Net;
using System.Text;
using zip_server.src.Cache;

namespace zip_server.src.Server
{
    internal class RequestHandler
    {
        private static readonly string fileDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Files");

        public static async Task HandleRequestAsync(object? obj)
        {
            HttpListenerContext context = (HttpListenerContext)obj!;

            try
            {
                string? request = context.Request.Url?.AbsolutePath.TrimStart('/');
                if (string.IsNullOrEmpty(request))
                {
                    await LogMessageAsync(context, "Primljen zahtev bez parametara.", 
                        "Nisu prosledjeni parametri zahtevu!", 400);
                    return;
                }
                Logger.Log("Primljen zahtev: " + request);

                string[] reqFiles = request.Split('&', StringSplitOptions.RemoveEmptyEntries);
                List<string> found = new List<string>();
                foreach (string filename in reqFiles)
                {
                    string safepath = Path.GetFileName(filename);
                    string filepath = Path.Combine(fileDir, safepath);
                    if (!File.Exists(filepath))
                    {
                        Logger.Log($"Zahtevani fajl {filename} ne postoji na serveru.");
                    }
                    else
                    {
                        found.Add(filepath);
                    }
                }
                if (found.Count == 0)
                {
                    await LogMessageAsync(context, "Zahtevani fajlovi ne postoje na serveru.", 
                        "Ne postoje zahtevani fajlovi!", 404);
                    return;
                }

                string cacheKey = string.Join('&', found.Select(f => Path.GetFileName(f)).OrderBy(f => f));
                if (CacheManager.TryGet(cacheKey, out byte[] cached))
                {
                    Logger.Log("Pronadjeno u cache: " + cacheKey);
                    await SendZipAsync(context, cached)
                        .ContinueWith(t =>
                        {
                            if (t.IsCompleted)
                                Logger.Log("Zip fajl poslat.\n");
                        });
                    return;
                }

                await ZipFilesAsync(found)
                    .ContinueWith(t =>
                    {
                        byte[] zipData = t.Result;

                        lock (StampedeLock.Get(cacheKey))
                        {
                            if (!CacheManager.TryGet(cacheKey, out _))
                            {
                                CacheManager.Set(cacheKey, zipData);
                                Logger.Log($"{cacheKey} kesiran.");
                            }
                        }

                        return SendZipAsync(context, zipData)
                            .ContinueWith(zipTask =>
                            {
                                if (zipTask.IsCompleted)
                                    Logger.Log("Zip fajl poslat.\n");
                            }, TaskScheduler.Default);
                    }, TaskScheduler.Default).Unwrap();
            }
            catch (Exception ex)
            {
                await LogMessageAsync(context, "Error: " + ex.Message, "Error: " + ex.Message, 500);
            }
        }

        static async Task<byte[]> ZipFilesAsync(List<string> found)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ZipOutputStream zips = new ZipOutputStream(ms))
                {
                    foreach (string filename in found)
                    {
                        using (FileStream fs = File.OpenRead(filename))
                        {
                            ZipEntry entry = new ZipEntry(Path.GetFileName(filename));
                            zips.PutNextEntry(entry);
                            await fs.CopyToAsync(zips);
                            zips.CloseEntry();
                            Logger.Log("Zipovan fajl: " + filename);
                        }
                    }
                    zips.Finish();
                }
                return ms.ToArray();
            }
        }

        static async Task SendZipAsync(HttpListenerContext context, byte[] data)
        {
            context.Response.AddHeader("Content-Disposition", "attachment; filename=files.zip");
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/zip";
            context.Response.ContentLength64 = data.Length;
            await context.Response.OutputStream.WriteAsync(data, 0, data.Length);
            context.Response.Close();
        }

        static async Task SendTextAsync(HttpListenerContext context, string text, int statusCode)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        static async Task LogMessageAsync(HttpListenerContext context, string logText, string sendText, int statusCode)
        {
            Logger.Log(logText);
            await SendTextAsync(context, sendText, statusCode);
        }
    }
}
