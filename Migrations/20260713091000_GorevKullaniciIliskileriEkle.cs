using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace yonetimpaneli.Migrations
{
    public partial class GorevKullaniciIliskileriEkle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AtananKullaniciId",
                table: "Gorevler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OlusturanKullaniciId",
                table: "Gorevler",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                @"
                UPDATE g
                SET g.AtananKullaniciId = TRY_CONVERT(int, g.AtananUserId)
                FROM Gorevler AS g
                WHERE TRY_CONVERT(int, g.AtananUserId) IS NOT NULL
                  AND EXISTS
                  (
                      SELECT 1
                      FROM Kullanicilar AS k
                      WHERE k.Id = TRY_CONVERT(int, g.AtananUserId)
                  );
                "
            );

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_AtananKullaniciId",
                table: "Gorevler",
                column: "AtananKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_OlusturanKullaniciId",
                table: "Gorevler",
                column: "OlusturanKullaniciId");

            migrationBuilder.AddForeignKey(
                name: "FK_Gorevler_Kullanicilar_AtananKullaniciId",
                table: "Gorevler",
                column: "AtananKullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Gorevler_Kullanicilar_OlusturanKullaniciId",
                table: "Gorevler",
                column: "OlusturanKullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Gorevler_Kullanicilar_AtananKullaniciId", table: "Gorevler");
            migrationBuilder.DropForeignKey(name: "FK_Gorevler_Kullanicilar_OlusturanKullaniciId", table: "Gorevler");
            migrationBuilder.DropIndex(name: "IX_Gorevler_AtananKullaniciId", table: "Gorevler");
            migrationBuilder.DropIndex(name: "IX_Gorevler_OlusturanKullaniciId", table: "Gorevler");
            migrationBuilder.DropColumn(name: "AtananKullaniciId", table: "Gorevler");
            migrationBuilder.DropColumn(name: "OlusturanKullaniciId", table: "Gorevler");
        }
    }
}
