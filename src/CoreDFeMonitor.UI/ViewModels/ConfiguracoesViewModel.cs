// src/CoreDFeMonitor.UI/ViewModels/ConfiguracoesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class ConfiguracoesViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        public ConfiguracoesViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        [RelayCommand]
        private void VoltarDashboard()
        {
            _mainViewModel.NavegarPara<DashboardViewModel>();
        }

        [RelayCommand]
        private void AdicionarNovaEmpresa()
        {
            _mainViewModel.NavegarPara<CadastroEmpresaViewModel>();
        }
    }
}