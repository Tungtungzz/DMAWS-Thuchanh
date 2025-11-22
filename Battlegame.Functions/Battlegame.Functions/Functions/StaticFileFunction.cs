using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Battlegame.Functions.Functions
{
    public class StaticFileFunction
    {
        private readonly ILogger _logger;

        public StaticFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StaticFileFunction>();
        }

        [Function("IndexHtml")]
        public async Task<HttpResponseData> GetIndexHtml([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "index.html")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                // Candidate paths to search for wwwroot/index.html
                var cwd = Directory.GetCurrentDirectory();
                var candidates = new[]
                {
                    Path.Combine(cwd, "wwwroot", "index.html"),
                    Path.Combine(cwd, "wwwroot", "index.htm"),
                    Path.Combine(cwd, "..", "wwwroot", "index.html"),
                    Path.Combine(cwd, "..", "..", "wwwroot", "index.html"),
                    Path.Combine(cwd, "bin", "Debug", "net10.0", "wwwroot", "index.html"),
                    Path.Combine(cwd, "bin", "Debug", "net10.0", "wwwroot", "index.htm"),
                    Path.Combine(cwd, "bin", "Debug", "net10.0", "wwwroot", "index.html".Replace("net10.0", Environment.Version.Major + "." + Environment.Version.Minor)),
                    Path.Combine(AppContext.BaseDirectory ?? cwd, "wwwroot", "index.html"),
                    Path.Combine(AppContext.BaseDirectory ?? cwd, "..", "wwwroot", "index.html")
                }.Select(p => Path.GetFullPath(p)).Distinct().ToList();

                _logger.LogInformation("StaticFileFunction - CurrentDirectory: {cwd}", cwd);
                _logger.LogInformation("StaticFileFunction - AppContextBaseDirectory: {baseDir}", AppContext.BaseDirectory);
                _logger.LogInformation("StaticFileFunction - Candidates: {candidates}", string.Join(" | ", candidates));

                // find first existing
                var found = candidates.FirstOrDefault(File.Exists);

                if (found == null)
                {
                    // Return 404 with helpful debug info in body
                    response.StatusCode = System.Net.HttpStatusCode.NotFound;
                    var msg = "index.html not found. Looked in these locations:\n" +
                              string.Join("\n", candidates) + "\n\n" +
                              "CurrentDirectory: " + cwd + "\n" +
                              "AppContext.BaseDirectory: " + AppContext.BaseDirectory + "\n";
                    await response.WriteStringAsync(msg);
                    _logger.LogWarning(msg);
                    return response;
                }

                // Serve the file
                var stream = File.OpenRead(found);
                response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                response.StatusCode = System.Net.HttpStatusCode.OK;
                await stream.CopyToAsync(response.Body);
                stream.Dispose();

                _logger.LogInformation("Served index.html from {found}", found);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving index.html");
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Server error while serving index.html: " + ex.Message);
                return response;
            }
        }
    }
}
