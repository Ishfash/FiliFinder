using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FilamentApi.Data;
using FilamentApi.Models;

namespace FilamentColors.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SwatchesController : ControllerBase
    {
        private readonly FilamentDbContext _context;
        private readonly ILogger<SwatchesController> _logger;

        public SwatchesController(FilamentDbContext context, ILogger<SwatchesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<Swatch>>> GetSwatches(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? colorParent = null,
            [FromQuery] string? manufacturer = null,
            [FromQuery] string? filamentType = null,
            [FromQuery] string? search = null)
        {
            var query = _context.Swatches
                .Include(s => s.Manufacturer)
                .Include(s => s.FilamentType)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(colorParent))
            {
                query = query.Where(s => s.ColorParent == colorParent);
            }

            if (!string.IsNullOrWhiteSpace(manufacturer))
            {
                query = query.Where(s => s.Manufacturer.Name.Contains(manufacturer));
            }

            if (!string.IsNullOrWhiteSpace(filamentType))
            {
                query = query.Where(s => s.FilamentType.Name.Contains(filamentType));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.ColorName.Contains(search) ||
                    s.Manufacturer.Name.Contains(search) ||
                    s.HexColor.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var swatches = await query
                .OrderByDescending(s => s.DatePublished)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PagedResult<Swatch>
            {
                Items = swatches,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Swatch>> GetSwatch(int id)
        {
            var swatch = await _context.Swatches
                .Include(s => s.Manufacturer)
                .Include(s => s.FilamentType)
                .Include(s => s.PantoneColors)
                .Include(s => s.PmsColors)
                .Include(s => s.RalColors)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (swatch == null)
            {
                return NotFound();
            }

            return swatch;
        }

        [HttpGet("colors")]
        public async Task<ActionResult<List<string>>> GetColorParents()
        {
            var colors = await _context.Swatches
                .Select(s => s.ColorParent)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(colors);
        }

        [HttpGet("manufacturers")]
        public async Task<ActionResult<List<Manufacturer>>> GetManufacturers()
        {
            var manufacturers = await _context.Manufacturers
                .OrderBy(m => m.Name)
                .ToListAsync();

            return Ok(manufacturers);
        }

        [HttpGet("filament-types")]
        public async Task<ActionResult<List<FilamentType>>> GetFilamentTypes()
        {
            var types = await _context.FilamentTypes
                .OrderBy(ft => ft.Name)
                .ToListAsync();

            return Ok(types);
        }

        [HttpGet("search-by-color/{hexColor}")]
        public async Task<ActionResult<List<Swatch>>> SearchByColor(string hexColor, [FromQuery] int tolerance = 10)
        {
            // Simple color matching - you could implement more sophisticated color distance algorithms
            var swatches = await _context.Swatches
                .Include(s => s.Manufacturer)
                .Include(s => s.FilamentType)
                .Where(s => s.HexColor.ToLower().Contains(hexColor.ToLower().Replace("#", "")))
                .Take(50)
                .ToListAsync();

            return Ok(swatches);
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DatabaseStats>> GetStats()
        {
            var stats = new DatabaseStats
            {
                TotalSwatches = await _context.Swatches.CountAsync(),
                TotalManufacturers = await _context.Manufacturers.CountAsync(),
                TotalFilamentTypes = await _context.FilamentTypes.CountAsync(),
                LastSyncDate = await _context.Swatches.MaxAsync(s => s.LastSynced),
                ColorBreakdown = await _context.Swatches
                    .GroupBy(s => s.ColorParent)
                    .Select(g => new ColorCount { Color = g.Key, Count = g.Count() })
                    .OrderByDescending(cc => cc.Count)
                    .ToListAsync()
            };

            return Ok(stats);
        }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class DatabaseStats
    {
        public int TotalSwatches { get; set; }
        public int TotalManufacturers { get; set; }
        public int TotalFilamentTypes { get; set; }
        public DateTime LastSyncDate { get; set; }
        public List<ColorCount> ColorBreakdown { get; set; } = new();
    }

    public class ColorCount
    {
        public string Color { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}