namespace YonetimPaneli.ViewModels.Bildirimler
{
    public class BildirimMenuViewModel
    {
        public int OkunmamisSayisi { get; set; }
        public List<BildirimListeItemViewModel> SonBildirimler { get; set; } = new();
    }
}
