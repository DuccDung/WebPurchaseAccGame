using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Index("UserId", Name = "UQ__Wallets__1788CC4D7C174AD4", IsUnique = true)]
public partial class Wallet
{
    [Key]
    public long WalletId { get; set; }

    public long UserId { get; set; }

    public long Balance { get; set; }

    [Precision(0)]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Wallet")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("Wallet")]
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
