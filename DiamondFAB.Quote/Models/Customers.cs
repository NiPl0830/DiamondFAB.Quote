namespace DiamondFAB.Quote.Models
{
    public class Customer
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();

        public string CompanyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public double DefaultTaxRate { get; set; }  // e.g. 7.5
        public double DefaultDiscountPercent { get; set; }  // e.g. 10
    }
}
