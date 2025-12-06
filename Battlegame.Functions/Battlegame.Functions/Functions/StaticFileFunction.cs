// File: Functions/StaticFileFunction.cs
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

        // Serve index.html for GET /index.html (works when routePrefix is "" => /index.html,
        // or when routePrefix is "api" => /api/index.html)
        [Function("IndexHtml")]
        public Task<HttpResponseData> IndexHtml([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "index.html")] HttpRequestData req)
            => ServeIndex(req);

        // Serve index.html for GET /  (root). When routePrefix is "api", this will be /api/
        [Function("Root")]
        public Task<HttpResponseData> Root([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "")] HttpRequestData req)
            => ServeIndex(req);

        // Optional: also provide explicit API path (works if routePrefix empty => /api/index.html,
        // if routePrefix "api" => /api/api/index.html - harmless but usually unnecessary)
        [Function("ApiIndexHtml")]
        public Task<HttpResponseData> ApiIndexHtml([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/index.html")] HttpRequestData req)
            => ServeIndex(req);

        private async Task<HttpResponseData> ServeIndex(HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var cwd = Directory.GetCurrentDirectory();
                var appBase = AppContext.BaseDirectory ?? cwd;

                var candidates = new[]
                {
                    Path.Combine(cwd, "wwwroot", "index.html"),
                    Path.Combine(cwd, "wwwroot", "index.htm"),
                    Path.Combine(appBase, "wwwroot", "index.html"),
                    Path.Combine(appBase, "wwwroot", "index.htm"),
                    Path.Combine(cwd, "..", "wwwroot", "index.html"),
                    Path.Combine(cwd, "..", "..", "wwwroot", "index.html"),
                    Path.Combine(cwd, "bin", "Debug", "net10.0", "wwwroot", "index.html"),
                    Path.Combine(cwd, "bin", "Debug", "net10.0", "wwwroot", "index.htm")
                }.Select(p => Path.GetFullPath(p)).Distinct().ToList();

                _logger.LogInformation("StaticFileFunction - CurrentDirectory: {cwd}", cwd);
                _logger.LogInformation("StaticFileFunction - AppContextBaseDirectory: {baseDir}", appBase);
                _logger.LogInformation("StaticFileFunction - Candidate paths: {candidates}", string.Join(" | ", candidates));

                var found = candidates.FirstOrDefault(File.Exists);

                if (found == null)
                {
                    // helpful debug response (only for local/testing)
                    response.StatusCode = System.Net.HttpStatusCode.NotFound;
                    var msg = "index.html not found. Searched locations:\n" +
                              string.Join("\n", candidates) + "\n\n" +
                              "CurrentDirectory: " + cwd + "\n" +
                              "AppContext.BaseDirectory: " + appBase + "\n";
                    await response.WriteStringAsync(msg);
                    _logger.LogWarning(msg);
                    return response;
                }

                // serve file
                var fs = File.OpenRead(found);
                response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                response.StatusCode = System.Net.HttpStatusCode.OK;
                await fs.CopyToAsync(response.Body);
                fs.Dispose();

                _logger.LogInformation("Served index.html from {path}", found);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving index.html");
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Server error while serving index.html: {ex.Message}");
                return response;
            }
        }
    }
}
