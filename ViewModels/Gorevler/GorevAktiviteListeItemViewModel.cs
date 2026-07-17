namespace YonetimPaneli.ViewModels.Gorevler
{
    public class GorevAktiviteListeItemViewModel
    {
        public int Id { get; set; }
        public string IslemTuru { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public string KullaniciAdi { get; set; } = "Sistem";
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }
    }
}
