namespace SystemPurchaseAccGame.Dtos
{
    public class ApiResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public object? Data { get; set; }
    }

}
