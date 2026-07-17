using Microsoft.EntityFrameworkCore;
using YonetimPaneli.Models;

namespace YonetimPaneli.Services
{
    public class OrganizasyonServisi
    {
        private readonly AppDbContext _context;

        public OrganizasyonServisi(AppDbContext context)
        {
            _context = context;
        }

        public bool YoneticiAtanabilirMi(int kullaniciId, int? yeniYoneticiId)
        {
            if (!yeniYoneticiId.HasValue)
            {
                return true;
            }

            if (kullaniciId == yeniYoneticiId.Value)
            {
                return false;
            }

            var kontrolEdilenYoneticiId = yeniYoneticiId;
            var ziyaretEdilenler = new HashSet<int>();

            while (kontrolEdilenYoneticiId.HasValue)
            {
                var mevcutId = kontrolEdilenYoneticiId.Value;

                if (!ziyaretEdilenler.Add(mevcutId))
                {
                    return false;
                }

                if (mevcutId == kullaniciId)
                {
                    return false;
                }

                var yonetici = _context.Kullanicilar
                    .AsNoTracking()
                    .Where(u => u.Id == mevcutId)
                    .Select(u => new { u.Id, u.YoneticiId })
                    .FirstOrDefault();

                if (yonetici == null)
                {
                    return false;
                }

                kontrolEdilenYoneticiId = yonetici.YoneticiId;
            }

            return true;
        }

        public List<int> TumAltKullaniciIdleriniGetir(int kullaniciId)
        {
            var sonuc = new List<int>();

            var kullanicilar = _context.Kullanicilar
                .AsNoTracking()
                .Select(u => new { u.Id, u.YoneticiId })
                .ToList();

            var kuyruk = new Queue<int>();
            kuyruk.Enqueue(kullaniciId);

            var ziyaretEdilenler = new HashSet<int> { kullaniciId };

            while (kuyruk.Count > 0)
            {
                var mevcutYoneticiId = kuyruk.Dequeue();

                var dogrudanAstlar = kullanicilar
                    .Where(u => u.YoneticiId == mevcutYoneticiId)
                    .Select(u => u.Id)
                    .ToList();

                foreach (var astId in dogrudanAstlar)
                {
                    if (!ziyaretEdilenler.Add(astId))
                    {
                        continue;
                    }

                    sonuc.Add(astId);
                    kuyruk.Enqueue(astId);
                }
            }

            return sonuc;
        }

        public bool DogrudanAstiMi(int yoneticiId, int kullaniciId)
        {
            return _context.Kullanicilar
                .Any(u => u.Id == kullaniciId && u.YoneticiId == yoneticiId);
        }

        public bool AltindaCalisiyorMu(int yoneticiId, int kullaniciId)
        {
            var altKullaniciIdleri = TumAltKullaniciIdleriniGetir(yoneticiId);
            return altKullaniciIdleri.Contains(kullaniciId);
        }
    }
}
