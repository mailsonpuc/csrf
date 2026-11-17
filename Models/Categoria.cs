

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiCookieHttpOnly.Models;

[Table("Categorias")]
public class Categoria
{
    [Key]
    public Guid CategoriaId { get; set; }


    [Required]
    [StringLength(80)]
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
