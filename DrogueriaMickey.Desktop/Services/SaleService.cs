using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DrogueriaMickey.Desktop.Services
{
    public class SaleItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string MedicineId { get; set; } = string.Empty;
        public string MedicineName { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
        public string BatchCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string SaleUnit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public int ReturnedQuantity { get; set; } // nuevo

        public bool IsFullyReturned => ReturnedQuantity >= Quantity;
        public string ReturnStatusLabel => IsFullyReturned ? "Devuelto" : (ReturnedQuantity > 0 ? $"Parcial ({ReturnedQuantity}/{Quantity})" : "");
    }

    public class SaleListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public int SaleNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public bool HasReturns { get; set; } // nuevo

        public string StatusDisplay => HasReturns ? $"{Status} (con devolución)" : Status;
    }

    public class SalesDateRangeSummaryDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public int SalesCount { get; set; }
        public decimal TotalSold { get; set; }
        public decimal AverageSale { get; set; }
        public List<SaleListItemDto> Sales { get; set; } = new();
    }

    public class ReceiptItemDto
    {
        public string MedicineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string SaleUnit { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }
        public decimal BasePrice { get; set; }
        public decimal TaxAmount { get; set; }
        public bool HasIva { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class SalesFilteredResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public SalesFilteredDataDto? Data { get; set; }
    }

    public class SalesFilteredDataDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<SaleListItemDto> Sales { get; set; } = new();
    }
    public class ReceiptDto
    {
        public int SaleNumber { get; set; }
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public List<ReceiptItemDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public decimal TotalTax { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal? AmountReceived { get; set; }
        public decimal? ChangeGiven { get; set; }
    }

    public class SaleDto
    {
        public string Id { get; set; } = string.Empty;
        public int SaleNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<SaleItemDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }

        public string? PaymentMethod { get; set; }
        public decimal? AmountReceived { get; set; }
        public decimal? ChangeGiven { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? FinalizedAt { get; set; }
    }

    public class StartSaleResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public int SaleNumber { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class FinalizeCashResultDto
    {
        public string Message { get; set; } = string.Empty;
        public string SaleId { get; set; } = string.Empty;
        public int SaleNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ChangeGiven { get; set; }
    }

    public class SaleService
    {
        private readonly HttpClient _httpClient;
        private readonly SessionService _session;

        private const string ConnectionErrorMessage = "No se pudo conectar con el servidor";

        public SaleService(HttpClient httpClient, SessionService session)
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

        public async Task<ApiResult<ReceiptDto>> GetReceiptAsync(string saleId)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync($"sales/{saleId}/receipt");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<ReceiptDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<ReceiptDto>(JsonOptions.Default);
                return new ApiResult<ReceiptDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<ReceiptDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-031 + RF-032
        public async Task<ApiResult<SaleDto>> StartSaleAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsync("sales", null);
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<SaleDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<SaleDto>(JsonOptions.Default);
                return new ApiResult<SaleDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<SaleDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        public async Task<ApiResult<SaleDto>> GetSaleAsync(string saleId)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync($"sales/{saleId}");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<SaleDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<SaleDto>(JsonOptions.Default);
                return new ApiResult<SaleDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<SaleDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-044
        public async Task<ApiResult<List<SaleListItemDto>>> ListSalesAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync("sales");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<List<SaleListItemDto>> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<List<SaleListItemDto>>(JsonOptions.Default);
                return new ApiResult<List<SaleListItemDto>> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<List<SaleListItemDto>> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-047
        public async Task<ApiResult<SalesDateRangeSummaryDto>> GetSalesByDateRangeAsync(string from, string to)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync($"sales/range?from={from}&to={to}");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<SalesDateRangeSummaryDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<SalesDateRangeSummaryDto>(JsonOptions.Default);
                return new ApiResult<SalesDateRangeSummaryDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<SalesDateRangeSummaryDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-033
        public async Task<ApiResult<SaleDto>> AddItemByBarcodeAsync(string saleId, string barcode, int quantity, string saleUnit)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync($"sales/{saleId}/items", new
                {
                    barcode,
                    quantity,
                    sale_unit = saleUnit
                }, JsonOptions.Default);

                if (!response.IsSuccessStatusCode)
                    return new ApiResult<SaleDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<SaleDto>(JsonOptions.Default);
                return new ApiResult<SaleDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<SaleDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        public async Task<ApiResult<SaleDto>> AddItemByMedicineIdAsync(string saleId, string medicineId, int quantity)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync($"sales/{saleId}/items", new
                {
                    medicine_id = medicineId,
                    quantity
                }, JsonOptions.Default);

                if (!response.IsSuccessStatusCode)
                    return new ApiResult<SaleDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<SaleDto>(JsonOptions.Default);
                return new ApiResult<SaleDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<SaleDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-035
        public async Task<ApiResult<SaleDto>> RemoveItemAsync(string saleId, string itemId)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.DeleteAsync($"sales/{saleId}/items/{itemId}");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<SaleDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<SaleDto>(JsonOptions.Default);
                return new ApiResult<SaleDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<SaleDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-036
        public async Task<ApiResult<object>> CancelSaleAsync(string saleId)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsync($"sales/{saleId}/cancel", null);
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };

                return new ApiResult<object> { Success = true };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<object> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-039 + RF-041
        public async Task<ApiResult<FinalizeCashResultDto>> FinalizeCashAsync(string saleId, decimal amountReceived)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync($"sales/{saleId}/finalize/cash", new
                {
                    amount_received = amountReceived
                }, JsonOptions.Default);

                if (!response.IsSuccessStatusCode)
                    return new ApiResult<FinalizeCashResultDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<FinalizeCashResultDto>(JsonOptions.Default);
                return new ApiResult<FinalizeCashResultDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<FinalizeCashResultDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-034
        public async Task<ApiResult<SaleDto>> UpdateItemQuantityAsync(string saleId, string itemId, int newQuantity)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PutAsJsonAsync($"sales/{saleId}/items/{itemId}", new
                {
                    quantity = newQuantity
                }, JsonOptions.Default);

                if (!response.IsSuccessStatusCode)
                    return new ApiResult<SaleDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<SaleDto>(JsonOptions.Default);
                return new ApiResult<SaleDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<SaleDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-044: filtro combinado (número de venta, fecha, usuario) — usado
        // junto con el rango de fechas de RF-047 en la misma vista.
        public async Task<ApiResult<SalesFilteredDataDto>> ListSalesFilteredAsync(string? startDate = null, string? endDate = null, string? userId = null)
        {
            try
            {
                AttachToken();
                var query = new List<string>();
                if (!string.IsNullOrWhiteSpace(startDate)) query.Add($"start_date={startDate}");
                if (!string.IsNullOrWhiteSpace(endDate)) query.Add($"end_date={endDate}");
                if (!string.IsNullOrWhiteSpace(userId)) query.Add($"user_id={userId}");
                var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

                var response = await _httpClient.GetAsync($"sales{queryString}");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<SalesFilteredDataDto> { Success = false, ErrorMessage = await ReadError(response) };

                var wrapper = await response.Content.ReadFromJsonAsync<SalesFilteredResponseDto>(JsonOptions.Default);
                return new ApiResult<SalesFilteredDataDto> { Success = true, Data = wrapper?.Data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<SalesFilteredDataDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-045: detalle completo de una venta (reutiliza GetSale).
        public async Task<ApiResult<SaleDto>> GetSaleDetailAsync(string saleId)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync($"sales/{saleId}");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<SaleDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<SaleDto>(JsonOptions.Default);
                return new ApiResult<SaleDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<SaleDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-046: anular una venta finalizada, con motivo obligatorio.
        public async Task<ApiResult<object>> VoidSaleAsync(string saleId, string reason)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync($"sales/{saleId}/void", new
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

        // RF-040 + RF-041
        public async Task<ApiResult<object>> FinalizeNequiAsync(string saleId, decimal amountPaid)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync($"sales/{saleId}/finalize/nequi", new
                {
                    amount_paid = amountPaid
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