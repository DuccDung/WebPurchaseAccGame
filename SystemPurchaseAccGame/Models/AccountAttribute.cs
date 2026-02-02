using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Index("AccountId", "AttrKey", Name = "IX_AccountAttr_AccountId_Key")]
public partial class AccountAttribute
{
    [Key]
    public long AttrId { get; set; }

    public long AccountId { get; set; }

    [StringLength(80)]
    public string AttrKey { get; set; } = null!;

    [StringLength(300)]
    public string AttrValue { get; set; } = null!;

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("AccountAttributes")]
    public virtual AccountListing Account { get; set; } = null!;
}
