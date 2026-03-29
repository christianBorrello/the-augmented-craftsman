using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TacBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTagColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "tags",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE tags SET color = (
                    CASE (floor(random() * 8)::int)
                        WHEN 0 THEN '#C2593A'
                        WHEN 1 THEN '#5B8C5A'
                        WHEN 2 THEN '#B5566E'
                        WHEN 3 THEN '#4A6FA5'
                        WHEN 4 THEN '#C48B28'
                        WHEN 5 THEN '#7B5C8D'
                        WHEN 6 THEN '#3D8B8B'
                        WHEN 7 THEN '#A06040'
                    END
                )
                WHERE color IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "tags",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "color",
                table: "tags");
        }
    }
}
