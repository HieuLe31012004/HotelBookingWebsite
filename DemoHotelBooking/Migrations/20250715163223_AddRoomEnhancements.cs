using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoHotelBooking.Migrations
{
    public partial class AddRoomEnhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RoomNumber",
                table: "Rooms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomType",
                table: "Rooms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Rooms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoomId1",
                table: "RoomImages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomImages_RoomId1",
                table: "RoomImages",
                column: "RoomId1");

            migrationBuilder.AddForeignKey(
                name: "FK_RoomImages_Rooms_RoomId1",
                table: "RoomImages",
                column: "RoomId1",
                principalTable: "Rooms",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoomImages_Rooms_RoomId1",
                table: "RoomImages");

            migrationBuilder.DropIndex(
                name: "IX_RoomImages_RoomId1",
                table: "RoomImages");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "RoomNumber",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "RoomType",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "RoomId1",
                table: "RoomImages");
        }
    }
}
