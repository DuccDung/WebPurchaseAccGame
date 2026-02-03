namespace SystemPurchaseAccGame.ViewModel
{
    public class GameStatsVm
    {
        public int GameId { get; set; }
        public string Name { get; set; } = "";
        public string? Slug { get; set; }
        public string? ThumbnailUrl { get; set; }

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
}
