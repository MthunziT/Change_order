using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Change_order.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    WindowsUsername = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.WindowsUsername);
                });

            migrationBuilder.CreateTable(
                name: "ChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CRId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApplicationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BusinessJustification = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Impact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImpactDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RollbackPlan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TestPlan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeploymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeploymentWindow = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeveloperUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeveloperName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateSubmitted = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Manager1UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Manager1Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Manager1ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Manager1Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Manager2UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Manager2Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Manager2ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Manager2Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeployedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_CRId",
                table: "ChangeRequests",
                column: "CRId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "ChangeRequests");
        }
    }
}
