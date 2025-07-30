using System.Text.Json.Serialization;

namespace CPExtension.Models
{
    public class FilamentResponseDto
    {
        public int Count { get; set; }
        public string Next { get; set; }
        public List<FilamentSwatch> Results { get; set; }
    }

    public class FilamentSwatch
    {
        public int Id { get; set; }

        [JsonPropertyName("color_name")]
        public string ColorName { get; set; }

        [JsonPropertyName("hex_color")]
        public string HexColor { get; set; }

        public Manufacturer Manufacturer { get; set; }

        [JsonPropertyName("filament_type")]
        public FilamentType Material { get; set; }

        [JsonPropertyName("card_img")]
        public string CardImg { get; set; }

        [JsonPropertyName("mfr_purchase_link")]
        public string MfrPurchaseLink { get; set; }
    }

    public class Manufacturer
    {
        public string Name { get; set; }
        public string Website { get; set; }
    }

    public class FilamentType
    {
        public string Name { get; set; }

        [JsonPropertyName("hot_end_temp")]
        public int? HotEndTemp { get; set; }

        [JsonPropertyName("bed_temp")]
        public int? BedTemp { get; set; }
    }
}