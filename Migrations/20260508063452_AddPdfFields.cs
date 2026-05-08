using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Change_order.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TestPlan",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RollbackPlan",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImpactDescription",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ChangeRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "BusinessJustification",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalCost",
                table: "ChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalResources",
                table: "ChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssessmentAssignedTo",
                table: "ChangeRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostResourceTime",
                table: "ChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeploymentEndDate",
                table: "ChangeRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpectedOutcome",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpactIfNotDone",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpactOnSchedule",
                table: "ChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpactOnScope",
                table: "ChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpactTimeline",
                table: "ChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImplementationLead",
                table: "ChangeRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionsConsidered",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedActions",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedStrategy",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StakeholdersAffected",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TasksAffected",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalCost",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "AdditionalResources",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "AssessmentAssignedTo",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "CostResourceTime",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "DeploymentEndDate",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "ExpectedOutcome",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "ImpactIfNotDone",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "ImpactOnSchedule",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "ImpactOnScope",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "ImpactTimeline",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "ImplementationLead",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "OptionsConsidered",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "RecommendedActions",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "RecommendedStrategy",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "StakeholdersAffected",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "TasksAffected",
                table: "ChangeRequests");

            migrationBuilder.AlterColumn<string>(
                name: "TestPlan",
                table: "ChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RollbackPlan",
                table: "ChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImpactDescription",
                table: "ChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ChangeRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "BusinessJustification",
                table: "ChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);
        }
    }
}
