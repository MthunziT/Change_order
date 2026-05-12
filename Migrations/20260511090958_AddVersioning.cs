using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Change_order.Migrations
{
    /// <inheritdoc />
    public partial class AddVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupKey",
                table: "ChangeRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "ChangeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "ChangeRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupKey",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ChangeRequests");
        }
    }
}
