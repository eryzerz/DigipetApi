using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DigipetApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedPets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Pets",
                columns: new[] { "PetId", "CreatedAt", "Happiness", "Health", "Mood", "Name", "Species", "Type", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1920), 100, 100, 100, "Buddy", "dogs", "labrador", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1920), null },
                    { 2, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1920), 100, 100, 100, "Whiskers", "cats", "persian", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1930), null },
                    { 3, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1930), 100, 100, 100, "Tweety", "birds", "canary", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1930), null },
                    { 4, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1930), 100, 100, 100, "Rex", "dogs", "german shepherd", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1930), null },
                    { 5, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1930), 100, 100, 100, "Fluffy", "cats", "maine coon", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1930), null },
                    { 6, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1930), 100, 100, 100, "Polly", "birds", "parrot", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1930), null },
                    { 7, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1930), 100, 100, 100, "Spike", "dogs", "bulldog", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1930), null },
                    { 8, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1940), 100, 100, 100, "Mittens", "cats", "siamese", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1940), null },
                    { 9, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1940), 100, 100, 100, "Chirpy", "birds", "finch", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1940), null },
                    { 10, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1940), 100, 100, 100, "Max", "dogs", "golden retriever", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1940), null },
                    { 11, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1940), 100, 100, 100, "Luna", "cats", "russian blue", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1940), null },
                    { 12, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1940), 100, 100, 100, "Kiwi", "birds", "budgerigar", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1940), null },
                    { 13, new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1950), 100, 100, 100, "Rocky", "dogs", "rottweiler", new DateTime(2024, 8, 18, 16, 54, 12, 370, DateTimeKind.Utc).AddTicks(1950), null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Pets",
                keyColumn: "PetId",
                keyValue: 13);
        }
    }
}
