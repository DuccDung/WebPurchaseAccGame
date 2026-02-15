namespace SystemPurchaseAccGame.ViewModel
{
    public class PaidAccountInfoVm
    {
        public long OrderId { get; set; }
        public long AccountId { get; set; }
        public string Title { get; set; } = "";
        public long Price { get; set; }

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? Note { get; set; }
        public bool MaskPublic { get; set; }

        public string Hint { get; set; } = "Thông tin tài khoản được lưu ở lịch sử mua hàng.";
    }
}