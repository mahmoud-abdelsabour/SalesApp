namespace WebApplication2.Models.ViewModels
{
    /// <summary>
    /// Everything on the Settings page. To add an option: add a property here, read/save it in
    /// SettingsService, and add its field to Views/Settings/Index.cshtml.
    /// </summary>
    public class SettingsViewModel
    {
        /// <summary>Products with this much stock or less are "low stock" (dashboard alert, inventory report).</summary>
        public int? LowStockThreshold { get; set; }
    }
}
