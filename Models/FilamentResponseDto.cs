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
        public string Id { get; set; }
        public string ColorName { get; set; }
        public string HexColor { get; set; }
        public Manufacturer Vendor { get; set; }
        public FilamentType Material { get; set; }
        public string CardImg { get; set; }
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
        public int? HotEndTemp { get; set; }
        public int? BedTemp { get; set; }
    }
}

 