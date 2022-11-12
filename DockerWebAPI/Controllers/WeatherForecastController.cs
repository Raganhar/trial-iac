using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace DockerWebAPI.Controllers;

[ApiController]
[Route("")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Bob"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public string Get()
    {
        return "hello b";
        // using (var connection = new MySqlConnection("Server=asdfsdfsdf-maindb.cylctavxxdxn.eu-central-1.rds.amazonaws.com;Port=3306;Database=testing;Uid=admin;Pwd=dfgdfg"))
        // // using (var connection = new MySqlConnection("Server=localhost;Port=33006;Database=testing;Uid=root;Pwd=root;"))
        // {
        //      return connection.Query<string>("select * from TestTable").ToArray();
        // }
        // return Summaries;
    }
}