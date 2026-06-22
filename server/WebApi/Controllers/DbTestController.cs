using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers;

[ApiController]
[Route("api/db-test")]
public class DbTestController : ControllerBase
{
    private readonly QuanLyDuAnAiContext _context;

    public DbTestController(QuanLyDuAnAiContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Test()
    {
        var canConnect = await _context.Database.CanConnectAsync();

        return Ok(new
        {
            database = "QuanLyDuAn_AI",
            connected = canConnect
        });
    }
}