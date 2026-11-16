using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiCookieHttpOnly.Migrations
{
    /// <inheritdoc />
    public partial class PopulaCategorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO Categorias(CategoriaId, Nome, ImagemUrl)  VALUES (NEWID(), 'Bebidas', 'bebidas.jpg')");
            migrationBuilder.Sql("INSERT INTO Categorias(CategoriaId, Nome, ImagemUrl)  VALUES (NEWID(), 'Lanches', 'lanches.jpg')");
            migrationBuilder.Sql("INSERT INTO Categorias(CategoriaId, Nome, ImagemUrl)  VALUES (NEWID(), 'Sobremesa', 'sobremesa.jpg')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Categorias");
        }
    }
}
