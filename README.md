# ServiceMonitor

ServiceMonitor is a lightweight, configuration‑driven monitoring tool designed for Linux‑based systems (including QNAP NAS). 
It periodically checks a list of URLs and sends SMTP notifications when services become unreachable or return error states. 
The focus is on robustness, simplicity, and clean extensibility.

| W                           | W                                                                                                                                                                                                                                   |
| --------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Code                        | https://dev.azure.com/saigkill/ServiceMonitor                                                                                                                                                                                       |
| Continuous Integration Prod | [![Build Status](https://dev.azure.com/saigkill/MagazineFetcher/_apis/build/status%2FMagazineFetcher-Productive?branchName=master)](https://dev.azure.com/saigkill/MagazineFetcher/_build/latest?definitionId=83&branchName=master) |
| Continuous Integration Dev  | [![Build Status](https://dev.azure.com/saigkill/MagazineFetcher/_apis/build/status%2FMagazineFetcher-Stage?branchName=develop)](https://dev.azure.com/saigkill/MagazineFetcher/_build/latest?definitionId=82&branchName=develop)    |
| Code Coverage               | [![Coverage](https://img.shields.io/azure-devops/coverage/saigkill/MagazineFetcher/82)](https://dev.azure.com/saigkill/MagazineFetcher/_build/latest?definitionId=82)                                                               |
| Bugreports                  | [![GitHub issues](https://img.shields.io/github/issues/saigkill/MagazineFetcher)](https://github.com/saigkill/MagazineFetcher/issues)                                                                                               |
| Downloads all               | ![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/saigkill/MagazineFetcher/total)                                                                                                              |
| Blog                        | [![Blog](https://img.shields.io/badge/Blog-Saigkill-blue)](https://saschamanns.de)                                                                                                                                                  |

File a bug report [on Github](https://github.com/saigkill/MagazineFetcher/issues).

## Features

* Monitor any HTTP/HTTPS endpoint
* Configurable timeout
* SMTP notifications on failure
* Structured logging to a configurable directory
* Clean .NET Options Pattern configuration
* Cross‑platform (Linux, Windows, Docker)
* Ideal for self‑hosted services and NAS environments

## Installation

### Requirements

* .NET 8 Runtime
* Write access to the log directory
* SMTP server access (optional, only needed for notifications)

### Deployment Option

* On Windows
* On Linux

## Configuration

ServiceMonitor uses a JSON configuration file. Below is an example configuration:
```json
{
	"Urls": [
		"https://saschamanns.de"
	],
	"Smtp": {
		"Server": "",
		"Port": 587,
		"UseSsl": true,
		"From": "",
		"To": "",
		"Username": "",
		"Password": ""
	},
	"TimeoutSeconds": 5,
	"Logging": {
		"LogDirectory": ""
	}
}
```

### Opeions Classes

Configuration is bound using the .NET Options Pattern:
```csharp
builder.Services.Configure<ServiceMonitorOptions>(
    builder.Configuration.GetSection("ServiceMonitor"));

```

### Options Model
```csharp
public sealed class ServiceMonitorOptions
{
    public List<string> Urls { get; set; } = new();
    public SmtpOptions Smtp { get; set; } = new();
    public int TimeoutSeconds { get; set; }
    public LoggingOptions Logging { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool UseSsl { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoggingOptions
{
    public string LogDirectory { get; set; } = string.Empty;
}


```

## Logging

ServiceMonitor writes structured log files to the configured directory.
Logs include:

* successful checks
* failures and unreachable services
* SMTP send attempts
* exception details

## SMTP Notifications

When a monitored service becomes unreachable, ServiceMonitor sends an email containing:

* the affected URL
* the error status
* timestamp
* optional exception details

Supported:

* authenticated SMTP
* SSL/TLS
* ports 25/465/587

## Running the Application

### Direct Execution

````bash
dotnet ServiceMonitor.dll
````

### systemd Service Example

````ini
[Unit]
Description=ServiceMonitor

[Service]
ExecStart=/usr/bin/dotnet /share/Multimedia/apps/ServiceMonitor/ServiceMonitor.dll
WorkingDirectory=/share/Multimedia/apps/ServiceMonitor/
Restart=always

[Install]
WantedBy=multi-user.target
````

## Extensibility

ServiceMonitor is built with modularity in mind.
Possible extensions include:

* additional notification providers (Telegram, Webhooks, Push)
* more health‑check types
* dashboard integration
* Docker image distribution
