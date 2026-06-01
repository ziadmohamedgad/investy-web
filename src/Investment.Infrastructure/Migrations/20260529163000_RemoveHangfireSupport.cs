using System;
using Investment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Investment.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(InvestmentDbContext))]
    [Migration("20260529163000_RemoveHangfireSupport")]
    public partial class RemoveHangfireSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'HangFire')
BEGIN
    DECLARE @dropConstraints nvarchar(max) = N'';
    DECLARE @dropTables nvarchar(max) = N'';

    SELECT @dropConstraints += N'ALTER TABLE [' + ps.name + N'].[' + pt.name + N'] DROP CONSTRAINT [' + fk.name + N'];' + CHAR(13)
    FROM sys.foreign_keys AS fk
    INNER JOIN sys.tables AS pt ON pt.object_id = fk.parent_object_id
    INNER JOIN sys.schemas AS ps ON ps.schema_id = pt.schema_id
    WHERE ps.name = N'HangFire';

    IF (@dropConstraints <> N'')
    BEGIN
        EXEC sp_executesql @dropConstraints;
    END

    SELECT @dropTables += N'DROP TABLE [HangFire].[' + t.name + N'];' + CHAR(13)
    FROM sys.tables AS t
    INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    WHERE s.name = N'HangFire';

    IF (@dropTables <> N'')
    BEGIN
        EXEC sp_executesql @dropTables;
    END

    DROP SCHEMA [HangFire];
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}