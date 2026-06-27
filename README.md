# ServiceMonitor

![Logo](https://raw.githubusercontent.com/saigkill/ServiceMonitor/develop/Assets/icon.png)


ServiceMonitor is a lightweight, configuration‑driven monitoring tool designed for cross platform systems (Linux, Windows).
It periodically checks a list of URLs and sends SMTP notifications when services become unreachable or return error states. 
The focus is on robustness, simplicity, and clean extensibility.

| W                           | W                                                                                                                                                                                                                        |
| --------------------------- |--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Continuous Integration Prod | [![Build status](https://dev.azure.com/saigkill/ServiceMonitor/_apis/build/status/ServiceMonitor-Prod)](https://dev.azure.com/saigkill/ServiceMonitor/_build/latest?definitionId=94)                                     |
| Continuous Integration Dev  | [![Build status](https://dev.azure.com/saigkill/ServiceMonitor/_apis/build/status/ServiceMonitor-Dev)](https://dev.azure.com/saigkill/ServiceMonitor/_build/latest?definitionId=93)                                      |
| Bugreports                  | [![GitHub issues](https://img.shields.io/github/issues/saigkill/ServiceMonitor)](https://github.com/saigkill/ServiceMonitor/issues)                                                                                      |
| Downloads all               | ![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/saigkill/ServiceMonitor/total)                                                                                                    |
| OS                          | ![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D6?logo=windows) ![Linux](https://img.shields.io/badge/Linux-DEB-0078D6?logo=linux) ![Linux](https://img.shields.io/badge/Linux-RPM-0078D6?logo=linux) |
| License                     | ![License](https://img.shields.io/badge/License-GPL3-green)                                                                                                                                                              |
| Blog                        | [![Blog](https://img.shields.io/badge/Blog-Saigkill-blue)](https://saschamanns.de)                                                                                                                                       |     

File a bug report [on Github](https://github.com/saigkill/ServiceMonitor/issues).
The documentation can be found in the [docs](https://writebook.saschamanns.de/5/service-manager) directory.

## Features

* Monitor any HTTP/HTTPS endpoint
* Configurable timeout
* SMTP notifications on failure
* Structured logging to a configurable directory
* Clean .NET Options Pattern configuration
* Cross‑platform (Linux, Windows, Docker)
* Ideal for self‑hosted services and NAS environments

## Requirements

* .NET 9 Runtime
* Write access to the log directory
* SMTP server access (optional, only needed for notifications)

## Extensibility

ServiceMonitor is built with modularity in mind.
Possible extensions include:

* additional notification providers (Telegram, Webhooks, Push)
* more health‑check types
* dashboard integration
* Docker image distribution
