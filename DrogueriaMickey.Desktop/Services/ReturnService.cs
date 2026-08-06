using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DrogueriaMickey.Desktop.Services
{
    public class ReturnDto
    {
        public string Id { get; set; } = string.Empty;
        public string SaleId { get; set; } = string.Empty;
        public int SaleNumber { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsVoided { get; set; }
        public string VoidedReason { get; set; } = string.Empty;
        public string VoidedAt { get; set; } = string.Empty;
        public string VoidedByName { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class ReturnService
    {
        // RF-092: motivos predefinidos, deben coincidir exacto con el backend.
        public static readonly List<string> ValidReasons = new()
        {
            "Error en la venta",
            "Error en el producto entregado",
            "Producto defectuoso",
            "Otro"
        };

        private readonly HttpClient _httpClient;
        private readonly SessionService _session;

        private const string ConnectionErrorMessage = "No se pudo conectar con el servidor";

        public ReturnService(HttpClient httpClient, SessionService session)
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

        // RF-089 + RF-090 + RF-092 (solo Administrador, requiere caja abierta)
        public async Task<ApiResult<object>> RegisterReturnAsync(string saleId, string saleItemId, int quantity, string reason)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync($"sales/{saleId}/returns", new
                {
                    sale_item_id = saleItemId,
                    quantity,
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

        // RF-094: anular devolución (solo Administrador).
        public async Task<ApiResult<object>> VoidReturnAsync(string returnId, string reason)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync($"returns/{returnId}/void", new
                {
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

        // RF-093 + RNF-027
        public async Task<ApiResult<List<ReturnDto>>> ListReturnsAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync("returns");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<List<ReturnDto>> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<List<ReturnDto>>(JsonOptions.Default);
                return new ApiResult<List<ReturnDto>> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<List<ReturnDto>> { Success = false, ErrorMessage = ConnectionErrorMessage };
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