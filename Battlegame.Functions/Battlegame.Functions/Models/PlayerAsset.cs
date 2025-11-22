using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Battlegame.Functions.Models
{
    public class PlayerAsset
    {
        [Key]
        public Guid PlayerAssetId { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Player))]
        public Guid PlayerId { get; set; }
        public Player? Player { get; set; }

        [ForeignKey(nameof(Asset))]
        public Guid AssetId { get; set; }
        public Asset? Asset { get; set; }

        public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
    }
}
