using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Bot
{
    public partial class BotChatViewModel : ObservableObject
    {
        private readonly BotService _botService;

        [ObservableProperty] private bool isOpen;
        [ObservableProperty] private ObservableCollection<BotChatMessage> messages = new();
        [ObservableProperty] private string inputText = string.Empty;
        [ObservableProperty] private bool isBusy;

        public BotChatViewModel(BotService botService)
        {
            _botService = botService;
        }

        [RelayCommand]
        private async Task Toggle()
        {
            IsOpen = !IsOpen;

            // Al abrir por primera vez en la sesión, carga alertas
            // pendientes (RF-071/072/073) como si el bot las "empujara".
            if (IsOpen && Messages.Count == 0)
            {
                Messages.Add(new BotChatMessage
                {
                    Text = "👋 ¡Hola! Soy el asistente de Droguería Mickey. Escribe *ayuda* para ver los comandos disponibles.",
                    IsFromUser = false
                });

                IsBusy = true;
                var alertsResult = await _botService.GetPendingAlertsAsync();
                IsBusy = false;

                if (alertsResult.Success && alertsResult.Data != null)
                {
                    foreach (var alert in alertsResult.Data.Alerts)
                    {
                        Messages.Add(new BotChatMessage { Text = alert, IsFromUser = false });
                    }
                }
            }
        }

        [RelayCommand]
        private async Task Send()
        {
            if (string.IsNullOrWhiteSpace(InputText)) return;

            var command = InputText.Trim();
            Messages.Add(new BotChatMessage { Text = command, IsFromUser = true });
            InputText = string.Empty;

            IsBusy = true;
            var result = await _botService.ExecuteCommandAsync(command);
            IsBusy = false;

            var responseText = result.Success && result.Data != null
                ? result.Data.Result
                : (result.ErrorMessage ?? "No se pudo procesar el comando");

            Messages.Add(new BotChatMessage { Text = responseText, IsFromUser = false });
        }

        // Botones de acceso rápido para comandos fijos (sin escribir).
        [RelayCommand]
        private async Task QuickAyuda() => await SendQuick("ayuda");

        [RelayCommand]
        private async Task QuickProductosBajos() => await SendQuick("productos bajos");

        private async Task SendQuick(string command)
        {
            Messages.Add(new BotChatMessage { Text = command, IsFromUser = true });

            IsBusy = true;
            var result = await _botService.ExecuteCommandAsync(command);
            IsBusy = false;

            var responseText = result.Success && result.Data != null
                ? result.Data.Result
                : (result.ErrorMessage ?? "No se pudo procesar el comando");

            Messages.Add(new BotChatMessage { Text = responseText, IsFromUser = false });
        }
    }
}