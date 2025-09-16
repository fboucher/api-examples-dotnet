using System.Net.Http.Json;
using web.Domain;

namespace web.Services;

public class LocationService
{
    private readonly HttpClient _http;

    public LocationService(HttpClient http)
    {
        _http = http;
    }

    public async Task<string?> GetCityFromCoordinates(double lat, double lng)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat}&lon={lng}&zoom=10&addressdetails=1";
            // Add User-Agent header as required by Nominatim
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-restaurant/1.0");
            var response = await _http.GetFromJsonAsync<NominatimResponse>(url);
            return response?.Address?.City ?? response?.Address?.Town ?? response?.Address?.Village;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetCityFromCoordinates: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> GetCityFromPosition(Position position)
    {
        if (position?.Coordinates == null)
        {
            return null;
        }

        var lat = position.Coordinates.Latitude;
        var lng = position.Coordinates.Longitude;

        return await GetCityFromCoordinates(lat, lng);
    }
}