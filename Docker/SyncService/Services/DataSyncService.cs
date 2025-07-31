using SyncService.Data;
using SyncService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SyncService.Services
{
    public class DataSyncService
    {
        private readonly FilamentDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DataSyncService> _logger;
        private const string BaseApiUrl = "https://filamentcolors.xyz/api/swatch/";

        public DataSyncService(FilamentDbContext context, HttpClient httpClient, ILogger<DataSyncService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SyncAllAsync()
        {
            _logger.LogInformation("Starting full sync from FilamentColors API");

            try
            {
                var allSwatches = new List<ApiSwatch>();
                var currentUrl = BaseApiUrl;
                int pageCount = 0;

                // Fetch all pages
                while (!string.IsNullOrEmpty(currentUrl))
                {
                    pageCount++;
                    _logger.LogInformation($"Fetching page {pageCount}: {currentUrl}");

                    var response = await _httpClient.GetAsync(currentUrl);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (apiResponse?.Results != null)
                    {
                        allSwatches.AddRange(apiResponse.Results);
                        _logger.LogInformation($"Fetched {apiResponse.Results.Count} swatches from page {pageCount}, total: {allSwatches.Count}");
                    }

                    currentUrl = apiResponse?.Next;

                    // Be respectful to the API
                    await Task.Delay(1000);
                }

                _logger.LogInformation($"Fetched {allSwatches.Count} total swatches from {pageCount} pages");

                // Process and save to database
                await ProcessSwatchesAsync(allSwatches);

                _logger.LogInformation("Sync completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sync process");
                throw;
            }
        }

        private async Task ProcessSwatchesAsync(List<ApiSwatch> apiSwatches)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var apiSwatch in apiSwatches)
                {
                    await ProcessSingleSwatchAsync(apiSwatch);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully processed {apiSwatches.Count} swatches");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing swatches, transaction rolled back");
                throw;
            }
        }

        private async Task ProcessSingleSwatchAsync(ApiSwatch apiSwatch)
        {
            // Ensure manufacturer exists
            var manufacturer = await _context.Manufacturers.FindAsync(apiSwatch.Manufacturer.Id);
            if (manufacturer == null)
            {
                manufacturer = new Manufacturer
                {
                    Id = apiSwatch.Manufacturer.Id,
                    Name = apiSwatch.Manufacturer.Name,
                    Website = apiSwatch.Manufacturer.Website
                };
                _context.Manufacturers.Add(manufacturer);
            }
            else
            {
                // Update existing manufacturer
                manufacturer.Name = apiSwatch.Manufacturer.Name;
                manufacturer.Website = apiSwatch.Manufacturer.Website;
            }

            // Ensure filament type exists
            var filamentType = await _context.FilamentTypes.FindAsync(apiSwatch.FilamentType.Id);
            if (filamentType == null)
            {
                filamentType = new FilamentType
                {
                    Id = apiSwatch.FilamentType.Id,
                    Name = apiSwatch.FilamentType.Name,
                    HotEndTemp = apiSwatch.FilamentType.HotEndTemp,
                    BedTemp = apiSwatch.FilamentType.BedTemp,
                    ParentType = apiSwatch.FilamentType.ParentType
                };
                _context.FilamentTypes.Add(filamentType);
            }
            else
            {
                // Update existing filament type
                filamentType.Name = apiSwatch.FilamentType.Name;
                filamentType.HotEndTemp = apiSwatch.FilamentType.HotEndTemp;
                filamentType.BedTemp = apiSwatch.FilamentType.BedTemp;
                filamentType.ParentType = apiSwatch.FilamentType.ParentType;
            }

            // Create or update swatch
            var existingSwatch = await _context.Swatches
                .Include(s => s.PantoneColors)
                .Include(s => s.PmsColors)
                .Include(s => s.RalColors)
                .FirstOrDefaultAsync(s => s.Id == apiSwatch.Id);

            if (existingSwatch == null)
            {
                var newSwatch = MapApiSwatchToSwatch(apiSwatch);
                newSwatch.LastSynced = DateTime.UtcNow;
                _context.Swatches.Add(newSwatch);
            }
            else
            {
                // Update existing swatch
                MapApiSwatchToExistingSwatch(apiSwatch, existingSwatch);
                existingSwatch.LastSynced = DateTime.UtcNow;
            }
        }

        private Swatch MapApiSwatchToSwatch(ApiSwatch apiSwatch)
        {
            var swatch = new Swatch
            {
                Id = apiSwatch.Id,
                ColorName = apiSwatch.ColorName,
                ColorParent = apiSwatch.ColorParent,
                AltColorParent = apiSwatch.AltColorParent,
                HexColor = apiSwatch.HexColor,
                ImageFront = apiSwatch.ImageFront,
                ImageBack = apiSwatch.ImageBack,
                ImageOther = apiSwatch.ImageOther,
                CardImg = apiSwatch.CardImg,
                DateAdded = apiSwatch.DateAdded,
                DatePublished = apiSwatch.DatePublished,
                HumanReadableDate = apiSwatch.HumanReadableDate,
                Notes = apiSwatch.Notes,
                AmazonPurchaseLink = apiSwatch.AmazonPurchaseLink,
                MfrPurchaseLink = apiSwatch.MfrPurchaseLink,
                IsAvailable = apiSwatch.IsAvailable,
                Published = apiSwatch.Published,
                Td = apiSwatch.Td,
                TdRange = apiSwatch.TdRange,
                ManufacturerId = apiSwatch.Manufacturer.Id,
                FilamentTypeId = apiSwatch.FilamentType.Id,
                OriginalJson = JsonSerializer.Serialize(apiSwatch),
                LastSynced = DateTime.UtcNow
            };

            // Add Pantone colors
            AddPantoneColors(swatch, apiSwatch);

            // Add PMS colors
            AddPmsColors(swatch, apiSwatch);

            // Add RAL colors
            AddRalColors(swatch, apiSwatch);

            return swatch;
        }

        private void MapApiSwatchToExistingSwatch(ApiSwatch apiSwatch, Swatch existingSwatch)
        {
            existingSwatch.ColorName = apiSwatch.ColorName;
            existingSwatch.ColorParent = apiSwatch.ColorParent;
            existingSwatch.AltColorParent = apiSwatch.AltColorParent;
            existingSwatch.HexColor = apiSwatch.HexColor;
            existingSwatch.ImageFront = apiSwatch.ImageFront;
            existingSwatch.ImageBack = apiSwatch.ImageBack;
            existingSwatch.ImageOther = apiSwatch.ImageOther;
            existingSwatch.CardImg = apiSwatch.CardImg;
            existingSwatch.DateAdded = apiSwatch.DateAdded;
            existingSwatch.DatePublished = apiSwatch.DatePublished;
            existingSwatch.HumanReadableDate = apiSwatch.HumanReadableDate;
            existingSwatch.Notes = apiSwatch.Notes;
            existingSwatch.AmazonPurchaseLink = apiSwatch.AmazonPurchaseLink;
            existingSwatch.MfrPurchaseLink = apiSwatch.MfrPurchaseLink;
            existingSwatch.IsAvailable = apiSwatch.IsAvailable;
            existingSwatch.Published = apiSwatch.Published;
            existingSwatch.Td = apiSwatch.Td;
            existingSwatch.TdRange = apiSwatch.TdRange;
            existingSwatch.ManufacturerId = apiSwatch.Manufacturer.Id;
            existingSwatch.FilamentTypeId = apiSwatch.FilamentType.Id;
            existingSwatch.OriginalJson = JsonSerializer.Serialize(apiSwatch);

            // Clear existing color relationships
            existingSwatch.PantoneColors.Clear();
            existingSwatch.PmsColors.Clear();
            existingSwatch.RalColors.Clear();

            // Add updated color relationships
            AddPantoneColors(existingSwatch, apiSwatch);
            AddPmsColors(existingSwatch, apiSwatch);
            AddRalColors(existingSwatch, apiSwatch);
        }

        private void AddPantoneColors(Swatch swatch, ApiSwatch apiSwatch)
        {
            if (apiSwatch.ClosestPantone1 != null)
            {
                swatch.PantoneColors.Add(new PantoneColor
                {
                    Code = apiSwatch.ClosestPantone1.Code,
                    Name = apiSwatch.ClosestPantone1.Name,
                    HexColor = apiSwatch.ClosestPantone1.HexColor,
                    Category = apiSwatch.ClosestPantone1.Category
                });
            }

            if (apiSwatch.ClosestPantone2 != null)
            {
                swatch.PantoneColors.Add(new PantoneColor
                {
                    Code = apiSwatch.ClosestPantone2.Code,
                    Name = apiSwatch.ClosestPantone2.Name,
                    HexColor = apiSwatch.ClosestPantone2.HexColor,
                    Category = apiSwatch.ClosestPantone2.Category
                });
            }

            if (apiSwatch.ClosestPantone3 != null)
            {
                swatch.PantoneColors.Add(new PantoneColor
                {
                    Code = apiSwatch.ClosestPantone3.Code,
                    Name = apiSwatch.ClosestPantone3.Name,
                    HexColor = apiSwatch.ClosestPantone3.HexColor,
                    Category = apiSwatch.ClosestPantone3.Category
                });
            }
        }

        private void AddPmsColors(Swatch swatch, ApiSwatch apiSwatch)
        {
            if (apiSwatch.ClosestPms1 != null)
            {
                swatch.PmsColors.Add(new PmsColor
                {
                    Code = apiSwatch.ClosestPms1.Code,
                    HexColor = apiSwatch.ClosestPms1.HexColor
                });
            }

            if (apiSwatch.ClosestPms2 != null)
            {
                swatch.PmsColors.Add(new PmsColor
                {
                    Code = apiSwatch.ClosestPms2.Code,
                    HexColor = apiSwatch.ClosestPms2.HexColor
                });
            }

            if (apiSwatch.ClosestPms3 != null)
            {
                swatch.PmsColors.Add(new PmsColor
                {
                    Code = apiSwatch.ClosestPms3.Code,
                    HexColor = apiSwatch.ClosestPms3.HexColor
                });
            }
        }

        private void AddRalColors(Swatch swatch, ApiSwatch apiSwatch)
        {
            if (apiSwatch.ClosestRal1 != null)
            {
                swatch.RalColors.Add(new RalColor
                {
                    Code = apiSwatch.ClosestRal1.Code,
                    Name = apiSwatch.ClosestRal1.Name,
                    HexColor = apiSwatch.ClosestRal1.HexColor,
                    Category = apiSwatch.ClosestRal1.Category
                });
            }

            if (apiSwatch.ClosestRal2 != null)
            {
                swatch.RalColors.Add(new RalColor
                {
                    Code = apiSwatch.ClosestRal2.Code,
                    Name = apiSwatch.ClosestRal2.Name,
                    HexColor = apiSwatch.ClosestRal2.HexColor,
                    Category = apiSwatch.ClosestRal2.Category
                });
            }

            if (apiSwatch.ClosestRal3 != null)
            {
                swatch.RalColors.Add(new RalColor
                {
                    Code = apiSwatch.ClosestRal3.Code,
                    Name = apiSwatch.ClosestRal3.Name,
                    HexColor = apiSwatch.ClosestRal3.HexColor,
                    Category = apiSwatch.ClosestRal3.Category
                });
            }
        }
    }
}