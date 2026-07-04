using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Investment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDividendKindAndSilver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DividendKind",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DividendKind",
                table: "Transactions");
        }
    }
}
