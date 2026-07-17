using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YonetimPaneli.Models;
using YonetimPaneli.Services;
using YonetimPaneli.ViewModels.Bildirimler;

namespace YonetimPaneli.ViewComponents
{
    public class BildirimMenuViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        private readonly BildirimServisi _bildirimServisi;

        public BildirimMenuViewComponent(
            AppDbContext context,
            BildirimServisi bildirimServisi)
        {
            _context = context;
            _bildirimServisi = bildirimServisi;
        }

        public IViewComponentResult Invoke()
        {
            var claimDegeri = HttpContext.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(claimDegeri, out var kullaniciId))
            {
                return View(new BildirimMenuViewModel());
            }

            var eklenen = _bildirimServisi.ZamanUyarilariniOlustur(kullaniciId);

            if (eklenen > 0)
            {
                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    _context.ChangeTracker.Clear();
                }
            }

            var model = new BildirimMenuViewModel
            {
                OkunmamisSayisi = _bildirimServisi.OkunmamisSayisi(kullaniciId),
                SonBildirimler = _bildirimServisi.Listele(
                    kullaniciId,
                    sadeceOkunmamis: false,
                    adet: 5)
            };

            return View(model);
        }
    }
}
