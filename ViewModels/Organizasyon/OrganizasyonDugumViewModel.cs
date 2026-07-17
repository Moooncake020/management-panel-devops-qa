namespace YonetimPaneli.ViewModels.Organizasyon
{
    /// <summary>
    /// Organizasyon ağacındaki tek bir çalışan kartını temsil eder.
    /// Veritabanı modeli doğrudan View'a gönderilmez; yalnızca ekranda gereken
    /// alanlar taşınır.
    /// </summary>
    public class OrganizasyonDugumViewModel
    {
        public int Id { get; set; }
        public string TamAd { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DepartmanAdi { get; set; } = "Atanmamış";
        public string UnvanAdi { get; set; } = "Atanmamış";
        public string KidemAdi { get; set; } = string.Empty;
        public string OrganizasyonRoluAdi { get; set; } = string.Empty;
        public string SistemRolu { get; set; } = string.Empty;
        public bool AktifMi { get; set; }
        public int DogrudanAstSayisi { get; set; }
        public List<OrganizasyonDugumViewModel> Astlar { get; set; } = new();

        public string BasHarfler
        {
            get
            {
                var parcalar = TamAd
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parcalar.Length == 0)
                {
                    return "?";
                }

                return string.Concat(
                    parcalar
                        .Take(2)
                        .Select(p => char.ToUpperInvariant(p[0]))
                );
            }
        }

        public string AramaMetni =>
            $"{TamAd} {Email} {DepartmanAdi} {UnvanAdi} {KidemAdi} " +
            $"{OrganizasyonRoluAdi} {SistemRolu}";
    }
}
