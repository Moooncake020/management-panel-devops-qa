namespace YonetimPaneli.Services
{
    public class GorevDurumServisi
    {
        private static readonly List<string> TumDurumlar = new()
        {
            "Açık",
            "Devam Ediyor",
            "QA / Test Bekleyen",
            "Bug / Hata",
            "Tamamlandı"
        };

        public List<string> IzinliDurumlariGetir(string mevcutDurum, bool tamDuzenlemeYetkisi)
        {
            var sonuc = new List<string> { mevcutDurum };

            if (tamDuzenlemeYetkisi)
            {
                switch (mevcutDurum)
                {
                    case "Açık":
                        sonuc.Add("Devam Ediyor");
                        break;

                    case "Devam Ediyor":
                        sonuc.Add("QA / Test Bekleyen");
                        sonuc.Add("Bug / Hata");
                        break;

                    case "QA / Test Bekleyen":
                        sonuc.Add("Tamamlandı");
                        sonuc.Add("Bug / Hata");
                        break;

                    case "Bug / Hata":
                        sonuc.Add("Devam Ediyor");
                        break;

                    case "Tamamlandı":
                        sonuc.Add("Açık");
                        break;
                }
            }
            else
            {
                switch (mevcutDurum)
                {
                    case "Açık":
                        sonuc.Add("Devam Ediyor");
                        break;

                    case "Devam Ediyor":
                        sonuc.Add("QA / Test Bekleyen");
                        break;

                    case "Bug / Hata":
                        sonuc.Add("Devam Ediyor");
                        break;
                }
            }

            return sonuc
                .Where(d => TumDurumlar.Contains(d))
                .Distinct()
                .ToList();
        }

        public bool DurumGecisiGecerliMi(string mevcutDurum, string? yeniDurum, bool tamDuzenlemeYetkisi)
        {
            if (string.IsNullOrWhiteSpace(yeniDurum))
            {
                return false;
            }

            if (!TumDurumlar.Contains(yeniDurum))
            {
                return false;
            }

            var izinliDurumlar = IzinliDurumlariGetir(mevcutDurum, tamDuzenlemeYetkisi);
            return izinliDurumlar.Contains(yeniDurum);
        }
    }
}
