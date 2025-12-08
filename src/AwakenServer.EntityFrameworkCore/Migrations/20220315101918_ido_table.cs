// using System;
// using Microsoft.EntityFrameworkCore.Migrations;
//
// namespace AwakenServer.Migrations
// {
//     public partial class ido_table : Migration
//     {
//         protected override void Up(MigrationBuilder migrationBuilder)
//         {
//             migrationBuilder.DropColumn(
//                 name: "InterestModelType",
//                 table: "AppCTokens");
//
//             migrationBuilder.RenameColumn(
//                 name: "BlocksPerDay",
//                 table: "AppChains",
//                 newName: "AElfChainId");
//
//             migrationBuilder.CreateTable(
//                 name: "AELFEthereumEventFilterInfos",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     FilterId = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     NodeName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     LatestBlockHeight = table.Column<long>(type: "bigint", nullable: false),
//                     EventStatus = table.Column<int>(type: "int", nullable: false),
//                     ConcurrencyStamp = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4")
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AELFEthereumEventFilterInfos", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateTable(
//                 name: "AELFEthereumProcessorKeys",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     NodeName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     ContractAddress = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     EventName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     FilterId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     ProcessorName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
//                         .Annotation("MySql:CharSet", "utf8mb4")
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AELFEthereumProcessorKeys", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateTable(
//                 name: "AppPublicOfferingRecords",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     PublicOfferingId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     ChainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     User = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     OperateType = table.Column<int>(type: "int", nullable: false),
//                     TokenAmount = table.Column<long>(type: "bigint", nullable: false),
//                     RaiseTokenAmount = table.Column<long>(type: "bigint", nullable: false),
//                     DateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
//                     TransactionHash = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     Channel = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4")
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AppPublicOfferingRecords", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateTable(
//                 name: "AppPublicOfferings",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     TokenId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     RaiseTokenId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     CurrentAmount = table.Column<long>(type: "bigint", nullable: false),
//                     RaiseCurrentAmount = table.Column<long>(type: "bigint", nullable: false),
//                     ChainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     OrderRank = table.Column<long>(type: "bigint", nullable: false),
//                     TokenContractAddress = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     MaxAmount = table.Column<long>(type: "bigint", nullable: false),
//                     RaiseMaxAmount = table.Column<long>(type: "bigint", nullable: false),
//                     Publisher = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
//                     EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AppPublicOfferings", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateTable(
//                 name: "AppUserPublicOfferings",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     PublicOfferingId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     ChainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     User = table.Column<string>(type: "longtext", nullable: true)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     TokenAmount = table.Column<long>(type: "bigint", nullable: false),
//                     RaiseTokenAmount = table.Column<long>(type: "bigint", nullable: false),
//                     IsHarvest = table.Column<bool>(type: "tinyint(1)", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AppUserPublicOfferings", x => x.Id);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateTable(
//                 name: "AELFEthereumEventDetailSyncInfos",
//                 columns: table => new
//                 {
//                     Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
//                     EventId = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     LatestBlockHeight = table.Column<long>(type: "bigint", nullable: false),
//                     FilterId = table.Column<Guid>(type: "char(64)", maxLength: 64, nullable: false, collation: "ascii_general_ci"),
//                     NodeName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
//                         .Annotation("MySql:CharSet", "utf8mb4"),
//                     EventStatus = table.Column<int>(type: "int", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_AELFEthereumEventDetailSyncInfos", x => x.Id);
//                     table.ForeignKey(
//                         name: "FK_AELFEthereumEventDetailSyncInfos_AELFEthereumEventFilterInfo~",
//                         column: x => x.FilterId,
//                         principalTable: "AELFEthereumEventFilterInfos",
//                         principalColumn: "Id",
//                         onDelete: ReferentialAction.Cascade);
//                 })
//                 .Annotation("MySql:CharSet", "utf8mb4");
//
//             migrationBuilder.CreateIndex(
//                 name: "IX_AELFEthereumEventDetailSyncInfos_FilterId",
//                 table: "AELFEthereumEventDetailSyncInfos",
//                 column: "FilterId");
//
//             migrationBuilder.CreateIndex(
//                 name: "IX_AELFEthereumEventDetailSyncInfos_NodeName_EventId_EventStatus",
//                 table: "AELFEthereumEventDetailSyncInfos",
//                 columns: new[] { "NodeName", "EventId", "EventStatus" });
//
//             migrationBuilder.CreateIndex(
//                 name: "IX_AELFEthereumEventFilterInfos_NodeName_FilterId_EventStatus",
//                 table: "AELFEthereumEventFilterInfos",
//                 columns: new[] { "NodeName", "FilterId", "EventStatus" });
//
//             migrationBuilder.CreateIndex(
//                 name: "IX_AppPublicOfferings_OrderRank",
//                 table: "AppPublicOfferings",
//                 column: "OrderRank");
//         }
//
//         protected override void Down(MigrationBuilder migrationBuilder)
//         {
//             migrationBuilder.DropTable(
//                 name: "AELFEthereumEventDetailSyncInfos");
//
//             migrationBuilder.DropTable(
//                 name: "AELFEthereumProcessorKeys");
//
//             migrationBuilder.DropTable(
//                 name: "AppPublicOfferingRecords");
//
//             migrationBuilder.DropTable(
//                 name: "AppPublicOfferings");
//
//             migrationBuilder.DropTable(
//                 name: "AppUserPublicOfferings");
//
//             migrationBuilder.DropTable(
//                 name: "AELFEthereumEventFilterInfos");
//
//             migrationBuilder.RenameColumn(
//                 name: "AElfChainId",
//                 table: "AppChains",
//                 newName: "BlocksPerDay");
//
//             migrationBuilder.AddColumn<int>(
//                 name: "InterestModelType",
//                 table: "AppCTokens",
//                 type: "int",
//                 nullable: false,
//                 defaultValue: 0);
//         }
//     }
// }
