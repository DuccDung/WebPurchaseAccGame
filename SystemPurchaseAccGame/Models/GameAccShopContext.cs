using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

public partial class GameAccShopContext : DbContext
{
    public GameAccShopContext()
    {
    }

    public GameAccShopContext(DbContextOptions<GameAccShopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountAttribute> AccountAttributes { get; set; }

    public virtual DbSet<AccountDelivery> AccountDeliveries { get; set; }

    public virtual DbSet<AccountListing> AccountListings { get; set; }

    public virtual DbSet<AccountMedium> AccountMedia { get; set; }

    public virtual DbSet<Game> Games { get; set; }

    public virtual DbSet<GameCategory> GameCategories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Topup> Topups { get; set; }

    public virtual DbSet<TopupBankDetail> TopupBankDetails { get; set; }

    public virtual DbSet<TopupCardDetail> TopupCardDetails { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    public virtual DbSet<vw_GameAccountStat> vw_GameAccountStats { get; set; }
    public DbSet<LuckySpinItem> LuckySpinItems => Set<LuckySpinItem>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<LuckySpinItem>(e =>
        {
            e.ToTable("LuckySpinItems");
            e.HasKey(x => x.ItemId);

            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.AccountUser).HasMaxLength(200);
            e.Property(x => x.AccountPass).HasMaxLength(200);
            e.Property(x => x.WinMessage).HasMaxLength(300);
            e.Property(x => x.Note).HasMaxLength(500);

            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.Weight).HasDefaultValue(0);
            e.Property(x => x.PrizeValue).HasDefaultValue(0);

            e.Property(x => x.RowVer).IsRowVersion();
        });

        modelBuilder.Entity<AccountAttribute>(entity =>
        {
            entity.HasKey(e => e.AttrId).HasName("PK__AccountA__0108334F926D4231");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Account).WithMany(p => p.AccountAttributes).HasConstraintName("FK_AccountAttr_Account");
        });

        modelBuilder.Entity<AccountDelivery>(entity =>
        {
            entity.HasKey(e => e.DeliveryId).HasName("PK__AccountD__626D8FCEB13F6F9F");

            entity.Property(e => e.DeliveredAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.DeliveredToUser).WithMany(p => p.AccountDeliveries)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Delivery_User");

            entity.HasOne(d => d.OrderItem).WithOne(p => p.AccountDelivery).HasConstraintName("FK_Delivery_OrderItem");
        });

        modelBuilder.Entity<AccountListing>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__AccountL__349DA5A6425E40E1");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("AVAILABLE");

            entity.HasOne(d => d.Game).WithMany(p => p.AccountListings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Account_Game");
        });

        modelBuilder.Entity<AccountMedium>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("PK__AccountM__B2C2B5CF195E121A");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Account).WithMany(p => p.AccountMedia).HasConstraintName("FK_AccountMedia_Account");
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.GameId).HasName("PK__Games__2AB897FD1C9F3E43");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Games)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Games_Category");
        });

        modelBuilder.Entity<GameCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__GameCate__19093A0BCD921DF7");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCF5E4044D1");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PaymentMethod).HasDefaultValue("WALLET");
            entity.Property(e => e.Status).HasDefaultValue("PENDING");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_User");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED06816AAF2C65");

            entity.HasOne(d => d.Account).WithOne(p => p.OrderItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Account");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems).HasConstraintName("FK_OrderItems_Order");
        });

        modelBuilder.Entity<Topup>(entity =>
        {
            entity.HasKey(e => e.TopupId).HasName("PK__Topups__81D777BB5A77CD23");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("PENDING");

            entity.HasOne(d => d.User).WithMany(p => p.Topups)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Topups_User");
        });

        modelBuilder.Entity<TopupBankDetail>(entity =>
        {
            entity.HasKey(e => e.TopupId).HasName("PK__TopupBan__81D777BB29645EC2");

            entity.Property(e => e.TopupId).ValueGeneratedNever();

            entity.HasOne(d => d.Topup).WithOne(p => p.TopupBankDetail).HasConstraintName("FK_TopupBank_Topup");
        });

        modelBuilder.Entity<TopupCardDetail>(entity =>
        {
            entity.HasKey(e => e.TopupId).HasName("PK__TopupCar__81D777BB12709646");

            entity.Property(e => e.TopupId).ValueGeneratedNever();

            entity.HasOne(d => d.Topup).WithOne(p => p.TopupCardDetail).HasConstraintName("FK_TopupCard_Topup");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C814C7A0C");

            entity.ToTable(tb => tb.HasTrigger("TR_Users_CreateWallet"));

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Role).HasDefaultValue("User");
            entity.Property(e => e.Status).HasDefaultValue((byte)1);
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__Wallets__84D4F90E0AB0C364");

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithOne(p => p.Wallet)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wallets_User");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.WalletTxId).HasName("PK__WalletTr__D00A89AE8B2A245D");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletTransactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalletTx_Wallet");
        });

        modelBuilder.Entity<vw_GameAccountStat>(entity =>
        {
            entity.ToView("vw_GameAccountStats");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
