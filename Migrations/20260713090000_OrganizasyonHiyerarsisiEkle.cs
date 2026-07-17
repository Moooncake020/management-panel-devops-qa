using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace yonetimpaneli.Migrations
{
    public partial class OrganizasyonHiyerarsisiEkle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departmanlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UstDepartmanId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departmanlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departmanlar_Departmanlar_UstDepartmanId",
                        column: x => x.UstDepartmanId,
                        principalTable: "Departmanlar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Unvanlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unvanlar", x => x.Id);
                });

            migrationBuilder.AddColumn<bool>(
                name: "AktifMi",
                table: "Kullanicilar",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmanId",
                table: "Kullanicilar",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KidemSeviyesi",
                table: "Kullanicilar",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "OrganizasyonRolu",
                table: "Kullanicilar",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "UnvanId",
                table: "Kullanicilar",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YoneticiId",
                table: "Kullanicilar",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departmanlar_UstDepartmanId",
                table: "Departmanlar",
                column: "UstDepartmanId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_DepartmanId",
                table: "Kullanicilar",
                column: "DepartmanId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_UnvanId",
                table: "Kullanicilar",
                column: "UnvanId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_YoneticiId",
                table: "Kullanicilar",
                column: "YoneticiId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Departmanlar_DepartmanId",
                table: "Kullanicilar",
                column: "DepartmanId",
                principalTable: "Departmanlar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Kullanicilar_YoneticiId",
                table: "Kullanicilar",
                column: "YoneticiId",
                principalTable: "Kullanicilar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Unvanlar_UnvanId",
                table: "Kullanicilar",
                column: "UnvanId",
                principalTable: "Unvanlar",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Kullanicilar_Departmanlar_DepartmanId", table: "Kullanicilar");
            migrationBuilder.DropForeignKey(name: "FK_Kullanicilar_Kullanicilar_YoneticiId", table: "Kullanicilar");
            migrationBuilder.DropForeignKey(name: "FK_Kullanicilar_Unvanlar_UnvanId", table: "Kullanicilar");
            migrationBuilder.DropTable(name: "Departmanlar");
            migrationBuilder.DropTable(name: "Unvanlar");
            migrationBuilder.DropIndex(name: "IX_Kullanicilar_DepartmanId", table: "Kullanicilar");
            migrationBuilder.DropIndex(name: "IX_Kullanicilar_UnvanId", table: "Kullanicilar");
            migrationBuilder.DropIndex(name: "IX_Kullanicilar_YoneticiId", table: "Kullanicilar");
            migrationBuilder.DropColumn(name: "AktifMi", table: "Kullanicilar");
            migrationBuilder.DropColumn(name: "DepartmanId", table: "Kullanicilar");
            migrationBuilder.DropColumn(name: "KidemSeviyesi", table: "Kullanicilar");
            migrationBuilder.DropColumn(name: "OrganizasyonRolu", table: "Kullanicilar");
            migrationBuilder.DropColumn(name: "UnvanId", table: "Kullanicilar");
            migrationBuilder.DropColumn(name: "YoneticiId", table: "Kullanicilar");
        }
    }
}
