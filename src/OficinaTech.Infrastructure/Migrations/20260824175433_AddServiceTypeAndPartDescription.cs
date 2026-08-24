using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaTech.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTypeAndPartDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "service_types",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "parts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "service_types");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "parts");
        }
    }
}
