// src/CoreDFeMonitor.Worker/Program.cs
using CoreDFeMonitor.Application;
using CoreDFeMonitor.Infrastructure;
using CoreDFeMonitor.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Usa as mesmas injeções limpas!
builder.Services.AddInfrastructure();
builder.Services.AddApplication();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();