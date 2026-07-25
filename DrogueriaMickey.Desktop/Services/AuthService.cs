using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DrogueriaMickey.Desktop.Data;
using DrogueriaMickey.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace DrogueriaMickey.Desktop.Services
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public bool WasOffline { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _db;
        private readonly SessionService _session;

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

        public AuthService(HttpClient httpClient, AppDbContext db, SessionService session)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            _db = db;
            _session = session;
        }

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            var localUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (localUser != null && localUser.LockedUntil.HasValue && localUser.LockedUntil.Value > DateTime.UtcNow)
            {
                var remaining = localUser.LockedUntil.Value - DateTime.UtcNow;
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = $"Cuenta bloqueada temporalmente, intenta de nuevo en {Math.Ceiling(remaining.TotalMinutes)} minuto(s)"
                };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("auth/login", new { username, password });

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<LoginApiResponse>(JsonOptions.Default);
                    var hash = BCrypt.Net.BCrypt.HashPassword(password);

                    if (localUser == null)
                    {
                        localUser = new User
                        {
                            Id = Guid.NewGuid(),
                            Username = username,
                            PasswordHash = hash,
                            IsActive = true,
                            FailedLoginCount = 0,
                            LockedUntil = null,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.Users.Add(localUser);
                    }
                    else
                    {
                        localUser.PasswordHash = hash;
                        localUser.FailedLoginCount = 0;
                        localUser.LockedUntil = null;
                        localUser.UpdatedAt = DateTime.UtcNow;
                    }

                    localUser.RoleName = body?.RoleName ?? string.Empty;

                    RegisterLoginSyncEvent(localUser.Id, isOffline: false);
                    await _db.SaveChangesAsync();

                    _session.SetSession(localUser.Id, username, localUser.RoleId, localUser.RoleName, body?.Token, isOffline: false);
                    return new LoginResult { Success = true, WasOffline = false };
                }

                if (localUser != null)
                {
                    await RegisterFailedAttemptAsync(localUser);
                }

                var errorBody = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions.Default);
                return new LoginResult { Success = false, ErrorMessage = errorBody?.Error ?? "Usuario o contraseña incorrectos" };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return await TryOfflineLogin(username, password, localUser);
            }
            catch (Exception ex)
            {
                // Cualquier otro error (ej. lock de SQLite) se reporta en vez de
                // dejar el login colgado indefinidamente.
                return new LoginResult { Success = false, ErrorMessage = $"Error al iniciar sesión: {ex.Message}" };
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "auth/logout");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);
                await _httpClient.SendAsync(request);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                // sin conexión: no bloqueamos el cierre de sesión local
            }
            finally
            {
                _session.Clear();
            }
        }

        private async Task<LoginResult> TryOfflineLogin(string username, string password, User? localUser)
        {
            if (localUser == null)
            {
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = "Sin conexión y este usuario nunca inició sesión antes en este equipo"
                };
            }

            if (!localUser.IsActive)
            {
                return new LoginResult { Success = false, ErrorMessage = "Usuario desactivado" };
            }

            bool validPassword;
            try
            {
                validPassword = BCrypt.Net.BCrypt.Verify(password, localUser.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                validPassword = false;
            }

            if (!validPassword)
            {
                var locked = await RegisterFailedAttemptAsync(localUser);
                if (locked)
                {
                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Cuenta bloqueada temporalmente por múltiples intentos fallidos"
                    };
                }
                return new LoginResult { Success = false, ErrorMessage = "Usuario o contraseña incorrectos" };
            }

            localUser.FailedLoginCount = 0;
            localUser.LockedUntil = null;
            RegisterLoginSyncEvent(localUser.Id, isOffline: true);
            await _db.SaveChangesAsync();

            _session.SetSession(localUser.Id, username, localUser.RoleId, localUser.RoleName, token: null, isOffline: true);
            return new LoginResult { Success = true, WasOffline = true };
        }

        private async Task<bool> RegisterFailedAttemptAsync(User user)
        {
            user.FailedLoginCount += 1;
            bool locked = user.FailedLoginCount >= MaxFailedAttempts;

            if (locked)
            {
                user.LockedUntil = DateTime.UtcNow.Add(LockDuration);

                _db.SyncEvents.Add(new SyncEvent
                {
                    Id = Guid.NewGuid(),
                    EntityType = "user_account_locked",
                    EntityId = user.Id,
                    Operation = "create",
                    PayloadJson = JsonSerializer.Serialize(new { user_id = user.Id, locked_at = DateTime.UtcNow }),
                    Status = "pendiente",
                    ClientCreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return locked;
        }

        private void RegisterLoginSyncEvent(Guid userId, bool isOffline)
        {
            var payload = JsonSerializer.Serialize(new
            {
                user_id = userId,
                logged_in_at = DateTime.UtcNow,
                offline = isOffline
            });

            _db.SyncEvents.Add(new SyncEvent
            {
                Id = Guid.NewGuid(),
                EntityType = "user_login",
                EntityId = userId,
                Operation = "create",
                PayloadJson = payload,
                Status = "pendiente",
                ClientCreatedAt = DateTime.UtcNow
            });
        }

        private class LoginApiResponse
        {
            public string? Token { get; set; }
            public string? RoleName { get; set; }
        }

        private class ErrorResponse
        {
            public string? Error { get; set; }
        }
    }
}