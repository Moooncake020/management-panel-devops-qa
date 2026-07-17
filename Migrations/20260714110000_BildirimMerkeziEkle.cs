using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace yonetimpaneli.Migrations
{
    /// <summary>
    /// Kullanıcı bazlı uygulama içi bildirimleri saklayan Bildirimler tablosunu
    /// ve sık kullanılan listeleme / tekilleştirme indekslerini oluşturur.
    /// </summary>
    public partial class BildirimMerkeziEkle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    GorevId = table.Column<int>(type: "int", nullable: true),
                    Tur = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Mesaj = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TekilAnahtar = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    OkunduMu = table.Column<bool>(type: "bit", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OkunmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildirimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bildirimler_Gorevler_GorevId",
                        column: x => x.GorevId,
                        principalTable: "Gorevler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Bildirimler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_GorevId",
                table: "Bildirimler",
                column: "GorevId");

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_KullaniciId_OkunduMu_OlusturulmaTarihi",
                table: "Bildirimler",
                columns: new[] { "KullaniciId", "OkunduMu", "OlusturulmaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_TekilAnahtar",
                table: "Bildirimler",
                column: "TekilAnahtar",
                unique: true,
                filter: "[TekilAnahtar] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Bildirimler");
        }
    }
}
