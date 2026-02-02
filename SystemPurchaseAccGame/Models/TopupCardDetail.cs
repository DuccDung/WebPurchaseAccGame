using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

public partial class TopupCardDetail
{
    [Key]
    public long TopupId { get; set; }

    [StringLength(30)]
    public string CardType { get; set; } = null!;

    public long Denomination { get; set; }

    [StringLength(80)]
    public string CardSerial { get; set; } = null!;

    [StringLength(80)]
    public string CardPin { get; set; } = null!;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? DiscountPercent { get; set; }

    [ForeignKey("TopupId")]
    [InverseProperty("TopupCardDetail")]
    public virtual Topup Topup { get; set; } = null!;
}
