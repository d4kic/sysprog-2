using ICSharpCode.SharpZipLib.Zip;
using System.Net;
using System.Text;
using zip_server.src.Cache;

namespace zip_server.src.Server
{
    internal class RequestHandler
    {
        private static readonly string fileDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Files");

        public static void HandleRequest(object? obj)
        {
            HttpListenerContext context = (HttpListenerContext)obj!;

            try
            {
                string? request = context.Request.Url?.AbsolutePath.TrimStart('/');
                if (string.IsNullOrEmpty(request))
                {
                    LogMessage(context, "Primljen zahtev bez parametara.", "Nisu prosledjeni parametri zahtevu!", 400);
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
                        continue;
                    }
                    else
                    {
                        found.Add(filepath);
                    }
                }
                if (found.Count == 0)
                {
                    LogMessage(context, "Zahtevani fajlovi ne postoje na serveru.", "Ne postoje zahtevani fajlovi!", 404);
                    return;
                }

                string cacheKey = string.Join('&', found.Select(f => Path.GetFileName(f)).OrderBy(f => f));
                if (CacheManager.TryGet(cacheKey, out byte[] cached))
                {
                    Logger.Log("Pronadjeno u cache: " + cacheKey);
                    SendZip(context, cached);
                    Logger.Log("Zip fajl poslat.\n");
                    return;
                }

                lock (StampedeLock.Get(cacheKey))
                {
                    if (CacheManager.TryGet(cacheKey, out byte[] cachedIn))
                    {
                        Logger.Log("Pronadjeno u cache: " + cacheKey);
                        SendZip(context, cachedIn);
                        Logger.Log("Zip fajl poslat.\n");
                        return;
                    }
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
                                    fs.CopyTo(zips);
                                    zips.CloseEntry();
                                    Logger.Log("Zipovan fajl: " + filename);
                                }
                            }
                            zips.Finish();
                        }

                        byte[] zipData = ms.ToArray();
                        CacheManager.Set(cacheKey, zipData);
                        Logger.Log($"{cacheKey} kesiran.");
                        SendZip(context, zipData);
                        Logger.Log("Zip fajl poslat.\n");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(context, "Error: " + ex.Message, "Error: " + ex.Message, 500);
            }
        }

        static void SendZip(HttpListenerContext context, byte[] data)
        {
            context.Response.AddHeader("Content-Disposition", "attachment; filename=files.zip");
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/zip";
            context.Response.ContentLength64 = data.Length;
            context.Response.OutputStream.Write(data, 0, data.Length);
            context.Response.Close();
        }

        static void SendText(HttpListenerContext context, string text, int statusCode)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        static void LogMessage(HttpListenerContext context, string logText, string sendText, int statusCode)
        {
            Logger.Log(logText);
            SendText(context, sendText, statusCode);
        }
    }
}
