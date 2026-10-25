using System;

namespace DesignPatternsDemo.Services.Interfaces;

public interface IExchangeRateClient
{
   public decimal GetExchangeRateAsync(string fromCurrency, string toCurrency);
}
