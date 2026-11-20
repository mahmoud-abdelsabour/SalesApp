namespace WebApplication2.Models.Entities
{
    /// <summary>Invoice status values as stored in the database.</summary>
    public static class InvoiceStatus
    {
        public const string Draft = "Draft";
        public const string Confirmed = "Confirmed";
        public const string Cancelled = "Cancelled";
    }
}
