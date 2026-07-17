using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace yonetimpaneli.Migrations
{
    /// <inheritdoc />
    public partial class AtananUserIdEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AtananKisi",
                table: "Gorevler");

            migrationBuilder.AlterColumn<string>(
                name: "Baslik",
                table: "Gorevler",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "AtananUserId",
                table: "Gorevler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AtananUserId",
                table: "Gorevler");

            migrationBuilder.AlterColumn<string>(
                name: "Baslik",
                table: "Gorevler",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AtananKisi",
                table: "Gorevler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
