using YonetimPaneli.Models.Enums;

namespace YonetimPaneli.ViewModels.Planlama
{
    public class PlanlamaGorevItemViewModel
    {
        public int Id { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Durum { get; set; } = "Açık";
        public string DurumKodu { get; set; } = "acik";
        public GorevOnceligi Oncelik { get; set; } = GorevOnceligi.Normal;
        public string OncelikMetni { get; set; } = "Normal";
        public string OncelikKodu { get; set; } = "normal";
        public int? AtananKullaniciId { get; set; }
        public string AtananKisi { get; set; } = "Atanmamış";
        public string AtananBasHarfleri { get; set; } = "?";
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? SonTarih { get; set; }
        public DateTime? TamamlanmaTarihi { get; set; }
        public string KalanGunMetni { get; set; } = "Son tarih yok";
        public bool GeciktiMi { get; set; }
        public bool BugunBitiyorMu { get; set; }
        public bool DurumDegistirebilirMi { get; set; }
        public List<string> IzinliDurumlar { get; set; } = new();
        public int YorumSayisi { get; set; }
    }
}
