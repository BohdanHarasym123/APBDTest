using APBDTest.Models;
using APBDTest.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBDTest.Controllers;

[ApiController]
[Route("api")]
public class WorkshopController : ControllerBase
{
    private readonly IDbService _dbService;

    public WorkshopController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpGet("visits/{id}")]
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

    [HttpPost("visits")]
    public async Task<IActionResult> AddVisitAsync([FromBody] CreateVisitDTO visit)
    {
        if(!visit.services.Any()) return BadRequest("At least one service is required");
        
        try
        {
            await _dbService.AddVisitAsync(visit);
            return Created("", visit);
        }
        catch (Exception e)
        {
            if(e.Message.Contains("must be greater")) return BadRequest(e.Message);
            if(e.Message.Contains("does not exist")) return NotFound(e.Message);
            if(e.Message.Contains("already exists")) return Conflict(e.Message);
            return BadRequest(e.Message);
        }
    }
}