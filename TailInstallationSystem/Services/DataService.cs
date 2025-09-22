using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using TailInstallationSystem.Models;
using TailInstallationSystem.Utils;

namespace TailInstallationSystem
{
    public class DataService
    {
        private readonly CommunicationConfig _config;
        private bool _disposed = false;

        public DataService(CommunicationConfig config = null)
        {
            _config = config ?? ConfigManager.LoadConfig();
        }

        /// <summary>
        /// 安全的数据库操作封装
        /// </summary>
        private async Task<T> ExecuteWithContext<T>(Func<NodeInstrumentMESEntities, Task<T>> operation, T defaultValue = default(T))
        {
            if (_disposed)
            {
                LogManager.LogWarning("DataService已释放，跳过数据库操作");
                return defaultValue;
            }

            try
            {
                using (var context = new NodeInstrumentMESEntities())
                {
                    context.Database.CommandTimeout = 30;
                    return await operation(context);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"数据库操作异常: {ex.Message}");
                return defaultValue;
            }
        }

        public async Task<ProductData> GetProductByBarcode(string barcode)
        {
            return await ExecuteWithContext(async context =>
            {
                return await context.ProductData
                    .FirstOrDefaultAsync(p => p.Barcode == barcode);
            }, null as ProductData);
        }

        public async Task<List<ProductData>> GetUnuploadedProducts()
        {
            return await ExecuteWithContext(async context =>
            {
                return await context.ProductData
                    .Where(p => p.IsUploaded == false || p.IsUploaded == null)
                    .OrderBy(p => p.CreatedTime)
                    .ToListAsync();
            }, new List<ProductData>());
        }

        public async Task<List<ProductData>> GetProductDataHistory(int days = 30)
        {
            return await ExecuteWithContext(async context =>
            {
                var cutoffDate = DateTime.Now.AddDays(-days);

                var productDataList = await context.ProductData
                    .Where(p => p.CreatedTime >= cutoffDate)
                    .OrderByDescending(p => p.CreatedTime)
                    .ToListAsync();
                LogManager.LogInfo($"获取了最近 {days} 天的 {productDataList.Count} 条产品数据");
                return productDataList;
            }, new List<ProductData>());
        }

        public async Task<List<ProductData>> GetAllProductData()
        {
            return await ExecuteWithContext(async context =>
            {
                var productDataList = await context.ProductData
                    .OrderByDescending(p => p.CreatedTime)
                    .ToListAsync();
                LogManager.LogInfo($"获取了所有 {productDataList.Count} 条产品数据");
                return productDataList;
            }, new List<ProductData>());
        }

        public async Task<List<ProductData>> SearchProductData(
            string barcode = null,
            bool? isCompleted = null,
            bool? isUploaded = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            return await ExecuteWithContext(async context =>
            {
                var query = context.ProductData.AsQueryable();

                if (!string.IsNullOrEmpty(barcode))
                {
                    query = query.Where(p => p.Barcode.Contains(barcode));
                }

                if (isCompleted.HasValue)
                {
                    query = query.Where(p => p.IsCompleted == isCompleted.Value);
                }

                if (isUploaded.HasValue)
                {
                    query = query.Where(p => p.IsUploaded == isUploaded.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(p => p.CreatedTime >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    query = query.Where(p => p.CreatedTime <= endDate.Value);
                }

                var result = await query.OrderByDescending(p => p.CreatedTime).ToListAsync();
                LogManager.LogInfo($"搜索产品数据，找到 {result.Count} 条记录");
                return result;
            }, new List<ProductData>());
        }

        public async Task<ProductDataStats> GetProductDataStats()
        {
            return await ExecuteWithContext(async context =>
            {
                var today = DateTime.Today;
                var thisWeek = today.AddDays(-(int)today.DayOfWeek);
                var thisMonth = new DateTime(today.Year, today.Month, 1);
                var stats = new ProductDataStats
                {
                    TotalCount = await context.ProductData.CountAsync(),
                    CompletedCount = await context.ProductData.CountAsync(p => p.IsCompleted == true),
                    UploadedCount = await context.ProductData.CountAsync(p => p.IsUploaded == true),
                    TodayCount = await context.ProductData.CountAsync(p => p.CreatedTime >= today),
                    WeekCount = await context.ProductData.CountAsync(p => p.CreatedTime >= thisWeek),
                    MonthCount = await context.ProductData.CountAsync(p => p.CreatedTime >= thisMonth),
                    PendingUploadCount = await context.ProductData.CountAsync(p => p.IsUploaded != true)
                };
                return stats;
            }, new ProductDataStats());
        }

        public async Task<bool> DeleteProductData(long id)
        {
            return await ExecuteWithContext(async context =>
            {
                var product = await context.ProductData.FindAsync(id);
                if (product != null)
                {
                    context.ProductData.Remove(product);
                    await context.SaveChangesAsync();
                    LogManager.LogInfo($"删除产品数据成功: ID={id}, Barcode={product.Barcode}");
                    return true;
                }

                LogManager.LogWarning($"未找到要删除的产品数据: ID={id}");
                return false;
            }, false);
        }

        public async Task<int> DeleteProductDataBatch(List<long> ids)
        {
            return await ExecuteWithContext(async context =>
            {
                var products = await context.ProductData
                    .Where(p => ids.Contains(p.Id))
                    .ToListAsync();
                if (products.Any())
                {
                    context.ProductData.RemoveRange(products);
                    await context.SaveChangesAsync();
                    LogManager.LogInfo($"批量删除产品数据成功: {products.Count} 条");
                    return products.Count;
                }
                return 0;
            }, 0);
        }

        public void Dispose()
        {
            _disposed = true;
        }

        /// <summary>
        /// 产品数据统计信息
        /// </summary>
        public class ProductDataStats
        {
            public int TotalCount { get; set; }
            public int CompletedCount { get; set; }
            public int UploadedCount { get; set; }
            public int TodayCount { get; set; }
            public int WeekCount { get; set; }
            public int MonthCount { get; set; }
            public int PendingUploadCount { get; set; }
            public double CompletionRate => TotalCount > 0 ? (double)CompletedCount / TotalCount * 100 : 0;
            public double UploadRate => TotalCount > 0 ? (double)UploadedCount / TotalCount * 100 : 0;
        }
    }
}
