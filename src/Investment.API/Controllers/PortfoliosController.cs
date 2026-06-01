using Investment.Application.DTOs;
using Investment.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Investment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfoliosController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public PortfoliosController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PortfolioDto>>> GetAll()
    {
        var portfolios = await _portfolioService.GetAllAsync();
        return Ok(portfolios);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PortfolioDto>> Get(int id)
    {
        var portfolio = await _portfolioService.GetByIdAsync(id);
        if (portfolio == null) return NotFound();
        return Ok(portfolio);
    }

    [HttpGet("{id}/summary")]
    public async Task<ActionResult<PortfolioSummaryDto>> GetSummary(int id)
    {
        var summary = await _portfolioService.GetSummaryAsync(id);
        if (summary == null) return NotFound();
        return Ok(summary);
    }

    [HttpPost]
    public async Task<ActionResult<PortfolioDto>> Create([FromBody] CreatePortfolioDto dto)
    {
        var created = await _portfolioService.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.PortfolioId }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PortfolioDto>> Update(int id, [FromBody] UpdatePortfolioDto dto)
    {
        var updated = await _portfolioService.UpdateAsync(id, dto);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _portfolioService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/assets")]
    public async Task<IActionResult> AddAsset(int id, [FromBody] PortfolioAssetAssignDto dto)
    {
        var result = await _portfolioService.AddAssetAsync(id, dto.AssetId);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpDelete("{id}/assets/{assetId}")]
    public async Task<IActionResult> RemoveAsset(int id, int assetId)
    {
        await _portfolioService.RemoveAssetAsync(id, assetId);
        return NoContent();
    }
}
