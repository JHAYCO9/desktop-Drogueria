using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DrogueriaMickey.Desktop.Services
{
    public class MedicineDto
    {
        public string Id { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Laboratory { get; set; } = string.Empty;
        public string Concentration { get; set; } = string.Empty;
        public string Presentation { get; set; } = string.Empty;
        public decimal PricePerPill { get; set; }
        public decimal PricePerSeal { get; set; }
        public decimal PricePerBox { get; set; }
        public int MinStock { get; set; }
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class BatchDto
    {
        public string Id { get; set; } = string.Empty;
        public string MedicineId { get; set; } = string.Empty;
        public string MedicineName { get; set; } = string.Empty;

        public string BatchCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string ReceivedAt { get; set; } = string.Empty;
        public string ExpirationDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // vigente, proximo_a_vencer, vencido
        public int DaysRemaining { get; set; }
    }

    public class CriticalMedicineDto
    {
        public MedicineDto Medicine { get; set; } = new();
        public int CurrentStock { get; set; }
        public int MinStock { get; set; }
        public bool IsCritical { get; set; }
    }

    public class MedicineService
    {
        private readonly HttpClient _httpClient;
        private readonly SessionService _session;

        public MedicineService(HttpClient httpClient, SessionService session)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new System.Uri(AppConfig.ApiBaseUrl);
            _session = session;
        }

        private void AttachToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _session.Token);
        }

        // RF-015
        public async Task<ApiResult<BatchRegisterResponse>> RegisterBatchAsync(string medicineId, int quantity, string receivedAt, string expirationDate)
        {
            AttachToken();
            var response = await _httpClient.PostAsJsonAsync("batches", new
            {
                medicine_id = medicineId,
                quantity,
                received_at = receivedAt,
                expiration_date = expirationDate
            }, JsonOptions.Default);

            if (!response.IsSuccessStatusCode)
                return new ApiResult<BatchRegisterResponse> { Success = false, ErrorMessage = await ReadError(response) };

            var data = await response.Content.ReadFromJsonAsync<BatchRegisterResponse>(JsonOptions.Default);
            return new ApiResult<BatchRegisterResponse> { Success = true, Data = data };
        }

        // RF-016
        public async Task<ApiResult<object>> RegisterIngresoAsync(string batchId, int quantity)
        {
            AttachToken();
            var response = await _httpClient.PostAsJsonAsync("inventory/ingreso", new
            {
                batch_id = batchId,
                quantity
            }, JsonOptions.Default);

            if (!response.IsSuccessStatusCode)
                return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };

            return new ApiResult<object> { Success = true };
        }

        // RF-018
        public async Task<ApiResult<List<BatchDto>>> ListBatchesByMedicineAsync(string medicineId)
        {
            AttachToken();
            var response = await _httpClient.GetAsync($"medicines/{medicineId}/batches");
            if (!response.IsSuccessStatusCode)
                return new ApiResult<List<BatchDto>> { Success = false, ErrorMessage = await ReadError(response) };

            var data = await response.Content.ReadFromJsonAsync<List<BatchDto>>(JsonOptions.Default);
            return new ApiResult<List<BatchDto>> { Success = true, Data = data };
        }

        // RF-021
        public async Task<ApiResult<List<CriticalMedicineDto>>> ListCriticalMedicinesAsync()
        {
            AttachToken();
            var response = await _httpClient.GetAsync("medicines/critical");
            if (!response.IsSuccessStatusCode)
                return new ApiResult<List<CriticalMedicineDto>> { Success = false, ErrorMessage = await ReadError(response) };

            var data = await response.Content.ReadFromJsonAsync<List<CriticalMedicineDto>>(JsonOptions.Default);
            return new ApiResult<List<CriticalMedicineDto>> { Success = true, Data = data };
        }

        // RF-022
        public async Task<ApiResult<object>> UpdateAlertDaysAsync(string medicineId, int days)
        {
            AttachToken();
            var request = new HttpRequestMessage(HttpMethod.Patch, $"medicines/{medicineId}/alert-days")
            {
                Content = JsonContent.Create(new { days }, options: JsonOptions.Default)
            };
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };

            return new ApiResult<object> { Success = true };
        }

        // RF-023
        public async Task<ApiResult<List<BatchDto>>> ListExpiringSoonAsync(int days = 30)
        {
            AttachToken();
            var response = await _httpClient.GetAsync($"batches/expiring?days={days}");
            if (!response.IsSuccessStatusCode)
                return new ApiResult<List<BatchDto>> { Success = false, ErrorMessage = await ReadError(response) };

            var data = await response.Content.ReadFromJsonAsync<List<BatchDto>>(JsonOptions.Default);
            return new ApiResult<List<BatchDto>> { Success = true, Data = data };
        }

        // RF-024
        public async Task<ApiResult<List<BatchDto>>> ListExpiredAsync()
        {
            AttachToken();
            var response = await _httpClient.GetAsync("batches/expired");
            if (!response.IsSuccessStatusCode)
                return new ApiResult<List<BatchDto>> { Success = false, ErrorMessage = await ReadError(response) };

            var data = await response.Content.ReadFromJsonAsync<List<BatchDto>>(JsonOptions.Default);
            return new ApiResult<List<BatchDto>> { Success = true, Data = data };
        }

        public class BatchRegisterResponse
        {
            public string Id { get; set; } = string.Empty;
            public string BatchCode { get; set; } = string.Empty;
        }

        public async Task<ApiResult<List<MedicineDto>>> ListMedicinesAsync()
        {
            AttachToken();
            var response = await _httpClient.GetAsync("medicines");
            if (!response.IsSuccessStatusCode)
                return new ApiResult<List<MedicineDto>> { Success = false, ErrorMessage = await ReadError(response) };

            var data = await response.Content.ReadFromJsonAsync<List<MedicineDto>>(JsonOptions.Default);
            return new ApiResult<List<MedicineDto>> { Success = true, Data = data };
        }

        // RF-027: busca por código de barras. Esto es exactamente lo que se
        // dispara cuando el lector físico "escribe" el código y presiona Enter.
        public async Task<ApiResult<MedicineDto>> GetByBarcodeAsync(string barcode)
        {
            AttachToken();
            var response = await _httpClient.GetAsync($"medicines/barcode/{barcode}");
            if (!response.IsSuccessStatusCode)
                return new ApiResult<MedicineDto> { Success = false, ErrorMessage = await ReadError(response) };

            var data = await response.Content.ReadFromJsonAsync<MedicineDto>(JsonOptions.Default);
            return new ApiResult<MedicineDto> { Success = true, Data = data };
        }

        public async Task<ApiResult<object>> RegisterMedicineAsync(
            string barcode, string name, string laboratory, string concentration, string presentation,
            decimal pricePerPill, decimal pricePerSeal, decimal pricePerBox, int minStock)
        {
            AttachToken();
            var response = await _httpClient.PostAsJsonAsync("medicines", new
            {
                barcode,
                name,
                laboratory,
                concentration,
                presentation,
                price_per_pill = pricePerPill,
                price_per_seal = pricePerSeal,
                price_per_box = pricePerBox,
                min_stock = minStock
            }, JsonOptions.Default);

            if (!response.IsSuccessStatusCode)
                return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };

            return new ApiResult<object> { Success = true };
        }

        public async Task<ApiResult<object>> UpdateMinStockAsync(string medicineId, int minStock)
        {
            AttachToken();
            var request = new HttpRequestMessage(HttpMethod.Patch, $"medicines/{medicineId}/min-stock")
            {
                Content = JsonContent.Create(new { min_stock = minStock }, options: JsonOptions.Default)
            };
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };

            return new ApiResult<object> { Success = true };
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