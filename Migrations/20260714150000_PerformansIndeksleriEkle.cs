using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YonetimPaneli.Models;

#nullable disable

namespace YonetimPaneli.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260714150000_PerformansIndeksleriEkle")]
    public partial class PerformansIndeksleriEkle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server, nvarchar(max) kolonlarını indeks anahtarı olarak kabul etmez.
            // Eski migrationlar Role ve Durum alanlarını nvarchar(max) oluşturduğu için
            // indekslerden önce bu iki kolon güvenli uzunluklara daraltılır.
            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Kullanicilar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Durum",
                table: "Gorevler",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_AktifMi_Role",
                table: "Kullanicilar",
                columns: new[] { "AktifMi", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_DepartmanId_AktifMi",
                table: "Kullanicilar",
                columns: new[] { "DepartmanId", "AktifMi" });

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_YoneticiId_AktifMi",
                table: "Kullanicilar",
                columns: new[] { "YoneticiId", "AktifMi" });

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_AtananKullaniciId_Durum_SonTarih",
                table: "Gorevler",
                columns: new[] { "AtananKullaniciId", "Durum", "SonTarih" });

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_Durum_OlusturulmaTarihi",
                table: "Gorevler",
                columns: new[] { "Durum", "OlusturulmaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_OlusturanKullaniciId_OlusturulmaTarihi",
                table: "Gorevler",
                columns: new[] { "OlusturanKullaniciId", "OlusturulmaTarihi" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_AktifMi_Role",
                table: "Kullanicilar");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_DepartmanId_AktifMi",
                table: "Kullanicilar");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_YoneticiId_AktifMi",
                table: "Kullanicilar");

            migrationBuilder.DropIndex(
                name: "IX_Gorevler_AtananKullaniciId_Durum_SonTarih",
                table: "Gorevler");

            migrationBuilder.DropIndex(
                name: "IX_Gorevler_Durum_OlusturulmaTarihi",
                table: "Gorevler");

            migrationBuilder.DropIndex(
                name: "IX_Gorevler_OlusturanKullaniciId_OlusturulmaTarihi",
                table: "Gorevler");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Kullanicilar",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Durum",
                table: "Gorevler",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);
        }
    }
}
