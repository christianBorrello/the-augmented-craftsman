using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TacBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_at",
                table: "blog_posts",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scheduled_at",
                table: "blog_posts");
        }
    }
}
