using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DrogueriaMickey.Desktop.Services
{
    public class UserListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly SessionService _session;

        public UserService(HttpClient httpClient, SessionService session)
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

        public async Task<ApiResult<List<UserListItemDto>>> ListUsersAsync()
        {
            AttachToken();
            var response = await _httpClient.GetAsync("users");
            if (!response.IsSuccessStatusCode)
                return new ApiResult<List<UserListItemDto>> { Success = false, ErrorMessage = await ReadError(response) };

            var data = await response.Content.ReadFromJsonAsync<List<UserListItemDto>>(JsonOptions.Default);
            return new ApiResult<List<UserListItemDto>> { Success = true, Data = data };
        }

        public async Task<ApiResult<List<RoleDto>>> ListRolesAsync()
        {
            AttachToken();
            var response = await _httpClient.GetAsync("roles");
            if (!response.IsSuccessStatusCode)
                return new ApiResult<List<RoleDto>> { Success = false, ErrorMessage = await ReadError(response) };

            var data = await response.Content.ReadFromJsonAsync<List<RoleDto>>(JsonOptions.Default);
            return new ApiResult<List<RoleDto>> { Success = true, Data = data };
        }

        public async Task<ApiResult<object>> RegisterUserAsync(string fullName, string documentId, string username, string tempPassword, string roleId)
        {
            AttachToken();
            var response = await _httpClient.PostAsJsonAsync("users", new
            {
                full_name = fullName,
                document_id = documentId,
                username,
                temporary_password = tempPassword,
                role_id = roleId
            }, JsonOptions.Default);

            if (!response.IsSuccessStatusCode)
                return new ApiResult<object> { Success = false, ErrorMessage = await ReadError(response) };

            return new ApiResult<object> { Success = true };
        }

        public async Task<ApiResult<object>> UpdateUserAsync(string id, string fullName, string username, string roleId, bool isActive)
        {
            AttachToken();
            var response = await _httpClient.PutAsJsonAsync($"users/{id}", new
            {
                full_name = fullName,
                username,
                role_id = roleId,
                is_active = isActive
            }, JsonOptions.Default);

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