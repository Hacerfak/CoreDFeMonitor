// src/CoreDFeMonitor.UI/ViewModels/MainViewModel.cs
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreDFeMonitor.Application.Features.Empresas.Queries;
using CoreDFeMonitor.Core.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ObservableObject _viewAtual = null!;

        [ObservableProperty]
        private bool _isCarregandoAplicacao = true;

        public MainViewModel(IMediator mediator, IServiceProvider serviceProvider)
        {
            _mediator = mediator;
            _serviceProvider = serviceProvider;
        }

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