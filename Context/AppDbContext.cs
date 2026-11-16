using ApiCookieHttpOnly.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiCookieHttpOnly.Context;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    //o ? é ultil se for opcional
    public DbSet<Categoria>? Categorias { get; set; }
    public DbSet<Produto>? Produtos { get; set; }
}