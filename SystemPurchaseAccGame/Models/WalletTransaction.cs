using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Index("WalletId", "CreatedAt", Name = "IX_WalletTx_WalletId_CreatedAt", IsDescending = new[] { false, true })]
public partial class WalletTransaction
{
    [Key]
    public long WalletTxId { get; set; }

    public long WalletId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string TxType { get; set; } = null!;

    public long Amount { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? RefTable { get; set; }

    public long? RefId { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("WalletId")]
    [InverseProperty("WalletTransactions")]
    public virtual Wallet Wallet { get; set; } = null!;
}
