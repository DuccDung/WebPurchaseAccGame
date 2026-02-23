namespace SystemPurchaseAccGame.ViewModel;

public class OrderHistoryVm
{
    public long OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long TotalAmount { get; set; }
    public string Status { get; set; } = "";

    public List<OrderHistoryItemVm> Items { get; set; } = new();
}

public class OrderHistoryItemVm
{
    public long AccountId { get; set; }
    public string Title { get; set; } = "";
    public long Price { get; set; }

    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Note { get; set; }
}