// src/CoreDFeMonitor.Worker/Program.cs
using CoreDFeMonitor.Application;
using CoreDFeMonitor.Infrastructure;
using CoreDFeMonitor.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configura o Log para imprimir no console (ou no Log do Windows/Linux no futuro)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Injeta as mesmíssimas camadas da UI
builder.Services.AddInfrastructure();
builder.Services.AddApplication();

// Registra a classe Worker como um serviço do SO
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();