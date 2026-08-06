using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DrogueriaMickey.Desktop.Services
{
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
        public string DiscrepancyType { get; set; } = string.Empty; // EXCESS | DEFICIT | NONE
        public string ClosedAt { get; set; } = string.Empty;
    }

    public class ManualMovementItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    // RF-019: estado detallado de caja en tiempo real.
    public class CashStatusDetailDto
    {
        public bool IsOpen { get; set; }
        public string? SessionId { get; set; }
        public decimal MontoInicial { get; set; }
        public string? FechaApertura { get; set; }
        public string? AbiertoPor { get; set; }
        public decimal TotalVentasEfectivo { get; set; }
        public decimal TotalVentasOtrosMedios { get; set; }

        [JsonPropertyName("total_ingresos_manuales")]
        public decimal TotalIngresosManual { get; set; }

        [JsonPropertyName("total_egresos_manuales")]
        public decimal TotalEgresosManual { get; set; }

        [JsonPropertyName("saldo_teorico_actual")]
        public decimal SaldoTeorico { get; set; }

        public List<ManualMovementItemDto> ListaMovimientos { get; set; } = new();
    }

    public class CashMovementDto
    {
        public string Id { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public string MovementCategory { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    // RF-056: historial de cierres.
    public class ClosedRegisterDto
    {
        public string Id { get; set; } = string.Empty;
        public string OpenedAt { get; set; } = string.Empty;
        public string ClosedAt { get; set; } = string.Empty;
        public string OpenedByName { get; set; } = string.Empty;
        public string ClosedByName { get; set; } = string.Empty;
        public decimal OpeningAmount { get; set; }
        public decimal CountedAmount { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal DifferenceAmount { get; set; }
        public string DiscrepancyType { get; set; } = string.Empty;
        public string DifferenceReason { get; set; } = string.Empty;
        public decimal TotalVentasEfectivo { get; set; }
        public decimal TotalOtrosMedios { get; set; }

        [JsonPropertyName("total_ingresos_manuales")]
        public decimal TotalIngresos { get; set; }

        [JsonPropertyName("total_egresos_manuales")]
        public decimal TotalEgresos { get; set; }

        public decimal SaldoTeorico { get; set; }
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

        // RF-048/RF-016
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

        // RF-019: estado detallado (reemplaza el estado simple anterior).
        public async Task<ApiResult<CashStatusDetailDto>> GetDetailedStatusAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync("cash-register/status");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<CashStatusDetailDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<CashStatusDetailDto>(JsonOptions.Default);
                return new ApiResult<CashStatusDetailDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<CashStatusDetailDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
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

        // RF-018: movimiento manual de caja (ingreso o egreso).
        public async Task<ApiResult<object>> RegisterManualMovementAsync(string type, decimal amount, string reason)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync("cash-register/movements", new
                {
                    type,
                    amount,
                    reason
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

        // RF-056: historial de cierres.
        public async Task<ApiResult<List<ClosedRegisterDto>>> GetHistoryAsync(string? from = null, string? to = null, string? userId = null)
        {
            try
            {
                AttachToken();
                var query = new List<string>();
                if (!string.IsNullOrWhiteSpace(from)) query.Add($"from={from}");
                if (!string.IsNullOrWhiteSpace(to)) query.Add($"to={to}");
                if (!string.IsNullOrWhiteSpace(userId)) query.Add($"user_id={userId}");
                var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

                var response = await _httpClient.GetAsync($"cash-register/history{queryString}");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<List<ClosedRegisterDto>> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<List<ClosedRegisterDto>>(JsonOptions.Default);
                return new ApiResult<List<ClosedRegisterDto>> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<List<ClosedRegisterDto>> { Success = false, ErrorMessage = ConnectionErrorMessage };
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