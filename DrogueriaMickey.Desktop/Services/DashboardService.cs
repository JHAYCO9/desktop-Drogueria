using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DrogueriaMickey.Desktop.Services
{
    public class ActivityItemDto
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Username { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class DashboardSummaryDto
    {
        public decimal TodaySalesTotal { get; set; }
        public int TodaySalesCount { get; set; }
        public decimal AverageTicket { get; set; }
        public string MostUsedPaymentMethod { get; set; } = string.Empty;

        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        public int ExpiredCount { get; set; }

        public bool CashRegisterOpen { get; set; }
        public decimal CashBalance { get; set; }

        public List<ActivityItemDto> RecentActivity { get; set; } = new();
    }

    public class DashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly SessionService _session;

        private const string ConnectionErrorMessage = "No se pudo conectar con el servidor";

        public DashboardService(HttpClient httpClient, SessionService session)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
            _session = session;
        }

        public async Task<ApiResult<DashboardSummaryDto>> GetSummaryAsync()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _session.Token);

                var response = await _httpClient.GetAsync("dashboard/summary");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<DashboardSummaryDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions.Default);
                return new ApiResult<DashboardSummaryDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<DashboardSummaryDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
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