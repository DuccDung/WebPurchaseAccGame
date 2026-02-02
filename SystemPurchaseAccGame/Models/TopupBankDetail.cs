using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

public partial class TopupBankDetail
{
    [Key]
    public long TopupId { get; set; }

    [StringLength(80)]
    public string BankName { get; set; } = null!;

    [StringLength(80)]
    public string? BankAccount { get; set; }

    [StringLength(120)]
    public string? BankOwner { get; set; }

    [StringLength(200)]
    public string? TransferContent { get; set; }

    [ForeignKey("TopupId")]
    [InverseProperty("TopupBankDetail")]
    public virtual Topup Topup { get; set; } = null!;
}
