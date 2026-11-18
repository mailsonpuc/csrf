using ApiCookieHttpOnly.Context;
using ApiCookieHttpOnly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace ApiCookieHttpOnly.Controllers;


[Route("api/[controller]")]
[ApiController]
public class ProdutosController : ControllerBase
{
    //injetando o dbContext
    private readonly AppDbContext _Context;

    public ProdutosController(AppDbContext context)
    {
        _Context = context;
    }

    [HttpGet]
    public async Task< ActionResult<IEnumerable<Produto>>> Get()
    {
        var produtos = await _Context.Produtos.AsNoTracking().ToListAsync();
        if (produtos == null)
        {
            return NotFound("Produtos not found");
        }
        return produtos;
    }


    [HttpGet("{id:guid}",  Name = "ObterProduto")]
    public async Task<ActionResult<Produto>> Get(Guid id,[BindRequired] string nome)
    {
        var nomeProduto = nome;
        
        var produto = await _Context.Produtos.FirstOrDefaultAsync(p => p.ProdutoId == id);
        if (produto == null)
        {
            return NotFound("Nao encontrado");
        }

        return produto;
    }



    [HttpPost]
    public ActionResult Post(Produto produto)
    {
        if (produto == null)
            return BadRequest();
        
        _Context.Produtos.Add(produto);
        _Context.SaveChanges();
        return new CreatedAtRouteResult("ObterProduto",  new { id = produto.ProdutoId }, produto);
                                                                                
    }



    [HttpPut("{id:guid}", Name = "AtualizarProduto")]
    public ActionResult Put(Guid id, Produto produto)
    {
        if (id != produto.ProdutoId)
        {
            return BadRequest();
        }
        
        _Context.Entry(produto).State = EntityState.Modified;
        _Context.SaveChanges();
        return Ok(produto);
    }


    [HttpDelete("{id:guid}", Name = "ExcluirProduto")]
    public ActionResult Delete(Guid id)
    {
        var produto = _Context.Produtos.FirstOrDefault((p => p.ProdutoId == id));
        if (produto == null)
        {
            return NotFound("Produto nao encontrado");
        }
        _Context.Produtos.Remove(produto);
        _Context.SaveChanges();
        return Ok(produto);
    }

}