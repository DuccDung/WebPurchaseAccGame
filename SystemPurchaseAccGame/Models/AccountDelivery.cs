using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Index("OrderItemId", Name = "UQ__AccountD__57ED068033ABF6D9", IsUnique = true)]
public partial class AccountDelivery
{
    [Key]
    public long DeliveryId { get; set; }

    public long OrderItemId { get; set; }

    public long DeliveredToUserId { get; set; }

    [Precision(0)]
    public DateTime DeliveredAt { get; set; }

    public string? LoginInfoSnapshot { get; set; }

    [ForeignKey("DeliveredToUserId")]
    [InverseProperty("AccountDeliveries")]
    public virtual User DeliveredToUser { get; set; } = null!;

    [ForeignKey("OrderItemId")]
    [InverseProperty("AccountDelivery")]
    public virtual OrderItem OrderItem { get; set; } = null!;
}
