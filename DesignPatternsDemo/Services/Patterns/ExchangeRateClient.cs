using DesignPatternsDemo.Services.Interfaces;

/// <summary>
/// ExchangeRateClient is a singleton class that provides exchange rate information between different currencies.
/// It uses a private constructor to prevent instantiation from outside the class and provides a static property to access the single instance of the class.
/// The GetExchangeRateAsync method retrieves the exchange rate for a given currency pair. In this mock implementation, it returns predefined exchange rates for a limited set of
/// </summary>
public sealed class ExchangeRateClient : IExchangeRateClient
{
    private static readonly ExchangeRateClient _instance = new();

    private ExchangeRateClient()    
    {
    }
    // Singleton instance of the ExchangeRateClient
    public static ExchangeRateClient GetInstance => _instance; 

    public decimal GetExchangeRateAsync(string fromCurrency, string toCurrency)
    {
        //This is a mock implementation, has 5 currency and returns the rate based on the currency pair. In a real-world scenario, this would call an external API to get the exchange rate.
        var exchangeRates = new Dictionary<string, decimal>
        {
            { "USD-EUR", 0.85m },
            { "EUR-USD", 1.18m },
            { "USD-GBP", 0.75m },
            { "GBP-USD", 1.33m },
            { "USD-INR", 74.5m }
        };

        return exchangeRates.TryGetValue($"{fromCurrency}-{toCurrency}", out var rate) ? rate : throw new Exception($"Exchange rate not found for {fromCurrency} to {toCurrency}");
    }
}
