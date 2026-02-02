using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Index("CategoryId", Name = "IX_Games_CategoryId")]
[Index("Slug", Name = "UQ__Games__BC7B5FB69F30015E", IsUnique = true)]
public partial class Game
{
    [Key]
    public int GameId { get; set; }

    public int CategoryId { get; set; }

    [StringLength(120)]
    public string Name { get; set; } = null!;

    [StringLength(160)]
    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    public bool IsActive { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Game")]
    public virtual ICollection<AccountListing> AccountListings { get; set; } = new List<AccountListing>();

    [ForeignKey("CategoryId")]
    [InverseProperty("Games")]
    public virtual GameCategory Category { get; set; } = null!;
}
