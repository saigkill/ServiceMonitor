# Introduction

![Logo](https://raw.githubusercontent.com/saigkill/ServiceMonitor/develop/Assets/service_monitor.png)
ServiceMonitor is a lightweight, configuration‑driven monitoring tool designed for Linux‑based systems (including QNAP NAS). It periodically checks a list of URLs and sends SMTP notifications when services become unreachable or return error states. The focus is on robustness, simplicity, and clean extensibility.

## Features

* Monitor any HTTP/HTTPS endpoint
* Configurable timeout
* SMTP notifications on failure
* Structured logging to a configurable directory
* Clean .NET Options Pattern configuration
* Cross‑platform (Linux, Windows, Docker)
* Ideal for self‑hosted services and NAS environments

## Logging

ServiceMonitor writes structured log files to the configured directory. Logs include:

* successful checks
* failures and unreachable services
* SMTP send attempts
* exception details
* SMTP Notifications

When a monitored service becomes unreachable, ServiceMonitor sends an email containing:

* the affected URL
* the error status
* timestamp
* optional exception details

Supported:

* authenticated SMTP
* SSL/TLS
* ports 25/465/587
