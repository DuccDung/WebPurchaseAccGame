using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Index("OrderId", Name = "IX_OrderItems_OrderId")]
[Index("AccountId", Name = "UQ_OrderItems_Account", IsUnique = true)]
public partial class OrderItem
{
    [Key]
    public long OrderItemId { get; set; }

    public long OrderId { get; set; }

    public long AccountId { get; set; }

    public long UnitPrice { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("OrderItem")]
    public virtual AccountListing Account { get; set; } = null!;

    [InverseProperty("OrderItem")]
    public virtual AccountDelivery? AccountDelivery { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("OrderItems")]
    public virtual Order Order { get; set; } = null!;
}
