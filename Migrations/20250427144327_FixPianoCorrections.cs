using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderTrackingApp.Migrations
{
    public partial class FixPianoCorrections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Note",
                table: "PianiDiLavorazioneScene",
                newName: "Osservazioni");

            migrationBuilder.RenameColumn(
                name: "NoteGenerali",
                table: "PianiDiLavorazione",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "GiornoLavorazione",
                table: "PianiDiLavorazione",
                newName: "TitoloCortometraggio");

            migrationBuilder.AlterColumn<int>(
                name: "NumeroScena",
                table: "PianiDiLavorazioneScene",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "PianiDiLavorazioneScene",
                keyColumn: "Location",
                keyValue: null,
                column: "Location",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "PianiDiLavorazioneScene",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "PianiDiLavorazioneScene",
                keyColumn: "Descrizione",
                keyValue: null,
                column: "Descrizione",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Descrizione",
                table: "PianiDiLavorazioneScene",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Attori",
                table: "PianiDiLavorazioneScene",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "NumeroGiorno",
                table: "PianiDiLavorazioneScene",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OraFine",
                table: "PianiDiLavorazioneScene",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TipoLocation",
                table: "PianiDiLavorazioneScene",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "PianiDiLavorazione",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NomeProduzione",
                table: "PianiDiLavorazione",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Produttore",
                table: "PianiDiLavorazione",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Regista",
                table: "PianiDiLavorazione",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attori",
                table: "PianiDiLavorazioneScene");

            migrationBuilder.DropColumn(
                name: "NumeroGiorno",
                table: "PianiDiLavorazioneScene");

            migrationBuilder.DropColumn(
                name: "OraFine",
                table: "PianiDiLavorazioneScene");

            migrationBuilder.DropColumn(
                name: "TipoLocation",
                table: "PianiDiLavorazioneScene");

            migrationBuilder.DropColumn(
                name: "NomeProduzione",
                table: "PianiDiLavorazione");

            migrationBuilder.DropColumn(
                name: "Produttore",
                table: "PianiDiLavorazione");

            migrationBuilder.DropColumn(
                name: "Regista",
                table: "PianiDiLavorazione");

            migrationBuilder.RenameColumn(
                name: "Osservazioni",
                table: "PianiDiLavorazioneScene",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "TitoloCortometraggio",
                table: "PianiDiLavorazione",
                newName: "GiornoLavorazione");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "PianiDiLavorazione",
                newName: "NoteGenerali");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroScena",
                table: "PianiDiLavorazioneScene",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "PianiDiLavorazioneScene",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Descrizione",
                table: "PianiDiLavorazioneScene",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "PianiDiLavorazione",
                keyColumn: "CreatedBy",
                keyValue: null,
                column: "CreatedBy",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "PianiDiLavorazione",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
