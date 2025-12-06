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



        [Function("getplayers")]
        public async Task<HttpResponseData> GetPlayers([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getplayers")] HttpRequestData req)
        {
            var players = await _db.Players
                .Select(p => new {
                    playerId = p.PlayerId,
                    playerName = p.PlayerName,
                    fullName = p.FullName,
                    age = p.Age,
                    level = p.Level,
                    email = p.Email
                })
                .ToListAsync();

            var resp = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await resp.WriteAsJsonAsync(players);
            return resp;
        }

        // 2) GET all assets (id + assetName + description + levelRequire)
        [Function("getassets")]
        public async Task<HttpResponseData> GetAssets([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getassets")] HttpRequestData req)
        {
            var assets = await _db.Assets
                .Select(a => new {
                    assetId = a.AssetId,
                    assetName = a.AssetName,
                    description = a.Description,
                    levelRequire = a.LevelRequire
                })
                .ToListAsync();

            var resp = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await resp.WriteAsJsonAsync(assets);
            return resp;
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
        public async Task<HttpResponseData> GetAssetsByPlayer_AllPlayers([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getassetsbyplayer")] HttpRequestData req)
        {
            // Left join players with playerAssets -> asset (if any)
            var query = from p in _db.Players
                        join pa in _db.PlayerAssets on p.PlayerId equals pa.PlayerId into paGroup
                        from pag in paGroup.DefaultIfEmpty()
                        join a in _db.Assets on pag.AssetId equals a.AssetId into aGroup
                        from ag in aGroup.DefaultIfEmpty()
                        orderby p.PlayerName
                        select new
                        {
                            PlayerId = p.PlayerId,
                            PlayerName = p.PlayerName,
                            Level = p.Level,
                            Age = p.Age,
                            AssetId = ag != null ? ag.AssetId : (Guid?)null,
                            AssetName = ag != null ? ag.AssetName : null
                        };

            var result = await query.ToListAsync();

            var resp = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await resp.WriteAsJsonAsync(result);
            return resp;
        }

        // --- Add this new function to delete a player by id ---
        [Function("deleteplayer")]
        public async Task<HttpResponseData> DeletePlayer(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "deleteplayer/{id}")] HttpRequestData req,
            string id)
        {
            var logger = _logger; // use injected logger
            if (!Guid.TryParse(id, out var playerGuid))
            {
                var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badReq.WriteStringAsync("Invalid player id.");
                return badReq;
            }

            var player = await _db.Players.FindAsync(playerGuid);
            if (player == null)
            {
                var notFound = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Player not found.");
                return notFound;
            }

            // Remove related PlayerAssets first (if not cascade)
            var relations = _db.PlayerAssets.Where(pa => pa.PlayerId == playerGuid);
            _db.PlayerAssets.RemoveRange(relations);

            // Then remove player
            _db.Players.Remove(player);

            await _db.SaveChangesAsync();

            var ok = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await ok.WriteStringAsync("Player deleted.");
            return ok;
        }


     

        private class AssignDto
        {
            public Guid PlayerId { get; set; }
            public Guid AssetId { get; set; }
        }
    }
}
