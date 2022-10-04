﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace advisor.Migrations.mssql
{
    public partial class is_mage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMage",
                table: "Units",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMage",
                table: "Units");
        }
    }
}
