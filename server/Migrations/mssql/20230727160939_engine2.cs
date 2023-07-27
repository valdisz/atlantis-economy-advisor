﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations.mssql
{
    public partial class engine2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Contents",
                table: "GameEngines",
                newName: "Engine");

            migrationBuilder.AddColumn<string>(
                name: "RemoteUrl",
                table: "GameEngines",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemoteUrl",
                table: "GameEngines");

            migrationBuilder.RenameColumn(
                name: "Engine",
                table: "GameEngines",
                newName: "Contents");
        }
    }
}
