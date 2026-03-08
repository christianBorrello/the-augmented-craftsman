using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TacBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLikesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_blog_posts_slug",
                table: "blog_posts",
                column: "slug");

            migrationBuilder.CreateTable(
                name: "likes",
                columns: table => new
                {
                    post_slug = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    visitor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_likes", x => new { x.post_slug, x.visitor_id });
                    table.ForeignKey(
                        name: "FK_likes_blog_posts_post_slug",
                        column: x => x.post_slug,
                        principalTable: "blog_posts",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "likes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_blog_posts_slug",
                table: "blog_posts");
        }
    }
}
