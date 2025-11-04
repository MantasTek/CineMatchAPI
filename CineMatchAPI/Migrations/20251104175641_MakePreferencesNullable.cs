using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineMatchAPI.Migrations
{
    /// <inheritdoc />
    public partial class MakePreferencesNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Swipes_Movies_MovieId1",
                table: "Swipes");

            migrationBuilder.DropIndex(
                name: "IX_Swipes_MovieId1",
                table: "Swipes");

            migrationBuilder.DropColumn(
                name: "MovieId1",
                table: "Swipes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MovieId1",
                table: "Swipes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Swipes_MovieId1",
                table: "Swipes",
                column: "MovieId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Swipes_Movies_MovieId1",
                table: "Swipes",
                column: "MovieId1",
                principalTable: "Movies",
                principalColumn: "Id");
        }
    }
}
