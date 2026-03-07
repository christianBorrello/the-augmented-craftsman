using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TacBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTagsAndPostTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "published_at",
                table: "blog_posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "post_tags",
                columns: table => new
                {
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_tags", x => new { x.post_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_post_tags_blog_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "blog_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_post_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_post_tags_tag_id",
                table: "post_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_tags_slug",
                table: "tags",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_tags");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropColumn(
                name: "published_at",
                table: "blog_posts");
        }
    }
}
