namespace SystemPurchaseAccGame.Models
{
    public class LuckySpinItem
    {
        public long ItemId { get; set; }
        public string Title { get; set; } = "";

        public int PrizeTier { get; set; }       // 1/10/100
        public long PrizeValue { get; set; }     // 1000/10000/100000

        public string? AccountUser { get; set; }
        public string? AccountPass { get; set; }

        public int Weight { get; set; }          // tỉ lệ trúng
        public int? Remaining { get; set; }      // null = unlimited
        public bool IsActive { get; set; } = true;

        public string? WinMessage { get; set; }
        public string? Note { get; set; }

        public long? LastWinnerUserId { get; set; }
        public DateTime? LastWonAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public byte[] RowVer { get; set; } = Array.Empty<byte>();
    }
}
