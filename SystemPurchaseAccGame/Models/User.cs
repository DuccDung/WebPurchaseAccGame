using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Index("Username", Name = "UQ__Users__536C85E474CCF340", IsUnique = true)]
[Index("Phone", Name = "UQ__Users__5C7E359ED702A575", IsUnique = true)]
[Index("Email", Name = "UQ__Users__A9D10534DDD0ED3F", IsUnique = true)]
public partial class User
{
    [Key]
    public long UserId { get; set; }

    [StringLength(50)]
    public string Username { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(120)]
    public string? FullName { get; set; }

    [StringLength(120)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    public byte Status { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string Role { get; set; } = null!;

    [InverseProperty("DeliveredToUser")]
    public virtual ICollection<AccountDelivery> AccountDeliveries { get; set; } = new List<AccountDelivery>();

    [InverseProperty("User")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("User")]
    public virtual ICollection<Topup> Topups { get; set; } = new List<Topup>();

    [InverseProperty("User")]
    public virtual Wallet? Wallet { get; set; }
}
