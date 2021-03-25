﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace advisor.Migrations.sqlite
{
    public partial class stats_per_region : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Label",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Terrain",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "X",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "Z",
                table: "Events",
                newName: "RegionId");

            migrationBuilder.AddColumn<long>(
                name: "RegionId",
                table: "FactionStats",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactionStats_RegionId",
                table: "FactionStats",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_RegionId",
                table: "Events",
                column: "RegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Regions_RegionId",
                table: "Events",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FactionStats_Regions_RegionId",
                table: "FactionStats",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Regions_RegionId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_FactionStats_Regions_RegionId",
                table: "FactionStats");

            migrationBuilder.DropIndex(
                name: "IX_FactionStats_RegionId",
                table: "FactionStats");

            migrationBuilder.DropIndex(
                name: "IX_Events_RegionId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "FactionStats");

            migrationBuilder.RenameColumn(
                name: "RegionId",
                table: "Events",
                newName: "Z");

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "Events",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Events",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Terrain",
                table: "Events",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "X",
                table: "Events",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Y",
                table: "Events",
                type: "INTEGER",
                nullable: true);
        }
    }
}
