namespace SalesDashboardAPI.Models
{
    public class Sales
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string Region { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public decimal TotalSales { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }
    }
}
