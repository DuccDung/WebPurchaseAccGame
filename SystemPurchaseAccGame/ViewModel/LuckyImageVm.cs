namespace SystemPurchaseAccGame.Models.ViewModels
{
    public class LuckyImageSlotVm
    {
        public int Slot { get; set; }           // 1..9
        public string FileKey { get; set; } = ""; // ID_01..ID_09
        public string? Url { get; set; }        // /img/lucky/ID_01.png (nếu tồn tại)
        public bool Exists => !string.IsNullOrEmpty(Url);
    }

    public class LuckyImageIndexVm
    {
        public List<LuckyImageSlotVm> Slots { get; set; } = new();
    }
}