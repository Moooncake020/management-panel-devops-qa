namespace YonetimPaneli.Models
{
    public class ErrorViewModel
    {
        public int StatusCode { get; set; } = 500;
        public string Baslik { get; set; } = "Beklenmeyen bir hata oluştu";
        public string Mesaj { get; set; } = "İşleminiz tamamlanamadı. Lütfen tekrar deneyin.";
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
        public bool GirisGerekliMi { get; set; }
    }
}
