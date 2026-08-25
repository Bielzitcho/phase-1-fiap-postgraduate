using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaTech.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No DDL change required. EF Core implements [ConcurrencyCheck] on integer columns
            // using application-side WHERE clause inclusion (the original StockQuantity value is
            // appended to every UPDATE WHERE clause at SaveChangesAsync time). This requires no
            // schema alteration — EF reads/writes the column as a normal integer and the
            // concurrency check is enforced entirely at the ORM level.
            //
            // If a true database-level row-version column (e.g., PostgreSQL xmin) were desired,
            // an AddColumn call would be needed here. The current [ConcurrencyCheck] approach is
            // intentional: this body is intentionally empty.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No DDL change was made in Up(), so nothing to revert here.
        }
    }
}
