// src/CoreDFeMonitor.UI/ViewModels/DocumentoItemViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CoreDFeMonitor.Application.Features.Documentos.Dtos;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class DocumentoItemViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isSelecionado;
        public DocumentoListagemDto Dados { get; }

        public DocumentoItemViewModel(DocumentoListagemDto dados)
        {
            Dados = dados;
        }
    }
}