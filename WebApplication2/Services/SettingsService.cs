using System;
using System.Globalization;
using System.Linq;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Services.Common;

namespace WebApplication2.Services
{
    /// <summary>Reads and saves application settings kept in the AppSettings table.</summary>
    public class SettingsService
    {
        public const string LowStockKey = "LowStockThreshold";
        public const int DefaultLowStockThreshold = 10;
        public const int MaxLowStockThreshold = 100000;

        private readonly AppDbContext db;

        public SettingsService(AppDbContext db)
        {
            this.db = db;
        }

        /// <summary>Products with this much stock or less count as "low stock".</summary>
        public int GetLowStockThreshold()
        {
            var row = db.AppSettings.AsNoTracking().FirstOrDefault(s => s.Key == LowStockKey);

            int value;
            if (row != null && int.TryParse(row.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0)
                return value;

            return DefaultLowStockThreshold;
        }

        public ServiceResult SetLowStockThreshold(int? threshold)
        {
            if (!threshold.HasValue || threshold < 0 || threshold > MaxLowStockThreshold)
                return ServiceResult.Fail(LanguageHelper.T(
                    "Enter a whole number from 0 to " + MaxLowStockThreshold + ".",
                    "أدخل رقماً صحيحاً من 0 إلى " + MaxLowStockThreshold + "."));

            var text = threshold.Value.ToString(CultureInfo.InvariantCulture);
            var row = db.AppSettings.FirstOrDefault(s => s.Key == LowStockKey);
            if (row == null)
                db.AppSettings.Add(new AppSetting { Key = LowStockKey, Value = text });
            else
                row.Value = text;
            db.SaveChanges();

            return ServiceResult.Ok(LanguageHelper.T(
                "Low-stock level saved: " + text + " units or fewer.",
                "تم حفظ حد المخزون المنخفض: " + text + " وحدة أو أقل."));
        }
    }
}
