using CPExtension.Models;
using System.Text.Json;
using Flurl.Http;

namespace CPExtension.Services
{
    public class FilamentService
    {
        private List<FilamentSwatch> _allFilaments = new();
        private DateTime _lastUpdated = DateTime.MinValue;

        public async Task InitializeAsync()
        {
            if ((DateTime.Now - _lastUpdated).TotalHours < 24 && _allFilaments.Any())
                return;

            await LoadAllFilaments();
        }

        private async Task LoadAllFilaments()
        {
            var allFilaments = new List<FilamentSwatch>();
            string nextUrl = "https://filamentcolors.xyz/api/swatch/";

            try
            {
                while (!string.IsNullOrEmpty(nextUrl))
                {
                    // Configure JSON options for snake_case property names
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };

                    // Use GetStringAsync and manually deserialize to have more control
                    var jsonString = await nextUrl.GetStringAsync();
                    var response = JsonSerializer.Deserialize<FilamentResponseDto>(jsonString, jsonOptions);

                    if (response?.Results == null) break;

                    allFilaments.AddRange(response.Results);
                    nextUrl = response.Next;

                    // Add a small delay to be respectful to the API
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                // Log the error or handle it appropriately
                Console.WriteLine($"Error loading filaments: {ex.Message}");
                throw;
            }

            _allFilaments = allFilaments;
            _lastUpdated = DateTime.Now;
        }

        public List<FilamentSwatch> FindClosestFilaments(string hexColor, int count = 5)
        {
            if (!_allFilaments.Any() || string.IsNullOrEmpty(hexColor))
                return new List<FilamentSwatch>();

            var targetColor = HexToRgb(hexColor);
            return _allFilaments
                .Where(f => !string.IsNullOrEmpty(f.HexColor)) // Filter out invalid hex colors
                .Select(f => new {
                    Swatch = f,
                    Distance = ColorDistance(targetColor, HexToRgb(f.HexColor))
                })
                .OrderBy(x => x.Distance)
                .Take(count)
                .Select(x => x.Swatch)
                .ToList();
        }

        private (int R, int G, int B) HexToRgb(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return (0, 0, 0);

            hex = hex.TrimStart('#');
            if (hex.Length != 6) return (0, 0, 0);

            try
            {
                return (
                    Convert.ToInt32(hex.Substring(0, 2), 16),
                    Convert.ToInt32(hex.Substring(2, 2), 16),
                    Convert.ToInt32(hex.Substring(4, 2), 16)
                );
            }
            catch
            {
                return (0, 0, 0);
            }
        }

        private double ColorDistance((int R, int G, int B) color1, (int R, int G, int B) color2)
        {
            // Simple Euclidean distance in RGB space
            return Math.Sqrt(
                Math.Pow(color1.R - color2.R, 2) +
                Math.Pow(color1.G - color2.G, 2) +
                Math.Pow(color1.B - color2.B, 2)
            );
        }
    }
}