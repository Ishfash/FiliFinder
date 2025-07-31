using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SyncService.Models
{
    public class Swatch
    {
        [Key]
        public int Id { get; set; }

        public string ColorName { get; set; } = string.Empty;
        public string ColorParent { get; set; } = string.Empty;
        public string? AltColorParent { get; set; }
        public string HexColor { get; set; } = string.Empty;

        public string ImageFront { get; set; } = string.Empty;
        public string ImageBack { get; set; } = string.Empty;
        public string? ImageOther { get; set; }
        public string CardImg { get; set; } = string.Empty;

        public DateTime DateAdded { get; set; }
        public DateTime DatePublished { get; set; }
        public string HumanReadableDate { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
        public string? AmazonPurchaseLink { get; set; }
        public string? MfrPurchaseLink { get; set; }

        public bool IsAvailable { get; set; }
        public bool Published { get; set; }

        public double? Td { get; set; }
        public double[]? TdRange { get; set; }

        // Navigation properties
        public int ManufacturerId { get; set; }
        public Manufacturer Manufacturer { get; set; } = null!;

        public int FilamentTypeId { get; set; }
        public FilamentType FilamentType { get; set; } = null!;

        // Color matching properties
        public List<PantoneColor> PantoneColors { get; set; } = new();
        public List<PmsColor> PmsColors { get; set; } = new();
        public List<RalColor> RalColors { get; set; } = new();

        // Store original JSON for complex queries
        public string OriginalJson { get; set; } = string.Empty;

        // Audit fields
        public DateTime LastSynced { get; set; }
    }

    public class Manufacturer
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;

        public List<Swatch> Swatches { get; set; } = new();
    }

    public class FilamentType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int HotEndTemp { get; set; }
        public int BedTemp { get; set; }

        public ParentType? ParentType { get; set; }
        public List<Swatch> Swatches { get; set; } = new();
    }

    public class ParentType
    {
        public string Name { get; set; } = string.Empty;
    }

    public class PantoneColor
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string HexColor { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public int SwatchId { get; set; }
        public Swatch Swatch { get; set; } = null!;
    }

    public class PmsColor
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string HexColor { get; set; } = string.Empty;

        public int SwatchId { get; set; }
        public Swatch Swatch { get; set; } = null!;
    }

    public class RalColor
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string HexColor { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public int SwatchId { get; set; }
        public Swatch Swatch { get; set; } = null!;
    }

    // API Response DTOs (matching the original API structure)
    public class ApiResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }

        [JsonPropertyName("results")]
        public List<ApiSwatch> Results { get; set; } = new();
    }

    public class ApiSwatch
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("manufacturer")]
        public ApiManufacturer Manufacturer { get; set; } = null!;

        [JsonPropertyName("color_name")]
        public string ColorName { get; set; } = string.Empty;

        [JsonPropertyName("filament_type")]
        public ApiFilamentType FilamentType { get; set; } = null!;

        [JsonPropertyName("color_parent")]
        public string ColorParent { get; set; } = string.Empty;

        [JsonPropertyName("alt_color_parent")]
        public string? AltColorParent { get; set; }

        [JsonPropertyName("hex_color")]
        public string HexColor { get; set; } = string.Empty;

        [JsonPropertyName("image_front")]
        public string ImageFront { get; set; } = string.Empty;

        [JsonPropertyName("image_back")]
        public string ImageBack { get; set; } = string.Empty;

        [JsonPropertyName("image_other")]
        public string? ImageOther { get; set; }

        [JsonPropertyName("card_img")]
        public string CardImg { get; set; } = string.Empty;

        [JsonPropertyName("date_added")]
        public DateTime DateAdded { get; set; }

        [JsonPropertyName("date_published")]
        public DateTime DatePublished { get; set; }

        [JsonPropertyName("human_readable_date")]
        public string HumanReadableDate { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("amazon_purchase_link")]
        public string? AmazonPurchaseLink { get; set; }

        [JsonPropertyName("mfr_purchase_link")]
        public string? MfrPurchaseLink { get; set; }

        [JsonPropertyName("is_available")]
        public bool IsAvailable { get; set; }

        [JsonPropertyName("published")]
        public bool Published { get; set; }

        [JsonPropertyName("td")]
        public double? Td { get; set; }

        [JsonPropertyName("td_range")]
        public double[]? TdRange { get; set; }

        [JsonPropertyName("closest_pantone_1")]
        public ApiPantoneColor? ClosestPantone1 { get; set; }

        [JsonPropertyName("closest_pantone_2")]
        public ApiPantoneColor? ClosestPantone2 { get; set; }

        [JsonPropertyName("closest_pantone_3")]
        public ApiPantoneColor? ClosestPantone3 { get; set; }

        [JsonPropertyName("closest_pms_1")]
        public ApiPmsColor? ClosestPms1 { get; set; }

        [JsonPropertyName("closest_pms_2")]
        public ApiPmsColor? ClosestPms2 { get; set; }

        [JsonPropertyName("closest_pms_3")]
        public ApiPmsColor? ClosestPms3 { get; set; }

        [JsonPropertyName("closest_ral_1")]
        public ApiRalColor? ClosestRal1 { get; set; }

        [JsonPropertyName("closest_ral_2")]
        public ApiRalColor? ClosestRal2 { get; set; }

        [JsonPropertyName("closest_ral_3")]
        public ApiRalColor? ClosestRal3 { get; set; }
    }

    public class ApiManufacturer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("website")]
        public string Website { get; set; } = string.Empty;
    }

    public class ApiFilamentType
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("hot_end_temp")]
        public int HotEndTemp { get; set; }

        [JsonPropertyName("bed_temp")]
        public int BedTemp { get; set; }

        [JsonPropertyName("parent_type")]
        public ParentType? ParentType { get; set; }
    }

    public class ApiPantoneColor
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("hex_color")]
        public string HexColor { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;
    }

    public class ApiPmsColor
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("hex_color")]
        public string HexColor { get; set; } = string.Empty;
    }

    public class ApiRalColor
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("hex_color")]
        public string HexColor { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;
    }
}