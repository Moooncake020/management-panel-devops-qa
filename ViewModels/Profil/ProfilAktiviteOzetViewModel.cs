namespace YonetimPaneli.ViewModels.Profil
{
    public class ProfilAktiviteOzetViewModel
    {
        public int GorevId { get; set; }
        public string GorevBasligi { get; set; } = string.Empty;
        public string IslemTuru { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public string IslemiYapan { get; set; } = "Sistem";
        public DateTime OlusturulmaTarihi { get; set; }
    }
}
