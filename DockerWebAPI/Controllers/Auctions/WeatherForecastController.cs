using System.Net;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace DockerWebAPI.Controllers.Auctions;

[ApiController]
[Route("[Controller]")]
public class AuctionsController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;

    public AuctionsController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "Auctions")]
    [ProducesResponseType(typeof(Auction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Auction>> Get()
    {
        return new Auction
        {
            Id = Guid.NewGuid(),
            Name = "bob",
            Price = new Random().Next(0, 10),
            Start = DateTime.Now
        };
    }
}

public class Auction
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime Start { get; set; }
    public Decimal Price { get; set; }
}