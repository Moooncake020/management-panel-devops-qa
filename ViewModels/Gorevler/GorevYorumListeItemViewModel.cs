namespace YonetimPaneli.ViewModels.Gorevler
{
    public class GorevYorumListeItemViewModel
    {
        public int Id { get; set; }
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = "Bilinmeyen kullanıcı";
        public string KullaniciBasHarfi { get; set; } = "?";
        public string Icerik { get; set; } = string.Empty;
        public DateTime OlusturulmaTarihi { get; set; }
        public DateTime? DuzenlenmeTarihi { get; set; }
        public bool SilinebilirMi { get; set; }
    }
}
