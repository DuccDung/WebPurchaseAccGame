using System.Collections.Generic;

namespace SystemPurchaseAccGame.ViewModel
{
    public class ConfirmPayMultiVm
    {
        public int GameId { get; set; }
        public string GameName { get; set; } = "";
        public int Quantity { get; set; }
        public long UnitPrice { get; set; }
        public long TotalPrice { get; set; }
        public long Balance { get; set; }
    }

    public class NeedTopupMultiVm
    {
        public int GameId { get; set; }
        public string GameName { get; set; } = "";
        public int Quantity { get; set; }
        public long UnitPrice { get; set; }
        public long TotalPrice { get; set; }
        public long Balance { get; set; }
        public long NeedMore { get; set; }
        public string? QrImageUrl { get; set; }
        public string? TopupNote { get; set; }
    }

    public class PaidAccountMultiVm
    {
        public long OrderId { get; set; }
        public string Title { get; set; } = ""; // ví dụ: "Mua acc giá rẻ - Genshin"
        public long TotalAmount { get; set; }
        public List<PaidAccountRowVm> Accounts { get; set; } = new();
        public string Hint { get; set; } = "Thông tin tài khoản được lưu ở lịch sử mua hàng.";
    }

    public class PaidAccountRowVm
    {
        public long AccountId { get; set; }
        public string Title { get; set; } = "";
        public long Price { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? Note { get; set; }
        public bool MaskPublic { get; set; }
    }
}