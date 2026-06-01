using Investment.Application.DTOs;
using Investment.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Investment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetAll([FromQuery] int? assetId, [FromQuery] string? type, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var transactions = await _transactionService.GetFilteredAsync(assetId, type, fromDate, toDate);
        return Ok(transactions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDto>> Get(int id)
    {
        var transaction = await _transactionService.GetByIdAsync(id);
        if (transaction == null) return NotFound();
        return Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create([FromBody] CreateTransactionDto dto)
    {
        var created = await _transactionService.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.TransactionId }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionDto>> Update(int id, [FromBody] UpdateTransactionDto dto)
    {
        var updated = await _transactionService.UpdateAsync(id, dto);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _transactionService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
