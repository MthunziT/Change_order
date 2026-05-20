using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Change_order.Migrations
{
    /// <inheritdoc />
    public partial class AddWasResubmitted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WasResubmitted",
                table: "ChangeRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WasResubmitted",
                table: "ChangeRequests");
        }
    }
}
