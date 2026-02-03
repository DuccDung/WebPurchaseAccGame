namespace SystemPurchaseAccGame.ViewModel
{
    public class AdminTopupVm
    {
        public long TopupId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        public string Method { get; set; } = "";     // "Card" / "BANK"
        public long Amount { get; set; }
        public string Status { get; set; } = "";     // "PENDING" / "APPROVED" / "REJECTED"
        public string? ReferenceCode { get; set; }
        public string? RawPayload { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? Provider { get; set; }  // card provider

    }
}
