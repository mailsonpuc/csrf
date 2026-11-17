using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiCookieHttpOnly.Migrations
{
    /// <inheritdoc />
    public partial class PopulaProdutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"INSERT INTO Produtos(ProdutoId, Nome, Descricao, Preco, ImagemUrl, Estoque, DataCadastro, CategoriaId) VALUES (
                    NEWID(),
                    'Coca-Cola Diet',
                    'Refrigerante de Cola 350 ml',
                    5.45,
                    'cocacola.jpg',
                    50,
                    GETDATE(),
                    (SELECT CategoriaId FROM Categorias WHERE Nome = 'Bebidas')
                )");

            migrationBuilder.Sql(@"
                INSERT INTO Produtos(ProdutoId, Nome, Descricao, Preco, ImagemUrl, Estoque, DataCadastro, CategoriaId)
                VALUES (
                    NEWID(),
                    'Lanche de Atum',
                    'Lanche de Atum com maionese',
                    8.50,
                    'atum.jpg',
                    10,
                    GETDATE(),
                    (SELECT CategoriaId FROM Categorias WHERE Nome = 'Lanches')
                )");

            migrationBuilder.Sql(@"
                INSERT INTO Produtos(ProdutoId, Nome, Descricao, Preco, ImagemUrl, Estoque, DataCadastro, CategoriaId)
                VALUES (
                    NEWID(),
                    'Pudim 100 g',
                    'Pudim de leite condensado 100g',
                    6.75,
                    'pudim.jpg',
                    20,
                    GETDATE(),
                    (SELECT CategoriaId FROM Categorias WHERE Nome = 'Sobremesa')
                )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Produtos");
        }
    }
}
