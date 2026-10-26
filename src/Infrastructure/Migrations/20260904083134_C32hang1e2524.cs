using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class C32hang1e2524 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION pg_temp.feature_flag_tags(value jsonb) RETURNS text[] LANGUAGE plpgsql AS $$
                BEGIN
                    IF value = '{}'::jsonb THEN RETURN ARRAY[]::text[]; END IF;
                    IF jsonb_typeof(value) <> 'array' THEN
                        RAISE EXCEPTION 'ساختار برچسب‌های قدیمی معتبر نیست؛ پیش از ارتقا داده را اصلاح کنید.';
                    END IF;
                    RETURN ARRAY(SELECT jsonb_array_elements_text(value));
                END $$;
                ALTER TABLE "App"."TblFeature" ALTER COLUMN "Tags" DROP DEFAULT;
                ALTER TABLE "App"."TblFeature" ALTER COLUMN "Tags" TYPE text[] USING pg_temp.feature_flag_tags("Tags");
                ALTER TABLE "App"."TblFeature" ALTER COLUMN "Tags" SET DEFAULT ARRAY[]::text[];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "App"."TblFeature" ALTER COLUMN "Tags" DROP DEFAULT;
                ALTER TABLE "App"."TblFeature" ALTER COLUMN "Tags" TYPE jsonb USING to_jsonb("Tags");
                ALTER TABLE "App"."TblFeature" ALTER COLUMN "Tags" SET DEFAULT '[]'::jsonb;
                """);
        }
    }
}
