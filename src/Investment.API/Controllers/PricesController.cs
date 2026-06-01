using Investment.Application.DTOs;
using Investment.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Investment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricesController : ControllerBase
{
    private readonly IPriceService _priceService;

    public PricesController(IPriceService priceService)
    {
        _priceService = priceService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PriceDto>>> GetAll()
    {
        var prices = await _priceService.GetAllAsync();
        return Ok(prices);
    }

    [HttpGet("asset/{assetId}")]
    public async Task<ActionResult<IEnumerable<PriceDto>>> GetByAsset(int assetId)
    {
        var prices = await _priceService.GetByAssetIdAsync(assetId);
        return Ok(prices);
    }

    [HttpPost]
    public async Task<ActionResult<PriceDto>> Create([FromBody] CreatePriceDto dto)
    {
        var created = await _priceService.CreateAsync(dto);
        return Ok(created);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkPriceDto dto)
    {
        var count = await _priceService.BulkCreateAsync(dto);
        return Ok(new { message = $"Successfully added {count} prices." });
    }
}
