using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedFavStops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FavStops",
                table: "FavStops");

            migrationBuilder.DropColumn(
                name: "StopIds",
                table: "FavStops");

            migrationBuilder.RenameTable(
                name: "FavStops",
                newName: "fav_stops");

            migrationBuilder.RenameColumn(
                name: "ChatId",
                table: "fav_stops",
                newName: "chat_id");

            migrationBuilder.AddColumn<int>(
                name: "stop_id",
                table: "fav_stops",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_fav_stops",
                table: "fav_stops",
                columns: new[] { "chat_id", "stop_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_fav_stops",
                table: "fav_stops");

            migrationBuilder.DropColumn(
                name: "stop_id",
                table: "fav_stops");

            migrationBuilder.RenameTable(
                name: "fav_stops",
                newName: "FavStops");

            migrationBuilder.RenameColumn(
                name: "chat_id",
                table: "FavStops",
                newName: "ChatId");

            migrationBuilder.AddColumn<List<int>>(
                name: "StopIds",
                table: "FavStops",
                type: "integer[]",
                nullable: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FavStops",
                table: "FavStops",
                column: "ChatId");
        }
    }
}
