using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Index("GameId", "Status", Name = "IX_Account_GameId_Status")]
public partial class AccountListing
{
    [Key]
    public long AccountId { get; set; }

    public int GameId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    public long Price { get; set; }

    public string? Description { get; set; }

    public string? LoginInfo { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Account")]
    public virtual ICollection<AccountAttribute> AccountAttributes { get; set; } = new List<AccountAttribute>();

    [InverseProperty("Account")]
    public virtual ICollection<AccountMedium> AccountMedia { get; set; } = new List<AccountMedium>();

    [ForeignKey("GameId")]
    [InverseProperty("AccountListings")]
    public virtual Game Game { get; set; } = null!;

    [InverseProperty("Account")]
    public virtual OrderItem? OrderItem { get; set; }
}
