

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ApiCookieHttpOnly.Validation;

namespace ApiCookieHttpOnly.Models;

[Table("Categorias")]
public class Categoria
{
    [Key]
    public Guid CategoriaId { get; set; }


    [Required(ErrorMessage = "O nome é obrigatorio")]
    [StringLength(80)]
    [PrimeiraLetraMaiuscula]
    public string? Nome { get; set; }

    [Required]
    [StringLength(300)]
    public string? ImagemUrl { get; set; }


    public Categoria()
    {
        Produtos = new HashSet<Produto>();
    }

    //Categoria tem muitos produtos
    public ICollection<Produto>? Produtos { get; set; }
}
