namespace SystemPurchaseAccGame.ViewModel
{
    public class GameStatsVm
    {
        public int GameId { get; set; }
        public string Name { get; set; } = "";
        public string? Slug { get; set; }
        public string? ThumbnailUrl { get; set; }
        public decimal? Price { get; set; }
        public int SoldCount { get; set; }
        public int RemainingCount { get; set; }
    }
    public class AccountListingVm
    {
        public long AccountListingId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string urlPhoto { get; set; } = "";
        public decimal Price { get; set; }

        // NEW: render hết thuộc tính (Rank, Server, Level...)
        public List<AttrDto> Attributes { get; set; } = new();
    }

    public class AttrDto
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }
    public class GameCategoryVm
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = "";
        public string? Slug { get; set; }

        public List<GameStatsVm> Games { get; set; } = new();
    }

    public class HomePageVm
    {
        public List<GameCategoryVm> Categories { get; set; } = new();
    }
    public class LoginVm
    {
        public string Identity { get; set; } = "";
        public string Password { get; set; } = "";
        public bool Remember { get; set; }
    }

    public class RegisterVm
    {
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Password2 { get; set; } = "";
        public bool Terms { get; set; }
    }
    public class AccountPurchaseDto
    {
        public long AccountId { get; set; }
        public int GameId { get; set; }
        public string Title { get; set; } = "";
        public long Price { get; set; }
        public string? Description { get; set; }
        public string? LoginInfo { get; set; }
        public string Status { get; set; } = "UNKNOWN";

        public List<MediaDto> Media { get; set; } = new();
        public List<AttrDto> Attributes { get; set; } = new();
    }

    public class MediaDto
    {
        public string MediaType { get; set; } = ""; // COVER / AVATAR / GALLERY
        public string Url { get; set; } = "";
        public int SortOrder { get; set; }
    }

    public class CartVm
    {
        public long? OrderId { get; set; }
        public string Status { get; set; } = "PENDING";
        public string PaymentMethod { get; set; } = "WALLET";

        public long Subtotal { get; set; }
        public long Fee { get; set; } = 0;
        public long Total => Subtotal + Fee;

        public List<CartItemVm> Items { get; set; } = new();
    }

    public class CartItemVm
    {
        public long OrderItemId { get; set; }
        public long AccountId { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "";
        public long UnitPrice { get; set; }
        public string ThumbnailUrl { get; set; } = "";
    }
    public class PaymentHistoryVm
    {
        public List<TopupRowVm> Topups { get; set; } = new();
        public List<OrderRowVm> Orders { get; set; } = new();
    }

    public class TopupRowVm
    {
        public long TopupId { get; set; }
        public string Method { get; set; } = "";
        public long Amount { get; set; }
        public long Fee { get; set; }
        public string Status { get; set; } = "";
        public string? Provider { get; set; }
        public string? ReferenceCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class OrderRowVm
    {
        public long OrderId { get; set; }
        public long TotalAmount { get; set; }
        public string Status { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        // Hiển thị danh sách account trong đơn
        public List<OrderItemRowVm> Items { get; set; } = new();
    }

    public class OrderItemRowVm
    {
        public long AccountId { get; set; }
        public long UnitPrice { get; set; }
        public string Title { get; set; } = "";
    }

    public class LoginInfoJson
    {
        public string? User { get; set; }
        public string? Pass { get; set; }
        public string? Note { get; set; }
        public bool MaskPublic { get; set; }
    }
    // Removed duplicate definition of BoughtAccountRowVm
    public class BoughtAccountRowVm
    {
        public long AccountId { get; set; }
        public int GameId { get; set; }
        public string Title { get; set; } = "";
        public long Price { get; set; }
        public string? Description { get; set; }
        public string? LoginInfoRaw { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        // Tách từ LoginInfo JSON
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? Note { get; set; }
        public bool MaskPublic { get; set; }
    }

    public class AccountInfoVm
    {
        public long OrderId { get; set; }
        public List<BoughtAccountRowVm> Items { get; set; } = new();
    }
    public class UserProfileDto
    {
        public long UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        // Lưu ý: thực tế không nên trả Password ra view.
        // Nhưng bạn đang yêu cầu hiển thị theo ảnh, nên mình để field này.
        public string Password { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        // Trong ảnh đang là 1/0 (hoặc 1/NULL). Để int cho đúng DB.
        public int Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string Role { get; set; } = "User";
        public decimal WalletBalance
        {
            get; set;
        }
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

            public string? QrImageUrl { get; set; } // ảnh QR (nếu dùng VietQR dạng ảnh)
            public string? TopupNote { get; set; }  // nội dung chuyển khoản / mã nạp
        }
    }
}
