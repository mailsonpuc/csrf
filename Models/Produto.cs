using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiCookieHttpOnly.Models;

public class Produto
{
    [Key]
    public Guid ProdutoId { get; set; }

    [Required(ErrorMessage = "O nome é obrigatorio")]
    [StringLength(10, ErrorMessage = "O nome deve conter entre 20 e 4 caracteres", MinimumLength = 4)]
    public string Nome { get; set; }

    [Required]
    [StringLength(300)]
    public string Descricao { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Preco { get; set; }


    [Required]
    [StringLength(300)]
    public string ImagemUrl { get; set; }


    public float Estoque { get; set; }
    public DateTime DataCadastro { get; set; }


    //FK da categoria
    public Guid CategoriaId { get; set; }
    public Categoria? Categoria { get; set; } // propiedade de navegaçao

}