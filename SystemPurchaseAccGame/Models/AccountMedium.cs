using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Index("AccountId", "SortOrder", Name = "IX_AccountMedia_AccountId")]
public partial class AccountMedium
{
    [Key]
    public long MediaId { get; set; }

    public long AccountId { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string MediaType { get; set; } = null!;

    [StringLength(600)]
    public string Url { get; set; } = null!;

    public int SortOrder { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("AccountMedia")]
    public virtual AccountListing Account { get; set; } = null!;
}
