    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    namespace SystemPurchaseAccGame.Models;

    [Index("UserId", "CreatedAt", Name = "IX_Orders_UserId_CreatedAt", IsDescending = new[] { false, true })]
    public partial class Order
    {
        [Key]
        public long OrderId { get; set; }

        public long UserId { get; set; }

        public long TotalAmount { get; set; }

        [StringLength(20)]
        [Unicode(false)]
        public string Status { get; set; } = null!;

        [StringLength(10)]
        [Unicode(false)]
        public string PaymentMethod { get; set; } = null!;

        [Precision(0)]
        public DateTime CreatedAt { get; set; }

        [Precision(0)]
        public DateTime? PaidAt { get; set; }

        [StringLength(300)]
        public string? Note { get; set; }

        [InverseProperty("Order")]
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        [ForeignKey("UserId")]
        [InverseProperty("Orders")]
        public virtual User User { get; set; } = null!;
    }
