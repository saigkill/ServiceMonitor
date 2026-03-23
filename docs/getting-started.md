# Getting Started

## Requirements

* .NET 10 Runtime
* SMTP server access (for email notifications)

## Deployment options

* Windows 10/11 (ZIP archive, MSIX package)
* Linux (DEB/RPM packages, ZIP archive, AppImage)

Download the latest release from the [Releases](https://github.com/saigkill/ServiceMonitor/releases) page.

## Installation

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

### Windows MSI
Download the MSI from Releasepage and doubleclick on it. After installation the app file are in C:\Users\$(Username)\AppData\Roaming\Saigkill\ServiceMonitor\win-x64.
Then you can use dotnet ServiceMonitor.dll to start the app or create a shortcut to it.

### Linux DEB (Experimental)
``````bash
sudo apt install servicemonitor_$(Version)_amd64.deb
``````

### Linux rpm (Experimental)
``````bash
sudo rpm -i servicemonitor-$(Version)-1.x86_64.rpm
``````

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

### Configuration

After the first start, the app launches the Configurator on http://localhost:17800/index.html. Follow the instructions to set up your monitoring preferences, including selecting services to monitor and configuring email notifications.
![Logo](https://raw.githubusercontent.com/saigkill/ServiceMonitor/develop/Assets/ServiceMonitorConfiguration.png)
Then start the application again and show how the magic happens.

#### Configuration File

ServiceMonitor is developed cross‑platform and can be configured using a JSON file. After the first start, a appsettings.user.json file will be created and then it fails.
The configuration file is on one of the following paths:

While each run the terminal shows you the path, where the app is installed and where the config and logs live. Search in terminal for "UserConfigPath is" and you know it.
