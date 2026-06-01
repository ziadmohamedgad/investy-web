using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Investment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CascadePriceDeleteAndPriceUpsert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prices_Assets_AssetId",
                table: "Prices");

            migrationBuilder.AddForeignKey(
                name: "FK_Prices_Assets_AssetId",
                table: "Prices",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "AssetId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prices_Assets_AssetId",
                table: "Prices");

            migrationBuilder.AddForeignKey(
                name: "FK_Prices_Assets_AssetId",
                table: "Prices",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "AssetId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
