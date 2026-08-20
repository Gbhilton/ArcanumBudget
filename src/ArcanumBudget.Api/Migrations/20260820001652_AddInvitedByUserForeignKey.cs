using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcanumBudget.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitedByUserForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "InvitedByUserId",
                table: "HouseholdMembers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_InvitedByUserId",
                table: "HouseholdMembers",
                column: "InvitedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseholdMembers_AspNetUsers_InvitedByUserId",
                table: "HouseholdMembers",
                column: "InvitedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HouseholdMembers_AspNetUsers_InvitedByUserId",
                table: "HouseholdMembers");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdMembers_InvitedByUserId",
                table: "HouseholdMembers");

            migrationBuilder.AlterColumn<string>(
                name: "InvitedByUserId",
                table: "HouseholdMembers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
