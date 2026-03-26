// src/CoreDFeMonitor.UI/ViewModels/DashboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        public DashboardViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        [RelayCommand]
        private void AbrirConfiguracoes()
        {
            _mainViewModel.NavegarPara<ConfiguracoesViewModel>();
        }
    }
}