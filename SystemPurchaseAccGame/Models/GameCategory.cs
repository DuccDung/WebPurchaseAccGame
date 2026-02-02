using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Index("Name", Name = "UQ__GameCate__737584F651CB65E3", IsUnique = true)]
[Index("Slug", Name = "UQ__GameCate__BC7B5FB6CB45277D", IsUnique = true)]
public partial class GameCategory
{
    [Key]
    public int CategoryId { get; set; }

    [StringLength(80)]
    public string Name { get; set; } = null!;

    [StringLength(120)]
    public string Slug { get; set; } = null!;

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Category")]
    public virtual ICollection<Game> Games { get; set; } = new List<Game>();
}
