namespace SystemPurchaseAccGame.Dtos
{
    public class ApiResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public object? Data { get; set; }
    }
    public class AjaxModalResponse
    {
        public bool Ok { get; set; }
        public string Code { get; set; } = ""; // NEED_LOGIN | CONFIRM_PAY | NEED_TOPUP | ERROR
        public string? Html { get; set; }      // partial html
        public string? Message { get; set; }
        public string? RedirectUrl { get; set; }
    }
}
