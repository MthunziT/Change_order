using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Change_order.Migrations
{
    /// <inheritdoc />
    public partial class AddAreasImpacted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AreasImpacted",
                table: "ChangeRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreasImpacted",
                table: "ChangeRequests");
        }
    }
}
