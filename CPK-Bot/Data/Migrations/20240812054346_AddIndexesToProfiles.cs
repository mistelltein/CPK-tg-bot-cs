using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPK_Bot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesToProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Rating",
                table: "Profiles",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Role",
                table: "Profiles",
                column: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Profiles_Rating",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_Role",
                table: "Profiles");
        }
    }
}
