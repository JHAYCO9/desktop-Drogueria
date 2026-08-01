using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;


namespace DrogueriaMickey.Desktop.Services
{
    public class CashRegisterStatusDto
    {
        public bool IsOpen { get; set; }
        public string? Id { get; set; }
        public decimal OpeningAmount { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal TotalNequiSales { get; set; }
        public decimal CurrentBalance { get; set; }
        public string? OpenedAt { get; set; }
        public string? OpenedByName { get; set; }
    }

    public class OpenCashRegisterResultDto
    {
        public string Id { get; set; } = string.Empty;
        public decimal OpeningAmount { get; set; }
        public string OpenedAt { get; set; } = string.Empty;
    }

    public class CloseCashRegisterResultDto
    {
        public decimal ExpectedAmount { get; set; }
        public decimal CountedAmount { get; set; }
        public decimal DifferenceAmount { get; set; }
        public string ClosedAt { get; set; } = string.Empty;
    }

    public class CashMovementDto
    {
        public string Id { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class ReporteZDto
    {
        public string BusinessName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Nit { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string DateTime { get; set; } = string.Empty;
        public string RegistradoraNo { get; set; } = string.Empty;
        public int RegistroInicial { get; set; }
        public int RegistroFinal { get; set; }
        public int ZNumero { get; set; }
        public decimal TotalExentos { get; set; }
        public int ExemptCount { get; set; }

        [JsonPropertyName("total_gravados_19")]
        public decimal TotalGravados19 { get; set; }

        [JsonPropertyName("total_iva_19")]
        public decimal TotalIva19 { get; set; }

        [JsonPropertyName("taxed_count_19")]
        public int TaxedCount19 { get; set; }

        public int NumeroVentas { get; set; }
        public decimal TotalVendido { get; set; }
    }

    public class CashRegisterService
    {
        private readonly HttpClient _httpClient;
        private readonly SessionService _session;

        private const string ConnectionErrorMessage = "No se pudo conectar con el servidor";

        public CashRegisterService(HttpClient httpClient, SessionService session)
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

        // RF-048
        public async Task<ApiResult<OpenCashRegisterResultDto>> OpenAsync(decimal openingAmount)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync("cash-register/open", new
                {
                    opening_amount = openingAmount
                }, JsonOptions.Default);

                if (!response.IsSuccessStatusCode)
                    return new ApiResult<OpenCashRegisterResultDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<OpenCashRegisterResultDto>(JsonOptions.Default);
                return new ApiResult<OpenCashRegisterResultDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<OpenCashRegisterResultDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        public async Task<ApiResult<ReporteZDto>> GetReporteZAsync(string cashRegisterId)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync($"cash-register/{cashRegisterId}/reporte-z");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<ReporteZDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<ReporteZDto>(JsonOptions.Default);
                return new ApiResult<ReporteZDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<ReporteZDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-049
        public async Task<ApiResult<CashRegisterStatusDto>> GetStatusAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync("cash-register/status");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<CashRegisterStatusDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<CashRegisterStatusDto>(JsonOptions.Default);
                return new ApiResult<CashRegisterStatusDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<CashRegisterStatusDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-052 + RF-053 + RF-054
        public async Task<ApiResult<CloseCashRegisterResultDto>> CloseAsync(decimal countedAmount, string differenceReason)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync("cash-register/close", new
                {
                    counted_amount = countedAmount,
                    difference_reason = differenceReason
                }, JsonOptions.Default);

                if (!response.IsSuccessStatusCode)
                    return new ApiResult<CloseCashRegisterResultDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<CloseCashRegisterResultDto>(JsonOptions.Default);
                return new ApiResult<CloseCashRegisterResultDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<CloseCashRegisterResultDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-051
        public async Task<ApiResult<List<CashMovementDto>>> GetMovementsAsync(string? userId = null, string? from = null, string? to = null)
        {
            try
            {
                AttachToken();
                var query = new List<string>();
                if (!string.IsNullOrWhiteSpace(userId)) query.Add($"user_id={userId}");
                if (!string.IsNullOrWhiteSpace(from)) query.Add($"from={from}");
                if (!string.IsNullOrWhiteSpace(to)) query.Add($"to={to}");
                var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

                var response = await _httpClient.GetAsync($"cash-register/movements{queryString}");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<List<CashMovementDto>> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<List<CashMovementDto>>(JsonOptions.Default);
                return new ApiResult<List<CashMovementDto>> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<List<CashMovementDto>> { Success = false, ErrorMessage = ConnectionErrorMessage };
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