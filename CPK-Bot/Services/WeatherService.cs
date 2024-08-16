using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace CPK_Bot.Services;

public interface IWeatherService
{
    Task<string> GetWeatherAsync(string location, int days = 3);
}

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public WeatherService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["WeatherApiKey"];
    }

    public async Task<string> GetWeatherAsync(string location, int days)
    {
        try
        {
            var url = $"https://api.weatherapi.com/v1/forecast.json?key={_apiKey}&q={location}&days={days}&aqi=no&alerts=no";
            var response = await _httpClient.GetStringAsync(url);
            var data = JObject.Parse(response);

            var locationName = data["location"]?["name"]?.ToString();
            if (locationName == null)
            {
                throw new HttpRequestException("City not found");
            }

            var forecastDays = data["forecast"]!["forecastday"];
            var weatherInfo = $"Weather forecast for {locationName}:\n\n";

            foreach (var day in forecastDays!)
            {
                var date = DateTime.Parse(day["date"]!.ToString()).ToString("yyyy-MM-dd");
                var minTemp = day["day"]!["mintemp_c"]!.ToString();
                var maxTemp = day["day"]!["maxtemp_c"]!.ToString();
                var avgTemp = day["day"]!["avgtemp_c"]!.ToString();
                var condition = day["day"]!["condition"]!["text"]!.ToString();
        
                weatherInfo += $"\n*{date}*\n" +
                               $"  - *Max Temp:* `{maxTemp}°C`\n" +
                               $"  - *Avg Temp:* `{avgTemp}°C`\n" +
                               $"  - *Min Temp:* `{minTemp}°C`\n" +
                               $"  - *Condition:* _{condition}_\n";
            }

            return weatherInfo;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("Error fetching weather data.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred.", ex);
        }
    }
}