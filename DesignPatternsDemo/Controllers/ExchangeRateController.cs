using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DesignPatternsDemo.Services.Interfaces;

namespace DesignPatternsDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //<summary>
    // ExchangeRateController handles requests related to exchange rates.
    // It uses the Singleton design pattern to ensure that only one instance of the ExchangeRateClient is created and used throughout the application.
    // The controller also uses the IConsoleLogger interface to log messages to the console.
    // </summary>
    public class ExchangeRateController : ControllerBase
    {
        private readonly IConsoleLogger _consoleLogger;

        public ExchangeRateController(IConsoleLogger consoleLogger)
        {
            _consoleLogger = consoleLogger;
        }

        [HttpGet("GetExchangeRate")]
        public async Task<IActionResult> GetExchangeRate(string fromCurrency, string toCurrency)
        {
            try
            {
                var rate =  ExchangeRateClient.GetInstance.GetExchangeRateAsync(fromCurrency, toCurrency);
                _consoleLogger.Log($"Exchange rate from {fromCurrency} to {toCurrency}: {rate}");
                return Ok(rate);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
