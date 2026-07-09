using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrogueriaMickey.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class FixUsersLockoutColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La migración "AddLockoutFields" solo creó la tabla SyncEvents,
            // pero nunca agregó las columnas de bloqueo (FailedLoginCount,
            // LockedUntil, LastLoginAt) a la tabla Users, aunque el modelo
            // User.cs ya las esperaba. Esto provocaba que CUALQUIER consulta
            // a Users (por ejemplo, en el login) fallara con
            // "SQLite Error 1: 'no such column: FailedLoginCount'",
            // lanzando una excepción no controlada que cerraba la app.
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");
        }
    }
}
