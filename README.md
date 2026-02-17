# ServiceMonitor

![Logo](https://raw.githubusercontent.com/saigkill/ServiceMonitor/develop/Assets/service_monitor.png)


ServiceMonitor is a lightweight, configuration‑driven monitoring tool designed for Linux‑based systems (including QNAP NAS). 
It periodically checks a list of URLs and sends SMTP notifications when services become unreachable or return error states. 
The focus is on robustness, simplicity, and clean extensibility.

| W                           | W                                                                                                                                                                                                                                                                                                                         |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Code                        | https://dev.azure.com/saigkill/ServiceMonitor                                                                                                                                                                                                                                                                             |
| Continuous Integration Prod | [![Build status](https://dev.azure.com/saigkill/ServiceMonitor/_apis/build/status/ServiceMonitor-Prod)](https://dev.azure.com/saigkill/ServiceMonitor/_build/latest?definitionId=94)                                                                                                                                      |
| Continuous Integration Dev  | [![Build status](https://dev.azure.com/saigkill/ServiceMonitor/_apis/build/status/ServiceMonitor-Dev)](https://dev.azure.com/saigkill/ServiceMonitor/_build/latest?definitionId=93)                                                                                                                                       |
| Code Coverage               | [![Coverage](https://img.shields.io/azure-devops/coverage/saigkill/ServiceMonitor/89)](https://dev.azure.com/saigkill/ServiceMonitor/_build/latest?definitionId=89)                                                                                                                                                       |
| Bugreports                  | [![GitHub issues](https://img.shields.io/github/issues/saigkill/ServiceMonitor)](https://github.com/saigkill/ServiceMonitor/issues)                                                                                                                                                                                       |
| Workitems                   | [![Board Status](https://dev.azure.com/saigkill/3cb00ef8-116d-47c1-9c08-748af6bbb726/0978944f-fa4e-4711-9ff7-46d373d94899/_apis/work/boardbadge/281b6fb6-0842-442f-b921-8aa7582f26a2)](https://dev.azure.com/saigkill/3cb00ef8-116d-47c1-9c08-748af6bbb726/_boards/board/t/0978944f-fa4e-4711-9ff7-46d373d94899/Stories/) |
| Downloads all               | ![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/saigkill/ServiceMonitor/total)                                                                                                                                                                                                     |
| Language                    | ![Framework](https://img.shields.io/badge/.NET-9%2B-blue?logo=csharp)                                                                                                                                                                                                                                                     |
| OS                          | ![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D6?logo=windows) ![Linux](https://img.shields.io/badge/Linux-DEB-0078D6?logo=linux) ![Linux](https://img.shields.io/badge/Linux-RPM-0078D6?logo=linux)                                                                                                  |
| License                     | ![License](https://img.shields.io/badge/License-MIT-green)                                                                                                                                                                                                                                                                |
| Status                      | ![Status](https://img.shields.io/badge/Status-Active-success)                                                                                                                                                                                                                                                             |
| Maintained                  | ![Maintained](https://img.shields.io/badge/Maintained-Yes-brightgreen)                                                                                                                                                                                                                                                    |
| Blog                        | [![Blog](https://img.shields.io/badge/Blog-Saigkill-blue)](https://saschamanns.de)                                                                                                                                                                                                                                        |     

File a bug report [on Github](https://github.com/saigkill/ServiceMonitor/issues).

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

* .NET 9 Runtime
* Write access to the log directory
* SMTP server access (optional, only needed for notifications)

### Deployment Option

* On Windows
* On Linux

Download the latest release from the [Releases](https://github.com/saigkill/ServiceMonitor/releases).

### Linux ZIP          
``````bash
# Download and extract
zipFile="ServiceMonitor-${Version}-Linux.zip"
url="https://github.com/saigkill/ServiceMonitor/releases/download/${tagName}/${zipFile}"

echo "Downloading: $url"
curl -L -o "$zipFile" "$url"

echo "Extracting..."
unzip -o "$zipFile" -d "/home/username"

# Run
cd /home/username
dotnet ServiceMonitor.dll
``````

### Windows ZIP          
``````powershell
# Download and extract
Invoke-WebRequest -Uri "https://github.com/saigkill/ServiceMonitor/releases/download/$tagName/ServiceMonitor-$(Version)-Windows.zip" -OutFile "ServiceMonitor-$(Version)-Windows.zip"
Expand-Archive -Path "ServiceMonitor-$(Version)-Windows.zip" -DestinationPath "C:\Users\Username"
          
# Run
cd /home/username
dotnet ServiceMonitor.dll
``````

### Linux DEB (Experimental)
``````bash
sudo apt install servicemonitor_$(Version)_amd64.deb
``````

### Linux rpm (Experimental)
``````bash
sudo rpm -i servicemonitor-$(Version)-1.x86_64.rpm
``````

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
