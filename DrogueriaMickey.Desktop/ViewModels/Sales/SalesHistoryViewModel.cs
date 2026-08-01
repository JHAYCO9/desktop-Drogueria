using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using DrogueriaMickey.Desktop.Navigation;
using DrogueriaMickey.Desktop.Services;
using System.Linq;

namespace DrogueriaMickey.Desktop.ViewModels.Sales
{
    public partial class SalesHistoryViewModel : ObservableObject
    {
        private readonly SaleService _saleService;
        private readonly IServiceProvider _services;
        private readonly NavigationService _navigation;

        [ObservableProperty] private ObservableCollection<SaleListItemDto> sales = new();
        [ObservableProperty] private string fromDate = System.DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
        [ObservableProperty] private string toDate = System.DateTime.Today.ToString("yyyy-MM-dd");
        [ObservableProperty] private int salesCount;
        [ObservableProperty] private decimal totalSold;
        [ObservableProperty] private decimal averageSale;
        [ObservableProperty] private bool isFiltered;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public SalesHistoryViewModel(SaleService saleService, IServiceProvider services, NavigationService navigation)
        {
            _saleService = saleService;
            _services = services;
            _navigation = navigation;
            _ = LoadAll();
        }

        // RF-044: historial completo, sin filtro.
        [RelayCommand]
        private async Task LoadAll()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _saleService.ListSalesFilteredAsync();
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo cargar el historial de ventas";
                return;
            }

            Sales = new ObservableCollection<SaleListItemDto>(result.Data.Sales);
            IsFiltered = false;
            SalesCount = 0;
            TotalSold = 0;
            AverageSale = 0;
        }

        // RF-044 + RF-047 combinadas: filtra por rango de fechas y calcula
        // el resumen (número de ventas, total vendido, promedio) sobre el
        // mismo resultado filtrado.
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
            var result = await _saleService.ListSalesFilteredAsync(FromDate, ToDate);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo consultar el rango de fechas";
                return;
            }

            var finalized = result.Data.Sales.Where(s => s.Status == "finalizada").ToList();

            Sales = new ObservableCollection<SaleListItemDto>(result.Data.Sales);
            SalesCount = finalized.Count;
            TotalSold = finalized.Sum(s => s.TotalAmount);
            AverageSale = finalized.Count > 0 ? TotalSold / finalized.Count : 0;
            IsFiltered = true;
        }

        // RF-045: botón "Consultar" en cada fila, navega al detalle.
        [RelayCommand]
        private void ViewDetail(SaleListItemDto sale)
        {
            var vm = _services.GetRequiredService<SaleDetailViewModel>();
            vm.Initialize(sale.Id);
            var view = new Views.Sales.SaleDetailView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void CreateReturn(SaleListItemDto sale)
        {
            var vm = _services.GetRequiredService<ViewModels.Returns.ReturnCreateViewModel>();
            vm.InitializeWithSaleId(sale.Id);
            var view = new Views.Returns.ReturnCreateView { DataContext = vm };
            _navigation.NavigateTo(view);
        }
    }
}