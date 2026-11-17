using ApiCookieHttpOnly.Context;
using ApiCookieHttpOnly.Models;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiCookieHttpOnly.Controllers;


[Route("api/[controller]")]
[ApiController]
public class CategoriasController: ControllerBase
{ 
    //injetando o dbContext
    private readonly AppDbContext _Context;

    public CategoriasController(AppDbContext context)
    {
        _Context = context;
    }


    [HttpGet("CategoriasComProdutos")]
    public ActionResult<IEnumerable<Categoria>> GetCategoriaComProdutos()
    {
        return _Context.Categorias.
            Include(p => p.Produtos)
            .AsNoTracking()
            .ToList();
    }
    
    
    [HttpGet]
    public ActionResult<IEnumerable<Categoria>> Get()
    {
        var categorias = _Context.Categorias.AsNoTracking().ToList();
        /*
        if (categorias == null)
        {
            return NotFound("Categoias not found");
        }
        return categorias;
        */
        try
        {
            throw new ConnectionAbortedException();
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado");
        }
       
    }


    [HttpGet("{id:guid}",  Name = "ObterCategoria")]
    public ActionResult<Categoria> Get(Guid id)
    {
        var categoria = _Context.Categorias.FirstOrDefault(p => p.CategoriaId == id);
        if (categoria == null)
        {
            return NotFound("Nao encontrado");
        }

        return categoria;
    }
    
    
    
    
    

    /// <summary>
    /// Creates a Categoria.
    /// </summary>
    /// <param name="item"></param>
    /// <returns>A newly created Categoria</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/Categoria
    ///     {
    ///       "categoriaId": "e179b259-fab8-473c-8fe7-510c08de9e71",
    ///       "nome": "bebidas",
    ///       "imagemUrl": "bebidas.png",
    ///     }
    ///
    /// </remarks>
    /// <response code="201">Returns the newly created item</response>
    /// <response code="400">If the item is null</response>
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost]
    public ActionResult Post(Categoria categoria)
    {
        if (categoria == null)
            return BadRequest();
        
        _Context.Categorias.Add(categoria);
        _Context.SaveChanges();
        return new CreatedAtRouteResult("ObterCategoria",  new { id = categoria.CategoriaId }, categoria);
    }
    
    
    
    
    [HttpPut("{id:guid}", Name = "AtualizarCategoria")]
    public ActionResult Put(Guid id, Categoria categoria)
    {
        if (id != categoria.CategoriaId)
        {
            return BadRequest();
        }
        
        _Context.Entry(categoria).State = EntityState.Modified;
        _Context.SaveChanges();
        return Ok(categoria);
    }

    
    
    
    [HttpDelete("{id:guid}", Name = "ExcluirCategoria")]
    public ActionResult Delete(Guid id)
    {
        var categoria = _Context.Categorias.FirstOrDefault((p => p.CategoriaId == id));
        if (categoria == null)
        {
            return NotFound("Categoria nao encontrado");
        }
        _Context.Categorias.Remove(categoria);
        _Context.SaveChanges();
        return Ok(categoria);
    }

}