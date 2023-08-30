// using System;
// using Microsoft.EntityFrameworkCore.Migrations;
//
// namespace AwakenServer.Migrations
// {
//     public partial class TradePair_IsTokenReversed : Migration
//     {
//         protected override void Up(MigrationBuilder migrationBuilder)
//         {
//             migrationBuilder.AddColumn<bool>(
//                 name: "IsTokenReversed",
//                 table: "AppTradePairs",
//                 type: "tinyint(1)",
//                 nullable: false,
//                 defaultValue: false);
//
//             migrationBuilder.AlterColumn<Guid>(
//                 name: "FilterId",
//                 table: "AELFEthereumEventDetailSyncInfos",
//                 type: "char(64)",
//                 maxLength: 64,
//                 nullable: false,
//                 collation: "ascii_general_ci",
//                 oldClrType: typeof(string),
//                 oldType: "char(64)",
//                 oldMaxLength: 64)
//                 .OldAnnotation("MySql:CharSet", "utf8mb4");
//         }
//
//         protected override void Down(MigrationBuilder migrationBuilder)
//         {
//             migrationBuilder.DropColumn(
//                 name: "IsTokenReversed",
//                 table: "AppTradePairs");
//
//             migrationBuilder.AlterColumn<string>(
//                 name: "FilterId",
//                 table: "AELFEthereumEventDetailSyncInfos",
//                 type: "char(64)",
//                 maxLength: 64,
//                 nullable: false,
//                 oldClrType: typeof(Guid),
//                 oldType: "char(64)",
//                 oldMaxLength: 64)
//                 .Annotation("MySql:CharSet", "utf8mb4")
//                 .OldAnnotation("Relational:Collation", "ascii_general_ci");
//         }
//     }
// }
