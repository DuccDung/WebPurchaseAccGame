using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Index("Status", Name = "IX_Topups_Status")]
[Index("UserId", "CreatedAt", Name = "IX_Topups_UserId_CreatedAt", IsDescending = new[] { false, true })]
public partial class Topup
{
    [Key]
    public long TopupId { get; set; }

    public long UserId { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string Method { get; set; } = null!;

    public long Amount { get; set; }

    public long Fee { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [StringLength(50)]
    public string? Provider { get; set; }

    [StringLength(120)]
    public string? ReferenceCode { get; set; }

    public string? RawPayload { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [Precision(0)]
    public DateTime? CompletedAt { get; set; }

    [InverseProperty("Topup")]
    public virtual TopupBankDetail? TopupBankDetail { get; set; }

    [InverseProperty("Topup")]
    public virtual TopupCardDetail? TopupCardDetail { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Topups")]
    public virtual User User { get; set; } = null!;
}
