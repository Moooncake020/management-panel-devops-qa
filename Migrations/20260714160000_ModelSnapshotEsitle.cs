using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace yonetimpaneli.Migrations
{
    /// <summary>
    /// AppDbContext ile migration model snapshot arasındaki silme davranışı
    /// meta verilerini eşitler. Veritabanı şemasında ek bir değişiklik yapmaz.
    /// </summary>
    public partial class ModelSnapshotEsitle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bilinçli olarak boş: ilişkilerin SQL Server tarafındaki NO ACTION
            // davranışı önceki migrationlarda zaten doğru şekilde oluşturuldu.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Şema değişikliği olmadığı için geri alma işlemi yoktur.
        }
    }
}
