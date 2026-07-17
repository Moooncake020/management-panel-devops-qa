namespace YonetimPaneli.ViewModels.Home
{
    /// <summary>
    /// Dashboard ekranının bütün metriklerini tek modelde toplar.
    /// View içinde veritabanı sorgusu veya karmaşık hesap yapılmaz.
    /// </summary>
    public class HomeIndexViewModel
    {
        public int ToplamGorev { get; set; }
        public int AcikGorev { get; set; }
        public int DevamEden { get; set; }
        public int QaBekleyen { get; set; }
        public int BugHata { get; set; }
        public int Tamamlanan { get; set; }
        public int Geciken { get; set; }
        public int BugunBiten { get; set; }
        public int KritikAktif { get; set; }
        public int BuHaftaTamamlanan { get; set; }

        public int TamamlanmaOrani => ToplamGorev == 0
            ? 0
            : (int)Math.Round((double)Tamamlanan / ToplamGorev * 100);

        public List<HomeGorevOzetViewModel> SonGorevler { get; set; }
            = new();

        public List<HomeGorevOzetViewModel> DikkatGerektirenGorevler { get; set; }
            = new();
    }
}
