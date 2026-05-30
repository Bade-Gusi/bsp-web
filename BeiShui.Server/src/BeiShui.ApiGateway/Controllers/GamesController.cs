using BeiShui.ApiGateway.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly AppDbContext _db;

    public GamesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetGames()
    {
        var games = await _db.Games.Where(g => g.Status == 1).ToListAsync();
        return Ok(games);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGame(int id)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null) return NotFound();
        return Ok(game);
    }
}
