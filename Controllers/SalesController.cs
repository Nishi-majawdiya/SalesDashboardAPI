using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesDashboardAPI.Data;
using SalesDashboardAPI.Models;

namespace SalesDashboardAPI.Controllers
{
    [ApiController]
    [Route("api/sales")]
    public class SalesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalesController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================
        // ✅ CSV / EXCEL UPLOAD (FINAL FIXED)
        // ==============================
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file selected");

            var list = new List<Sales>();

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                bool isHeader = true;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (isHeader)
                    {
                        isHeader = false;
                        continue;
                    }

                    var cols = line.Split(',');

                    // ✅ Handle BOTH formats (your Excel + correct CSV)
                    if (cols.Length < 7)
                        continue;

                    try
                    {
                        DateTime date;

                        // ✅ FIX DATE (handles #### issue)
                        if (!DateTime.TryParse(cols[1], out date))
                            continue;

                        int qty = int.Parse(cols[5]);
                        decimal price = decimal.Parse(cols[6]);

                        var sale = new Sales
                        {
                            OrderDate = date,
                            Category = cols[3].Trim(),
                            Region = cols[4].Trim(),
                            Quantity = qty,
                            Price = price,
                            TotalSales = qty * price,   // auto calculate
                            Month = date.Month,
                            Year = date.Year
                        };

                        list.Add(sale);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            // ❌ No data case
            if (!list.Any())
                return Ok("❌ No valid data found in file");

            // ============================
            // ✅ REMOVE DUPLICATES (FILE)
            // ============================
            list = list
                .GroupBy(x => new { x.OrderDate, x.Region, x.Category, x.Price, x.Quantity })
                .Select(g => g.First())
                .ToList();

            // ============================
            // ✅ REMOVE DUPLICATES (DB)
            // ============================
            var existing = await _context.Sales
                .AsNoTracking()
                .Select(x => new { x.OrderDate, x.Region, x.Category, x.Price, x.Quantity })
                .ToListAsync();

            list = list.Where(x => !existing.Any(e =>
                e.OrderDate == x.OrderDate &&
                e.Region == x.Region &&
                e.Category == x.Category &&
                e.Price == x.Price &&
                e.Quantity == x.Quantity
            )).ToList();

            if (!list.Any())
                return Ok("⚠️ Duplicate file! No new data inserted");

            // ============================
            // ✅ INSERT
            // ============================
            await _context.Sales.AddRangeAsync(list);
            await _context.SaveChangesAsync();

            return Ok($"✅ Uploaded {list.Count} new records");
        }

        // ==============================
        // ✅ FILTER
        // ==============================
        private IQueryable<Sales> ApplyFilters(string region, string category)
        {
            var data = _context.Sales.AsQueryable();

            if (!string.IsNullOrWhiteSpace(region))
                data = data.Where(x => x.Region.ToLower() == region.ToLower());

            if (!string.IsNullOrWhiteSpace(category))
                data = data.Where(x => x.Category.ToLower() == category.ToLower());

            return data;
        }

        // ==============================
        // ✅ REGION
        // ==============================
        [HttpGet("region")]
        public IActionResult Region(string region = "", string category = "")
        {
            var data = ApplyFilters(region, category);

            return Ok(data.GroupBy(x => x.Region)
                .Select(g => new { region = g.Key, total = g.Sum(x => x.TotalSales) })
                .ToList());
        }

        // ==============================
        // ✅ MONTHLY
        // ==============================
        [HttpGet("monthly")]
        public IActionResult Monthly(string region = "", string category = "")
        {
            var data = ApplyFilters(region, category);

            return Ok(data.GroupBy(x => x.Month)
                .Select(g => new { month = g.Key, total = g.Sum(x => x.TotalSales) })
                .OrderBy(x => x.month)
                .ToList());
        }

        // ==============================
        // ✅ CATEGORY
        // ==============================
        [HttpGet("category")]
        public IActionResult Category(string region = "", string category = "")
        {
            var data = ApplyFilters(region, category);

            return Ok(data.GroupBy(x => x.Category)
                .Select(g => new { category = g.Key, total = g.Sum(x => x.TotalSales) })
                .ToList());
        }

        // ==============================
        // ✅ CARDS
        // ==============================
        [HttpGet("orders")]
        public IActionResult Orders(string region = "", string category = "")
        {
            return Ok(ApplyFilters(region, category).Count());
        }

        [HttpGet("total-revenue")]
        public IActionResult Revenue(string region = "", string category = "")
        {
            return Ok(ApplyFilters(region, category).Sum(x => x.TotalSales));
        }

        [HttpGet("top-region")]
        public IActionResult TopRegion(string region = "", string category = "")
        {
            var result = ApplyFilters(region, category)
                .GroupBy(x => x.Region)
                .OrderByDescending(g => g.Sum(x => x.TotalSales))
                .Select(g => g.Key)
                .FirstOrDefault();

            return Ok(result ?? "-");
        }

        [HttpGet("top-category")]
        public IActionResult TopCategory(string region = "", string category = "")
        {
            var result = ApplyFilters(region, category)
                .GroupBy(x => x.Category)
                .OrderByDescending(g => g.Sum(x => x.TotalSales))
                .Select(g => g.Key)
                .FirstOrDefault();

            return Ok(result ?? "-");
        }

        // ==============================
        // ✅ HEATMAP (WORKING)
        // ==============================
        [HttpGet("heatmap")]
        public IActionResult Heatmap(string region = "", string category = "")
        {
            var data = ApplyFilters(region, category).ToList();

            double Corr(Func<Sales, double> a, Func<Sales, double> b)
            {
                var xs = data.Select(a).ToList();
                var ys = data.Select(b).ToList();

                if (!xs.Any()) return 0;

                var avgX = xs.Average();
                var avgY = ys.Average();

                var cov = xs.Zip(ys, (x, y) => (x - avgX) * (y - avgY)).Average();

                var stdX = Math.Sqrt(xs.Average(x => Math.Pow(x - avgX, 2)));
                var stdY = Math.Sqrt(ys.Average(y => Math.Pow(y - avgY, 2)));

                return (stdX == 0 || stdY == 0) ? 0 : Math.Round(cov / (stdX * stdY), 2);
            }

            return Ok(new
            {
                labels = new[] { "Sales", "Price", "Qty", "Month" },
                matrix = new double[][]
                {
                    new double[]{1, Corr(x=> (double)x.TotalSales, x=> (double)x.Price), Corr(x=> (double)x.TotalSales, x=> (double)x.Quantity), Corr(x=> (double)x.TotalSales, x=> (double)x.Month)},
                    new double[]{Corr(x=> (double)x.Price, x=> (double)x.TotalSales),1, Corr(x=> (double)x.Price, x=> (double)x.Quantity), Corr(x=> (double)x.Price, x=> (double)x.Month)},
                    new double[]{Corr(x=> (double)x.Quantity, x=> (double)x.TotalSales), Corr(x=> (double)x.Quantity, x=> (double)x.Price),1, Corr(x=> (double)x.Quantity, x=> (double)x.Month)},
                    new double[]{Corr(x=> (double)x.Month, x=> (double)x.TotalSales), Corr(x=> (double)x.Month, x=> (double)x.Price), Corr(x=> (double)x.Month, x=> (double)x.Quantity),1}
                }
            });
        }
    }
}