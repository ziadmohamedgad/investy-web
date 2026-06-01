using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Investment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniquePricePerAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prices_AssetId_PriceDate",
                table: "Prices");

            migrationBuilder.Sql(@"
DELETE FROM Prices
WHERE PriceId IN (
    SELECT PriceId
    FROM (
        SELECT PriceId,
               ROW_NUMBER() OVER (
                   PARTITION BY AssetId
                   ORDER BY PriceDate DESC, CreatedAt DESC, PriceId DESC
               ) AS rn
        FROM Prices
    ) AS ranked
    WHERE ranked.rn > 1
)");

            migrationBuilder.CreateIndex(
                name: "IX_Prices_AssetId",
                table: "Prices",
                column: "AssetId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prices_AssetId",
                table: "Prices");

            migrationBuilder.CreateIndex(
                name: "IX_Prices_AssetId_PriceDate",
                table: "Prices",
                columns: new[] { "AssetId", "PriceDate" });
        }
    }
}
