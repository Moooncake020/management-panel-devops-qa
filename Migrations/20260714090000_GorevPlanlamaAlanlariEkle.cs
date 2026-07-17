using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace yonetimpaneli.Migrations
{
    /// <summary>
    /// Görevlere öncelik, planlanan başlangıç, son tarih ve gerçek
    /// tamamlanma zamanı alanlarını ekler. Eski görevlerin başlangıç
    /// tarihi oluşturulma gününden türetilir.
    /// </summary>
    public partial class GorevPlanlamaAlanlariEkle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BaslangicTarihi",
                table: "Gorevler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Oncelik",
                table: "Gorevler",
                type: "int",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonTarih",
                table: "Gorevler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TamamlanmaTarihi",
                table: "Gorevler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE Gorevler
                SET BaslangicTarihi = CAST(OlusturulmaTarihi AS date)
                WHERE BaslangicTarihi IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_Oncelik",
                table: "Gorevler",
                column: "Oncelik");

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_SonTarih",
                table: "Gorevler",
                column: "SonTarih");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Gorevler_Oncelik",
                table: "Gorevler");

            migrationBuilder.DropIndex(
                name: "IX_Gorevler_SonTarih",
                table: "Gorevler");

            migrationBuilder.DropColumn(name: "BaslangicTarihi", table: "Gorevler");
            migrationBuilder.DropColumn(name: "Oncelik", table: "Gorevler");
            migrationBuilder.DropColumn(name: "SonTarih", table: "Gorevler");
            migrationBuilder.DropColumn(name: "TamamlanmaTarihi", table: "Gorevler");
        }
    }
}
