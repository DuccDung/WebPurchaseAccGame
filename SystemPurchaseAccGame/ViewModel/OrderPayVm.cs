namespace SystemPurchaseAccGame.ViewModel
{
        public class ConfirmPayVm
        {
            public long AccountId { get; set; }
            public string Title { get; set; } = "";
            public long Price { get; set; }
            public long Balance { get; set; }
        }

        public class NeedTopupVm
        {
            public long AccountId { get; set; }
            public string Title { get; set; } = "";
            public long Price { get; set; }
            public long Balance { get; set; }
            public long NeedMore { get; set; }
            public string? QrImageUrl { get; set; }
            public string? TopupNote { get; set; }
        }
}
