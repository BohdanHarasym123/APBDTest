using APBDTest.Models;
using APBDTest.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBDTest.Controllers;

[Controller]
[Route("api")]
public class WorkshopController : ControllerBase
{
    private readonly IDbService _dbService;

    public WorkshopController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [Route("visits/{id}")]
    public async Task<IActionResult> GetVisitByIdAsync(int id)
    {
        try
        {
            var visit = await _dbService.GetVisitByIdAsync(id);
            return Ok(visit);
        }
        catch (Exception e)
        {
            if(e.Message.Contains("does not exist")) return NotFound(e.Message);
            return BadRequest(e.Message);
        }
    }
}