using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using System.Text.Json;

namespace Controllers;

public class PublicController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<PublicController> _logger;

    public PublicController(AppDbContext context, ILogger<PublicController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/landing")]
    public IActionResult Landing()
    {
        return View();
    }

    [HttpGet("/pricing")]
    public async Task<IActionResult> Pricing()
    {
        var plans = await _context.Set<SubscriptionPlan>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync();

        return View(plans);
    }

    [HttpGet("/features")]
    public IActionResult Features()
    {
        return View();
    }

    [HttpGet("/about")]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet("/contact")]
    public IActionResult Contact()
    {
        return View();
    }

    [HttpGet("/terms")]
    public IActionResult Terms()
    {
        return View();
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy()
    {
        return View();
    }
}
