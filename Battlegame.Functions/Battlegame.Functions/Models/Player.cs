using System;
using System.ComponentModel.DataAnnotations;

namespace Battlegame.Functions.Models
{
    public class Player
    {
        [Key]
        public Guid PlayerId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(64)]
        public string PlayerName { get; set; } = null!;

        [MaxLength(128)]
        public string? FullName { get; set; }

        public int Age { get; set; }

        public int Level { get; set; }

        [MaxLength(256)]
        public string? Email { get; set; }
    }
}
