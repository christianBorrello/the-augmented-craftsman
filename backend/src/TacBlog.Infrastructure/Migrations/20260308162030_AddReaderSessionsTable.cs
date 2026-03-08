using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TacBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReaderSessionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reader_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    provider = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    provider_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reader_sessions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reader_sessions_expires",
                table: "reader_sessions",
                column: "expires_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reader_sessions");
        }
    }
}
