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