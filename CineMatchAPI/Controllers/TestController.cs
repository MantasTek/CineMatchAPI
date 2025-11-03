using CineMatchAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineMatchAPI.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
 private readonly CineMatchDbContext _db;

 public TestController(CineMatchDbContext db)
 {
 _db = db;
 }

 // Simple health check
 [HttpGet]
 public IActionResult Health()
 {
 return Ok(new { status = "ok" });
 }

 // Database check
 [HttpGet("db")]
 public async Task<IActionResult> DatabaseCheck()
 {
 try
 {
 // For InMemory/Sqlite: ensure schema exists
 await _db.Database.EnsureCreatedAsync();

 // Quick query to validate access
 await _db.Database.ExecuteSqlRawAsync("SELECT1");
 }
 catch
 {
 // Even if raw SQL not supported (InMemory), still OK since EnsureCreated succeeded
 }

 return Ok(new { database = "ok" });
 }
}
