using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DrogueriaMickey.Desktop.Services
{
    public class BotQueryResultDto
    {
        public string Command { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
    }

    public class BotAlertsResponseDto
    {
        public List<string> Alerts { get; set; } = new();
    }

    public class BotService
    {
        private readonly HttpClient _httpClient;
        private readonly SessionService _session;

        private const string ConnectionErrorMessage = "No se pudo conectar con el servidor";

        public BotService(HttpClient httpClient, SessionService session)
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

        public class BotConfigDto
        {
            public bool IsEnabled { get; set; }
            public string WelcomeMessage { get; set; } = string.Empty;
        }

        public class BotQueryLogDto
        {
            public string Username { get; set; } = string.Empty;
            public string Command { get; set; } = string.Empty;
            public string Result { get; set; } = string.Empty;
            public string CreatedAt { get; set; } = string.Empty;
        }

        public class BotAuthorizedUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string CreatedAt { get; set; } = string.Empty;
        }

        // RF-074/075/076/077
        public async Task<ApiResult<BotQueryResultDto>> ExecuteCommandAsync(string command)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync("bot/query", new { command }, JsonOptions.Default);
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<BotQueryResultDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<BotQueryResultDto>(JsonOptions.Default);
                return new ApiResult<BotQueryResultDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<BotQueryResultDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-078
        public async Task<ApiResult<List<BotQueryLogDto>>> ListQueryLogsAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync("bot/logs");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<List<BotQueryLogDto>> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<List<BotQueryLogDto>>(JsonOptions.Default);
                return new ApiResult<List<BotQueryLogDto>> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<List<BotQueryLogDto>> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        // RF-071/072/073
        public async Task<ApiResult<BotAlertsResponseDto>> GetPendingAlertsAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync("bot/alerts");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<BotAlertsResponseDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<BotAlertsResponseDto>(JsonOptions.Default);
                return new ApiResult<BotAlertsResponseDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<BotAlertsResponseDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
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

        // RF-082
        public async Task<ApiResult<BotConfigDto>> GetConfigAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync("bot/config");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<BotConfigDto> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<BotConfigDto>(JsonOptions.Default);
                return new ApiResult<BotConfigDto> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<BotConfigDto> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        public async Task<ApiResult<object>> UpdateConfigAsync(bool isEnabled, string welcomeMessage)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PutAsJsonAsync("bot/config", new
                {
                    is_enabled = isEnabled,
                    welcome_message = welcomeMessage
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

        // RF-070
        public async Task<ApiResult<List<BotAuthorizedUserDto>>> ListAuthorizedAsync()
        {
            try
            {
                AttachToken();
                var response = await _httpClient.GetAsync("bot/access");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<List<BotAuthorizedUserDto>> { Success = false, ErrorMessage = await ReadError(response) };

                var data = await response.Content.ReadFromJsonAsync<List<BotAuthorizedUserDto>>(JsonOptions.Default);
                return new ApiResult<List<BotAuthorizedUserDto>> { Success = true, Data = data };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<List<BotAuthorizedUserDto>> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        public async Task<ApiResult<object>> GrantAccessAsync(string userId)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.PostAsJsonAsync("bot/access", new { user_id = userId }, JsonOptions.Default);
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };
                return new ApiResult<object> { Success = true };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<object> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        public async Task<ApiResult<object>> RevokeAccessAsync(string id)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.DeleteAsync($"bot/access/{id}");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };
                return new ApiResult<object> { Success = true };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<object> { Success = false, ErrorMessage = ConnectionErrorMessage };
            }
        }

        private class ErrorResponse
        {
            public string? Error { get; set; }
        }
    }
}