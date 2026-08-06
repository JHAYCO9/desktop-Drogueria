using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DrogueriaMickey.Desktop.Services
{
    public class SystemSettingsDto
    {
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessNit { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public string BusinessPhone { get; set; } = string.Empty;
        public string BusinessEmail { get; set; } = string.Empty;
        public string LegalName { get; set; } = string.Empty;
        public string TaxRegime { get; set; } = string.Empty;
        public string FiscalAddress { get; set; } = string.Empty;
        public int ExpirationAlertDays { get; set; }
        public bool ExpirationAlertEnabled { get; set; }
        public bool StockAlertEnabled { get; set; }
        public int StockAlertFrequencyHours { get; set; }
        public string StockAlertStartTime { get; set; } = string.Empty;
        public string StockAlertEndTime { get; set; } = string.Empty;
        public string ReporteZHour { get; set; } = string.Empty;
        public string? LastBackupAt { get; set; }
    }

    public class BackupResultDto
    {
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class SystemSettingsService
    {
        private readonly HttpClient _httpClient;
        private readonly SessionService _session;

        private const string ConnectionErrorMessage = "No se pudo conectar con el servidor";

        public SystemSettingsService(HttpClient httpClient, SessionService session)
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

        // RF-086
        public async Task<ApiResult<object>> UpdateTaxSettingsAsync(string nit, string legalName, string taxRegime, string fiscalAddress)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PutAsJsonAsync("settings/tax", new
                {
                    nit,
                    legal_name = legalName,
                    tax_regime = taxRegime,
                    fiscal_address = fiscalAddress
                }, JsonOptions.Default);

                if (!response.IsSuccessStatusCode)
                    return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };
                return new ApiResult<object> { Success = true };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<object> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        public async Task<ApiResult<SystemSettingsDto>> GetAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync("settings");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<SystemSettingsDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<SystemSettingsDto>(JsonOptions.Default);
                return new ApiResult<SystemSettingsDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<SystemSettingsDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-079
        public async Task<ApiResult<object>> UpdateBusinessInfoAsync(string name, string nit, string address, string phone, string email)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PutAsJsonAsync("settings/business", new
                {
                    business_name = name,
                    business_nit = nit,
                    business_address = address,
                    business_phone = phone,
                    business_email = email
                }, JsonOptions.Default);

                if (!response.IsSuccessStatusCode)
                    return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };
                return new ApiResult<object> { Success = true };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<object> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-080
        public async Task<ApiResult<object>> UpdateExpirationSettingsAsync(int days, bool enabled)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PutAsJsonAsync("settings/expiration", new
                {
                    expiration_alert_days = days,
                    expiration_alert_enabled = enabled
                }, JsonOptions.Default);

                if (!response.IsSuccessStatusCode)
                    return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };
                return new ApiResult<object> { Success = true };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<object> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-081
        public async Task<ApiResult<object>> UpdateStockSettingsAsync(bool enabled, int frequencyHours, string startTime, string endTime)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PutAsJsonAsync("settings/stock", new
                {
                    stock_alert_enabled = enabled,
                    stock_alert_frequency_hours = frequencyHours,
                    stock_alert_start_time = startTime,
                    stock_alert_end_time = endTime
                }, JsonOptions.Default);

                if (!response.IsSuccessStatusCode)
                    return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };
                return new ApiResult<object> { Success = true };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<object> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-083
        public async Task<ApiResult<object>> UpdateReporteZScheduleAsync(string hour)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PutAsJsonAsync("settings/reporte-z-schedule", new
                {
                    reporte_z_hour = hour
                }, JsonOptions.Default);

                if (!response.IsSuccessStatusCode)
                    return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };
                return new ApiResult<object> { Success = true };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<object> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-084
        public async Task<ApiResult<BackupResultDto>> CreateBackupAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsync("settings/backup", null);
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<BackupResultDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<BackupResultDto>(JsonOptions.Default);
                return new ApiResult<BackupResultDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<BackupResultDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
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