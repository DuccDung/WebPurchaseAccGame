namespace SystemPurchaseAccGame.Models.ViewModels;

public class LuckySpinItemVm
{
    public long ItemId { get; set; }
    public string Title { get; set; } = "";
    public int PrizeTier { get; set; }
    public long PrizeValue { get; set; }
    public int Weight { get; set; }
    public int? Remaining { get; set; }
    public bool IsActive { get; set; }
    public string? WinMessage { get; set; }
}

public class LuckyIndexVm
{
    public List<LuckySpinItemVm> Items { get; set; } = new();
}

public class SpinRequestVm
{
    public List<long> CurrentItemIds { get; set; } = new();
}

public class SpinResponseVm
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public LuckySpinItemVm? Won { get; set; }
    public LuckySpinItemVm? Replacement { get; set; } // item mới để thay vào ô vừa trúng
}