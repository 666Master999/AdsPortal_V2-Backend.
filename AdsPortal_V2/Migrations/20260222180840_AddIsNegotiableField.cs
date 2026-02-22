using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdsPortal_V2.Migrations
{
    /// <inheritdoc />
    public partial class AddIsNegotiableField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNegotiable",
                table: "Ads",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNegotiable",
                table: "Ads");
        }
    }
}
