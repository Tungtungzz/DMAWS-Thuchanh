using System;
using System.ComponentModel.DataAnnotations;

namespace Battlegame.Functions.Models
{
    public class Asset
    {
        [Key]
        public Guid AssetId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(128)]
        public string AssetName { get; set; } = null!;

        public string? Description { get; set; }

        public int LevelRequire { get; set; }
    }
}
