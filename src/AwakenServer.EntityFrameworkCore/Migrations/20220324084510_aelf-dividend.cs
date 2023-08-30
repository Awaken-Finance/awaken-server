// using System;
// using Microsoft.EntityFrameworkCore.Migrations;
//
// namespace AwakenServer.Migrations
// {
//     public partial class aelfdividend : Migration
//     {
//         protected override void Up(MigrationBuilder migrationBuilder)
//         {
//             migrationBuilder.CreateTable(
//                 name: "AppDividend",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     TotalWeight = table.Column<int>(type: "int", nullable: false),
//                     ChainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     Address = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4")
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AppDividend", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateTable(
//                 name: "AppDividendPool",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     DividendId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     PoolTokenId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     ChainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     Pid = table.Column<int>(type: "int", nullable: false),
//                     Weight = table.Column<int>(type: "int", nullable: false),
//                     DepositAmount = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4")
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AppDividendPool", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateTable(
//                 name: "AppDividendPoolToken",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     PoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     DividendTokenId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     ChainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     AccumulativeDividend = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     LastRewardBlock = table.Column<long>(type: "bigint", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AppDividendPoolToken", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateTable(
//                 name: "AppDividendToken",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     DividendId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     TokenId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     ChainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     StartBlock = table.Column<long>(type: "bigint", nullable: false),
//                     EndBlock = table.Column<long>(type: "bigint", nullable: false),
//                     AmountPerBlock = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4")
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AppDividendToken", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateTable(
//                 name: "AppDividendUserPool",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     PoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     ChainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     User = table.Column<string>(type: "varchar(255)", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     DepositAmount = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4")
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AppDividendUserPool", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateTable(
//                 name: "AppDividendUserRecord",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     PoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     DividendTokenId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     ChainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     TransactionHash = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     User = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     DateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
//                     Amount = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     BehaviorType = table.Column<int>(type: "int", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AppDividendUserRecord", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateTable(
//                 name: "AppDividendUserToken",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     DividendTokenId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     PoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     ChainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     User = table.Column<string>(type: "varchar(255)", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     AccumulativeDividend = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4")
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AppDividendUserToken", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateIndex(
//                 name: "IX_AppDividendPoolToken_PoolId",
//                 table: "AppDividendPoolToken",
//                 column: "PoolId");
//
//             migrationBuilder.CreateIndex(
//                 name: "IX_AppDividendUserPool_User",
//                 table: "AppDividendUserPool",
//                 column: "User");
//
//             migrationBuilder.CreateIndex(
//                 name: "IX_AppDividendUserToken_User",
//                 table: "AppDividendUserToken",
//                 column: "User");
//         }
//
//         protected override void Down(MigrationBuilder migrationBuilder)
//         {
//             migrationBuilder.DropForeignKey(
//                 name: "FK_AELFEthereumEventDetailSyncInfos_AELFEthereumEventFilterInfo~",
//                 table: "AELFEthereumEventDetailSyncInfos");
//
//             migrationBuilder.DropTable(
//                 name: "AppDividend");
//
//             migrationBuilder.DropTable(
//                 name: "AppDividendPool");
//
//             migrationBuilder.DropTable(
//                 name: "AppDividendPoolToken");
//
//             migrationBuilder.DropTable(
//                 name: "AppDividendToken");
//
//             migrationBuilder.DropTable(
//                 name: "AppDividendUserPool");
//
//             migrationBuilder.DropTable(
//                 name: "AppDividendUserRecord");
//
//             migrationBuilder.DropTable(
//                 name: "AppDividendUserToken");
//         }
//     }
// }
