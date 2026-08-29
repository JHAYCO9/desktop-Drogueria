using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DrogueriaMickey.Desktop.Services
{
    public class DailySalesReportDto
    {
        public string Date { get; set; } = string.Empty;
        public int SalesCount { get; set; }
        public decimal TotalSold { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalNequi { get; set; }
        public decimal AverageTicket { get; set; }
    }

    public class MonthlySalesReportDto
    {
        public string Month { get; set; } = string.Empty;
        public int SalesCount { get; set; }
        public decimal TotalSold { get; set; }
        public decimal AverageDaily { get; set; }
        public string BestDay { get; set; } = string.Empty;
        public decimal BestDayTotal { get; set; }
    }

    public class TopSellingProductDto
    {
        public string MedicineId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalGenerated { get; set; }
    }

    public class LowRotationProductDto
    {
        public string MedicineId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
    }

    public class TopSellingReportDto
    {
        public string GeneratedAt { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public List<TopSellingProductDto> Products { get; set; } = new();
    }

    public class LowRotationReportDto
    {
        public string GeneratedAt { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public List<LowRotationProductDto> Products { get; set; } = new();
    }

    public class ReportService
    {
        private readonly HttpClient _httpClient;
        private readonly SessionService _session;

        private const string ConnectionErrorMessage = "No se pudo conectar con el servidor";

        public ReportService(HttpClient httpClient, SessionService session)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
            _session = session;
        }

        private void AttachToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _session.Token);
        }

        // RF-061
        public async Task<ApiResult<DailySalesReportDto>> GetDailySalesReportAsync(string date)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync($"reports/daily-sales?date={date}");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<DailySalesReportDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<DailySalesReportDto>(JsonOptions.Default);
                return new ApiResult<DailySalesReportDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<DailySalesReportDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-062
        public async Task<ApiResult<MonthlySalesReportDto>> GetMonthlySalesReportAsync(int year, int month)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync($"reports/monthly-sales?year={year}&month={month}");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<MonthlySalesReportDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<MonthlySalesReportDto>(JsonOptions.Default);
                return new ApiResult<MonthlySalesReportDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<MonthlySalesReportDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-063
        public async Task<ApiResult<TopSellingReportDto>> GetTopSellingProductsAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync("reports/top-selling");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<TopSellingReportDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<TopSellingReportDto>(JsonOptions.Default);
                return new ApiResult<TopSellingReportDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<TopSellingReportDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-064
        public async Task<ApiResult<LowRotationReportDto>> GetLowRotationProductsAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync("reports/low-rotation");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<LowRotationReportDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<LowRotationReportDto>(JsonOptions.Default);
                return new ApiResult<LowRotationReportDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<LowRotationReportDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        private async Task<string> ReadError(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions.Default);
                return body?.Error ?? $"Error {(int)response.StatusCode}";
            }
            catch
            {
                return $"Error {(int)response.StatusCode}";
            }
        }

        private class ErrorResponse
        {
            public string? Error { get; set; }
        }
    }
}