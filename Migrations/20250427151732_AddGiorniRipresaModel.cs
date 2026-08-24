using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderTrackingApp.Migrations
{
    public partial class AddGiorniRipresaModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PianiDiLavorazioneScene");

            migrationBuilder.CreateTable(
                name: "GiorniRipresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroGiorno = table.Column<int>(type: "int", nullable: false),
                    Osservazioni = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PianoDiLavorazioneId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiorniRipresa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiorniRipresa_PianiDiLavorazione_PianoDiLavorazioneId",
                        column: x => x.PianoDiLavorazioneId,
                        principalTable: "PianiDiLavorazione",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AttoriRipresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NomeAttore = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GiornoRipresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttoriRipresa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttoriRipresa_GiorniRipresa_GiornoRipresaId",
                        column: x => x.GiornoRipresaId,
                        principalTable: "GiorniRipresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LocationsRipresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NomeLocation = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoLocation = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GiornoRipresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationsRipresa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationsRipresa_GiorniRipresa_GiornoRipresaId",
                        column: x => x.GiornoRipresaId,
                        principalTable: "GiorniRipresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SceneRipresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroScena = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descrizione = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GiornoRipresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneRipresa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneRipresa_GiorniRipresa_GiornoRipresaId",
                        column: x => x.GiornoRipresaId,
                        principalTable: "GiorniRipresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AttoriRipresa_GiornoRipresaId",
                table: "AttoriRipresa",
                column: "GiornoRipresaId");

            migrationBuilder.CreateIndex(
                name: "IX_GiorniRipresa_PianoDiLavorazioneId",
                table: "GiorniRipresa",
                column: "PianoDiLavorazioneId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationsRipresa_GiornoRipresaId",
                table: "LocationsRipresa",
                column: "GiornoRipresaId");

            migrationBuilder.CreateIndex(
                name: "IX_SceneRipresa_GiornoRipresaId",
                table: "SceneRipresa",
                column: "GiornoRipresaId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttoriRipresa");

            migrationBuilder.DropTable(
                name: "LocationsRipresa");

            migrationBuilder.DropTable(
                name: "SceneRipresa");

            migrationBuilder.DropTable(
                name: "GiorniRipresa");

            migrationBuilder.CreateTable(
                name: "PianiDiLavorazioneScene",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PianoDiLavorazioneId = table.Column<int>(type: "int", nullable: false),
                    Attori = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descrizione = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumeroGiorno = table.Column<int>(type: "int", nullable: false),
                    NumeroScena = table.Column<int>(type: "int", nullable: false),
                    OraFine = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OraInizio = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Osservazioni = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoLocation = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PianiDiLavorazioneScene", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PianiDiLavorazioneScene_PianiDiLavorazione_PianoDiLavorazion~",
                        column: x => x.PianoDiLavorazioneId,
                        principalTable: "PianiDiLavorazione",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PianiDiLavorazioneScene_PianoDiLavorazioneId",
                table: "PianiDiLavorazioneScene",
                column: "PianoDiLavorazioneId");
        }
    }
}
