using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Battlegame.Functions.Data;
using Battlegame.Functions.Models;

namespace Battlegame.Functions.Functions
{
    public class PlayerFunctions
    {
        private readonly AppDbContext _db;
        private readonly ILogger _logger;

        public PlayerFunctions(AppDbContext db, ILoggerFactory loggerFactory)
        {
            _db = db;
            _logger = loggerFactory.CreateLogger<PlayerFunctions>();
        }

        [Function("registerplayer")]
        public async Task<HttpResponseData> RegisterPlayer([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "registerplayer")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonSerializer.Deserialize<Player>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null)
            {
                var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid payload");
                return bad;
            }

            // ensure new guid if not provided
            if (dto.PlayerId == Guid.Empty) dto.PlayerId = Guid.NewGuid();

            _db.Players.Add(dto);
            await _db.SaveChangesAsync();

            var ok = req.CreateResponse(System.Net.HttpStatusCode.Created);
            await ok.WriteAsJsonAsync(dto);
            return ok;
        }

        [Function("createasset")]
        public async Task<HttpResponseData> CreateAsset([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "createasset")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonSerializer.Deserialize<Asset>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null)
            {
                var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid payload");
                return bad;
            }

            if (dto.AssetId == Guid.Empty) dto.AssetId = Guid.NewGuid();

            _db.Assets.Add(dto);
            await _db.SaveChangesAsync();

            var ok = req.CreateResponse(System.Net.HttpStatusCode.Created);
            await ok.WriteAsJsonAsync(dto);
            return ok;
        }

        [Function("assignasset")]
        public async Task<HttpResponseData> AssignAsset([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "assignasset")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonSerializer.Deserialize<AssignDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null || dto.PlayerId == Guid.Empty || dto.AssetId == Guid.Empty)
            {
                var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid payload. Required: playerId, assetId (GUID).");
                return bad;
            }

            var player = await _db.Players.FindAsync(dto.PlayerId);
            var asset = await _db.Assets.FindAsync(dto.AssetId);
            if (player == null || asset == null)
            {
                var notFound = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Player or Asset not found.");
                return notFound;
            }

            var pa = new PlayerAsset { PlayerId = player.PlayerId, AssetId = asset.AssetId, AcquiredAt = DateTime.UtcNow };
            _db.PlayerAssets.Add(pa);
            await _db.SaveChangesAsync();

            var ok = req.CreateResponse(System.Net.HttpStatusCode.Created);
            await ok.WriteAsJsonAsync(pa);
            return ok;
        }

        [Function("getassetsbyplayer")]
        public async Task<HttpResponseData> GetAssetsByPlayer([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getassetsbyplayer")] HttpRequestData req)
        {
            var list = await _db.PlayerAssets
                .Include(pa => pa.Player)
                .Include(pa => pa.Asset)
                .OrderBy(pa => pa.AcquiredAt)
                .ToListAsync();

            var result = list.Select((pa, idx) => new {
                No = idx + 1,
                PlayerName = pa.Player?.PlayerName,
                Level = pa.Player?.Level,
                Age = pa.Player?.Age,
                AssetName = pa.Asset?.AssetName,
                AcquiredAt = pa.AcquiredAt
            }).ToList();

            var resp = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await resp.WriteAsJsonAsync(result);
            return resp;
        }

        private class AssignDto
        {
            public Guid PlayerId { get; set; }
            public Guid AssetId { get; set; }
        }
    }
}
