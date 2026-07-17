using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace yonetimpaneli.Migrations
{
    /// <summary>
    /// Görev yorumları ve görev aktivite geçmişi tablolarını ekler.
    /// Mevcut görevler için de başlangıç niteliğinde bir sistem aktivitesi oluşturur.
    /// </summary>
    public partial class GorevYorumVeAktiviteEkle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GorevAktiviteleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GorevId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: true),
                    IslemTuru = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EskiDeger = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    YeniDeger = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GorevAktiviteleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GorevAktiviteleri_Gorevler_GorevId",
                        column: x => x.GorevId,
                        principalTable: "Gorevler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GorevAktiviteleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "GorevYorumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GorevId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DuzenlenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SilindiMi = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GorevYorumlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GorevYorumlari_Gorevler_GorevId",
                        column: x => x.GorevId,
                        principalTable: "Gorevler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GorevYorumlari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GorevAktiviteleri_GorevId",
                table: "GorevAktiviteleri",
                column: "GorevId");

            migrationBuilder.CreateIndex(
                name: "IX_GorevAktiviteleri_KullaniciId",
                table: "GorevAktiviteleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_GorevYorumlari_GorevId",
                table: "GorevYorumlari",
                column: "GorevId");

            migrationBuilder.CreateIndex(
                name: "IX_GorevYorumlari_KullaniciId",
                table: "GorevYorumlari",
                column: "KullaniciId");

            // Bu özellik eklenmeden önce oluşturulan görevler boş zaman çizelgesiyle
            // başlamasın diye her mevcut görev için tek bir sistem kaydı oluştururuz.
            migrationBuilder.Sql(
                """
                INSERT INTO GorevAktiviteleri
                    (GorevId, KullaniciId, IslemTuru, Aciklama, EskiDeger, YeniDeger, OlusturulmaTarihi)
                SELECT
                    Id,
                    NULL,
                    N'SistemKaydi',
                    N'Görev, aktivite geçmişi özelliği etkinleştirildiğinde sisteme aktarıldı.',
                    NULL,
                    NULL,
                    GETDATE()
                FROM Gorevler;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "GorevAktiviteleri");
            migrationBuilder.DropTable(name: "GorevYorumlari");
        }
    }
}
