using CPExtension.Models;
using System.Text.Json;
using Newtonsoft.Json;
using Flurl.Http;

namespace CPExtension.Services
{
    public class FilamentService
    {
        private List<FilamentSwatch> _allFilaments = new();
        private DateTime _lastUpdated = DateTime.MinValue;
        private readonly string _localApiUrl;
        private readonly bool _useLocalApi;

        public FilamentService()
        {
            // Configure your NAS IP address here
            _localApiUrl = "http://your-nas-ip:8080/api/swatches";
            _useLocalApi = true; // Set to false to use original API
        }

        public async Task InitializeAsync()
        {
            if ((DateTime.Now - _lastUpdated).TotalHours < 1 && _allFilaments.Any())
                return;

            if (_useLocalApi)
            {
                await LoadFromLocalApi();
            }
            else
            {
                await LoadFromOriginalApi();
            }
        }

        private async Task LoadFromLocalApi()
        {
            try
            {
                // Call your local API - much faster!
                var response = await $"{_localApiUrl}?pageSize=5000".GetJsonAsync<LocalApiResponse>();

                if (response?.Items != null)
                {
                    _allFilaments = response.Items.Select(ConvertToFilamentSwatch).ToList();
                    _lastUpdated = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load from local API: {ex.Message}");
                // Fallback to original API
                await LoadFromOriginalApi();
            }
        }

        private async Task LoadFromOriginalApi()
        {
            var allFilaments = new List<FilamentSwatch>();
            string nextUrl = "https://filamentcolors.xyz/api/swatch/";
            int pageCount = 0;

            while (!string.IsNullOrEmpty(nextUrl) && pageCount < 100) // Safety limit
            {
                try
                {
                    var response = await nextUrl.GetJsonAsync<FilamentResponseDto>();
                    if (response?.Results == null) break;

                    allFilaments.AddRange(response.Results);
                    nextUrl = response.Next;
                    pageCount++;

                    // Add delay to be respectful to the API
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading page {pageCount}: {ex.Message}");
                    break;
                }
            }

            _allFilaments = allFilaments;
            _lastUpdated = DateTime.Now;
        }

        private FilamentSwatch ConvertToFilamentSwatch(LocalSwatch localSwatch)
        {
            return new FilamentSwatch
            {
                Id = localSwatch.Id,
                ColorName = localSwatch.ColorName,
                HexColor = localSwatch.HexColor,
                CardImg = localSwatch.CardImg,
                MfrPurchaseLink = localSwatch.MfrPurchaseLink,
                Manufacturer = new Manufacturer
                {
                    Name = localSwatch.Manufacturer?.Name ?? "",
                    Website = localSwatch.Manufacturer?.Website ?? ""
                },
                Material = new FilamentType
                {
                    Name = localSwatch.FilamentType?.Name ?? "",
                    HotEndTemp = localSwatch.FilamentType?.HotEndTemp,
                    BedTemp = localSwatch.FilamentType?.BedTemp
                }
            };
        }

        public List<FilamentSwatch> FindClosestFilaments(string hexColor, int count = 5)
        {
            if (!_allFilaments.Any() || string.IsNullOrEmpty(hexColor))
                return new List<FilamentSwatch>();

            var targetColor = HexToRgb(hexColor);
            return _allFilaments
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
            hex = hex.TrimStart('#');
            if (hex.Length != 6) return (0, 0, 0);

            return (
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16)
            );
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

        public async Task<List<string>> GetAvailableColors()
        {
            if (_useLocalApi)
            {
                try
                {
                    return await $"{_localApiUrl}/colors".GetJsonAsync<List<string>>();
                }
                catch
                {
                    return _allFilaments.Select(f => f.Manufacturer?.Name ?? "").Distinct().ToList();
                }
            }

            return _allFilaments.Select(f => f.Manufacturer?.Name ?? "").Distinct().ToList();
        }
    }

    // Local API response models
    public class LocalApiResponse
    {
        public List<LocalSwatch> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class LocalSwatch
    {
        public int Id { get; set; }
        public string ColorName { get; set; } = "";
        public string HexColor { get; set; } = "";
        public string CardImg { get; set; } = "";
        public string? MfrPurchaseLink { get; set; }
        public LocalManufacturer? Manufacturer { get; set; }
        public LocalFilamentType? FilamentType { get; set; }
    }

    public class LocalManufacturer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Website { get; set; } = "";
    }

    public class LocalFilamentType
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int HotEndTemp { get; set; }
        public int BedTemp { get; set; }
    }
}