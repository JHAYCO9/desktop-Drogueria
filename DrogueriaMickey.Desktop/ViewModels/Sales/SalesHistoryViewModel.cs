using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Sales
{
    public partial class SalesHistoryViewModel : ObservableObject
    {
        private readonly SaleService _saleService;

        [ObservableProperty] private ObservableCollection<SaleListItemDto> sales = new();
        [ObservableProperty] private string fromDate = System.DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
        [ObservableProperty] private string toDate = System.DateTime.Today.ToString("yyyy-MM-dd");
        [ObservableProperty] private int salesCount;
        [ObservableProperty] private decimal totalSold;
        [ObservableProperty] private decimal averageSale;
        [ObservableProperty] private bool isFiltered;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public SalesHistoryViewModel(SaleService saleService)
        {
            _saleService = saleService;
            _ = LoadAll();
        }

        // RF-044: historial completo, sin filtro.
        [RelayCommand]
        private async Task LoadAll()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _saleService.ListSalesAsync();
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo cargar el historial de ventas";
                return;
            }

            Sales = new ObservableCollection<SaleListItemDto>(result.Data ?? new());
            IsFiltered = false;
            SalesCount = 0;
            TotalSold = 0;
            AverageSale = 0;
        }

        // RF-047: filtra por rango de fechas y muestra el resumen.
        [RelayCommand]
        private async Task FilterByRange()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FromDate) || string.IsNullOrWhiteSpace(ToDate))
            {
                ErrorMessage = "Selecciona fecha inicial y fecha final";
                return;
            }

            IsBusy = true;
            var result = await _saleService.GetSalesByDateRangeAsync(FromDate, ToDate);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo consultar el rango de fechas";
                return;
            }

            Sales = new ObservableCollection<SaleListItemDto>(result.Data.Sales);
            SalesCount = result.Data.SalesCount;
            TotalSold = result.Data.TotalSold;
            AverageSale = result.Data.AverageSale;
            IsFiltered = true;
        }
    }
}