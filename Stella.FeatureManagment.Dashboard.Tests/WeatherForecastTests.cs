using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Stella.FeatureManagment.Dashboard.Tests;

public class WeatherForecastTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WeatherForecastTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/weatherforecast");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsFiveForecasts()
    {
        // Act
        var forecasts = await _client.GetFromJsonAsync<WeatherForecastDto[]>("/weatherforecast");

        // Assert
        forecasts.ShouldNotBeNull();
        forecasts.Length.ShouldBe(5);
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsValidForecastData()
    {
        // Act
        var forecasts = await _client.GetFromJsonAsync<WeatherForecastDto[]>("/weatherforecast");

        // Assert
        forecasts.ShouldNotBeNull();
        foreach (var forecast in forecasts)
        {
            forecast.Date.ShouldBeGreaterThan(DateOnly.FromDateTime(DateTime.Now));
            forecast.Summary.ShouldNotBeNullOrEmpty();
        }
    }

    private record WeatherForecastDto(DateOnly Date, int TemperatureC, string? Summary, int TemperatureF);
}
