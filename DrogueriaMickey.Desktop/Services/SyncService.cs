using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DrogueriaMickey.Desktop.Data;

namespace DrogueriaMickey.Desktop.Services
{
    public enum SyncStatus { Synced, Syncing, Offline, Error }

    public partial class SyncService : ObservableObject
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HttpClient _httpClient;
        private readonly SessionService _session;

        private Timer? _timer;
        private readonly object _lock = new();
        private bool _isRunning;
        private int _consecutiveFailures;

        private static readonly TimeSpan[] BackoffSteps =
        {
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1)
        };
        private static readonly TimeSpan SteadyOfflineInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan FastInterval = TimeSpan.FromSeconds(30);   // mientras hay pendientes
        private static readonly TimeSpan SteadyInterval = TimeSpan.FromMinutes(5); 

        [ObservableProperty] private SyncStatus status = SyncStatus.Syncing;
        [ObservableProperty] private int pendingCount;
        [ObservableProperty] private string statusText = "Verificando conexión...";

        public SyncService(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory, SessionService session)
        {
            _scopeFactory = scopeFactory;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(8);
            _session = session;
        }

        public void Start()
        {
            if (_timer != null) return;

            _timer = new Timer(async _ => await TickAsync(), null, TimeSpan.Zero, FastInterval);

            // Reacciona de inmediato cuando el sistema operativo detecta
            // que volvió la red, en vez de esperar al próximo tick del
            // timer — cumple "si tiene conexión que lo mande de una vez".
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        private async void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            try
            {
                if (e.IsAvailable)
                {
                    await TickAsync(force: true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncService] Error en NetworkAvailabilityChanged: {ex}");
            }
        }

        public async Task SyncNowAsync() => await TickAsync(force: true);

        private async Task TickAsync(bool force = false)
        {
            if (_isRunning && !force) return;
            lock (_lock)
            {
                if (_isRunning) return;
                _isRunning = true;
            }

            try
            {
                await RefreshPendingCountAsync();

                var online = await CheckConnectivityAsync();
                if (!online)
                {
                    _consecutiveFailures++;
                    SetStatus(SyncStatus.Offline);
                    RescheduleBackoff();
                    return;
                }

                _consecutiveFailures = 0;

                if (string.IsNullOrEmpty(_session.Token))
                {
                    SetStatus(PendingCount > 0 ? SyncStatus.Offline : SyncStatus.Synced);
                    RescheduleByPending();
                    return;
                }

                await ProcessPendingEventsAsync();
                RescheduleByPending();
            }
            catch (Exception ex)
            {
                // Nunca dejar el estado "colgado" — si algo inesperado falla,
                // se refleja como error visible en vez de quedarse en el texto
                // inicial para siempre.
                System.Diagnostics.Debug.WriteLine($"[SyncService] Error en TickAsync: {ex}");
                SetStatus(SyncStatus.Error);
                RescheduleBackoff();
            }
            finally
            {
                _isRunning = false;
            }
        }

        private async Task<bool> CheckConnectivityAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync("health", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task ProcessPendingEventsAsync()
        {
            SetStatus(SyncStatus.Syncing);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pending = await db.SyncEvents
                .Where(e => e.Status == "pendiente" || e.Status == "error")
                .OrderBy(e => e.ClientCreatedAt)
                .ToListAsync();

            foreach (var evt in pending)
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _session.Token);

                try
                {
                    object? payloadObject;
                    try { payloadObject = System.Text.Json.JsonSerializer.Deserialize<object>(evt.PayloadJson); }
                    catch { payloadObject = evt.PayloadJson; }

                    var response = await _httpClient.PostAsJsonAsync("sync/events", new
                    {
                        id = evt.Id,
                        entity_type = evt.EntityType,
                        entity_id = evt.EntityId,
                        operation = evt.Operation,
                        payload = payloadObject,
                        client_created_at = DateTime.SpecifyKind(evt.ClientCreatedAt, DateTimeKind.Utc).ToString("o")
                    }, JsonOptions.Default);

                    if (response.IsSuccessStatusCode)
                    {
                        evt.Status = "confirmado";
                    }
                    else if ((int)response.StatusCode is >= 400 and < 500)
                    {
                        evt.Status = "error";
                        evt.ErrorDetail = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        break;
                    }

                    await db.SaveChangesAsync();
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    break;
                }
            }

            await RefreshPendingCountAsync();
            SetStatus(PendingCount > 0 ? SyncStatus.Error : SyncStatus.Synced);
        }

        private async Task RefreshPendingCountAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var count = await db.SyncEvents.CountAsync(e => e.Status == "pendiente" || e.Status == "error");
            Application.Current?.Dispatcher.Invoke(() => PendingCount = count);
        }

        private void SetStatus(SyncStatus newStatus)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Status = newStatus;
                StatusText = newStatus switch
                {
                    SyncStatus.Synced => "Sincronizado",
                    SyncStatus.Syncing => "Sincronizando...",
                    SyncStatus.Offline => PendingCount > 0 ? $"Sin conexión ({PendingCount} pendientes)" : "Sin conexión",
                    SyncStatus.Error => $"Error de sincronización ({PendingCount} por revisar)",
                    _ => "Sin conexión"
                };
            });
        }

        // Mientras haya pendientes, revisa cada 30s (para no demorar la
        // entrega apenas vuelva la red). Sin nada pendiente, baja a cada
        // 1 hora — la cadencia base que pediste.
        private void RescheduleByPending()
        {
            var interval = PendingCount > 0 ? FastInterval : SteadyInterval;
            _timer?.Change(interval, interval);
        }

        private void RescheduleBackoff()
        {
            var index = Math.Min(_consecutiveFailures - 1, BackoffSteps.Length - 1);
            var interval = _consecutiveFailures <= BackoffSteps.Length ? BackoffSteps[index] : SteadyOfflineInterval;
            _timer?.Change(interval, interval);
        }
    }
}