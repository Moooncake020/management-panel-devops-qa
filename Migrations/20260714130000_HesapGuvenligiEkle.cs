using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace yonetimpaneli.Migrations
{
    /// <summary>
    /// Kullanıcı hesaplarına başarısız giriş, geçici kilit, son giriş,
    /// şifre değişim tarihi ve JWT güvenlik damgası alanlarını ekler.
    /// Şifre kolonunun adı korunur; içerik uygulama başlangıcında PBKDF2
    /// hash formatına dönüştürülür.
    /// </summary>
    public partial class HesapGuvenligiEkle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BasarisizGirisSayisi",
                table: "Kullanicilar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GuvenlikDamgasi",
                table: "Kullanicilar",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "KilitBitisTarihi",
                table: "Kullanicilar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SifreDegistirmeTarihi",
                table: "Kullanicilar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonGirisTarihi",
                table: "Kullanicilar",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BasarisizGirisSayisi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "GuvenlikDamgasi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "KilitBitisTarihi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "SifreDegistirmeTarihi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "SonGirisTarihi",
                table: "Kullanicilar");
        }
    }
}
