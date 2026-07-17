using Microsoft.EntityFrameworkCore;

namespace YonetimPaneli.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Gorev> Gorevler { get; set; }
        public DbSet<AppUser> Kullanicilar { get; set; }
        public DbSet<Departman> Departmanlar { get; set; }
        public DbSet<Unvan> Unvanlar { get; set; }
        public DbSet<GorevYorum> GorevYorumlari { get; set; }
        public DbSet<GorevAktivite> GorevAktiviteleri { get; set; }
        public DbSet<Bildirim> Bildirimler { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Liste ve hiyerarşi ekranlarında sık kullanılan filtreler için
            // birleşik indeksler. Sayfalama ile birlikte tam tablo taramasını azaltır.
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => new { u.AktifMi, u.Role });

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => new { u.DepartmanId, u.AktifMi });

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => new { u.YoneticiId, u.AktifMi });

            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Departman)
                .WithMany(d => d.Kullanicilar)
                .HasForeignKey(u => u.DepartmanId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Unvan)
                .WithMany(u => u.Kullanicilar)
                .HasForeignKey(u => u.UnvanId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Yonetici)
                .WithMany(u => u.Astlar)
                .HasForeignKey(u => u.YoneticiId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Departman>()
                .HasOne(d => d.UstDepartman)
                .WithMany(d => d.AltDepartmanlar)
                .HasForeignKey(d => d.UstDepartmanId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Gorev>()
                .HasOne(g => g.AtananKullanici)
                .WithMany()
                .HasForeignKey(g => g.AtananKullaniciId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Gorev>()
                .HasOne(g => g.OlusturanKullanici)
                .WithMany()
                .HasForeignKey(g => g.OlusturanKullaniciId)
                .OnDelete(DeleteBehavior.NoAction);

            // Görev listeleri ve dashboard metrikleri son tarih / öncelik
            // üzerinden sık filtrelendiği için veritabanı indeksleri eklenir.
            modelBuilder.Entity<Gorev>()
                .HasIndex(g => g.SonTarih);

            modelBuilder.Entity<Gorev>()
                .HasIndex(g => g.Oncelik);

            modelBuilder.Entity<Gorev>()
                .HasIndex(g => new { g.Durum, g.OlusturulmaTarihi });

            modelBuilder.Entity<Gorev>()
                .HasIndex(g => new
                {
                    g.AtananKullaniciId,
                    g.Durum,
                    g.SonTarih
                });

            modelBuilder.Entity<Gorev>()
                .HasIndex(g => new
                {
                    g.OlusturanKullaniciId,
                    g.OlusturulmaTarihi
                });

            modelBuilder.Entity<GorevYorum>()
                .HasOne(y => y.Gorev)
                .WithMany(g => g.Yorumlar)
                .HasForeignKey(y => y.GorevId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GorevYorum>()
                .HasOne(y => y.Kullanici)
                .WithMany()
                .HasForeignKey(y => y.KullaniciId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<GorevAktivite>()
                .HasOne(a => a.Gorev)
                .WithMany(g => g.Aktiviteler)
                .HasForeignKey(a => a.GorevId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GorevAktivite>()
                .HasOne(a => a.Kullanici)
                .WithMany()
                .HasForeignKey(a => a.KullaniciId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Bildirim>()
                .HasOne(b => b.Kullanici)
                .WithMany()
                .HasForeignKey(b => b.KullaniciId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Bildirim>()
                .HasOne(b => b.Gorev)
                .WithMany()
                .HasForeignKey(b => b.GorevId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Bildirim>()
                .HasIndex(b => new { b.KullaniciId, b.OkunduMu, b.OlusturulmaTarihi });

            modelBuilder.Entity<Bildirim>()
                .HasIndex(b => b.TekilAnahtar)
                .IsUnique()
                .HasFilter("[TekilAnahtar] IS NOT NULL");
        }
    }
}
