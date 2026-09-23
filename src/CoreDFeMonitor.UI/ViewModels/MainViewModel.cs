using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreDFeMonitor.Application.Features.Empresas.Queries;
using CoreDFeMonitor.Application.Services;
using CoreDFeMonitor.Core.Mediator;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly IServiceProvider _serviceProvider;
        private readonly IToastService _toastService;

        [ObservableProperty] private ObservableObject _viewAtual = null!;
        [ObservableProperty] private bool _isCarregandoAplicacao = true;

        public ObservableCollection<ToastMessage> Toasts { get; } = new();

        public MainViewModel(IMediator mediator, IServiceProvider serviceProvider, IToastService toastService)
        {
            _mediator = mediator;
            _serviceProvider = serviceProvider;
            _toastService = toastService;

            _toastService.OnToastAdicionado += ToastAdded;
            _toastService.OnToastRemovido += ToastRemoved;
        }

        private void ToastAdded(ToastMessage toast)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Toasts.Add(toast));
        }

        private void ToastRemoved(Guid id)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                var target = Toasts.FirstOrDefault(t => t.Id == id);
                if (target != null) Toasts.Remove(target);
            });
        }

        [RelayCommand]
        private void FecharToast(Guid id) => _toastService.Remover(id);

        public async Task InicializarAsync()
        {
            IsCarregandoAplicacao = true;
            bool possuiEmpresas = await _mediator.Send(new VerificarEmpresasCadastradasQuery());
            if (possuiEmpresas)
            {
                NavegarPara<DashboardViewModel>();
            }
            else
            {
                NavegarPara<CadastroEmpresaViewModel>();
            }
            IsCarregandoAplicacao = false;
        }

        public void NavegarPara<TViewModel>() where TViewModel : ObservableObject
        {
            ViewAtual = _serviceProvider.GetRequiredService<TViewModel>();
        }
    }
}