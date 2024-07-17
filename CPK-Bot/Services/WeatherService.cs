using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace CPK_Bot.Services;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public WeatherService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["WeatherApiKey"];
    }

    public async Task<string> GetWeatherAsync(string location, int days = 3)
    {
        var url = $"http://api.weatherapi.com/v1/forecast.json?key={_apiKey}&q={location}&days={days}&aqi=no&alerts=no";
        var response = await _httpClient.GetStringAsync(url);
        var data = JObject.Parse(response);

        var locationName = data["location"]?["name"]?.ToString();
        if (locationName == null)
        {
            throw new HttpRequestException("City not found");
        }

        var forecastDays = data["forecast"]!["forecastday"];
        var weatherInfo = $"Weather forecast for {locationName}:\n";

        foreach (var day in forecastDays!)
        {
            var date = day["date"]!.ToString();
            var temp = day["day"]!["avgtemp_c"]!.ToString();
            var condition = day["day"]!["condition"]!["text"]!.ToString();
            weatherInfo += $"{date}: {temp}°C, {condition}\n";
        }

        return weatherInfo;
    }
}