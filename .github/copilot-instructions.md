# Copilot Instructions for ServiceMonitor

Diese Datei definiert die Projektregeln, Architekturprinzipien und Erwartungen für GitHub Copilot beim Arbeiten mit dem ServiceMonitor‑Repository. Copilot soll diese Informationen als Kontext nutzen, um konsistenten, wartbaren und architekturkonformen Code zu erzeugen.

---

## 1. Projektüberblick

ServiceMonitor ist eine .NET‑basierte Konsolenanwendung, die systemnahe URLs (Webseiten) überwacht und eine Email bei einem Ausfall an eine oder mehrere Emailempfänger sendet.
Ziele:
- Stabiler, deterministischer Betrieb
- Klare Trennung von Domänenlogik, Infrastruktur und I/O
- Minimale Abhängigkeiten, maximal testbare Architektur
- Cross‑Platform‑Fähigkeit (Windows, Linux)
- x64 Optimierung. x86 wird nicht veröffentlicht.

---

## 2. Architekturprinzipien

- **Clean Architecture**: Domänenschicht ist unabhängig von Infrastruktur.
- **Dependency Injection**: Alle externen Abhängigkeiten werden über DI bereitgestellt.
- **Konfigurationsklarheit**: Einstellungen werden zentral über `IConfiguration` geladen und mittels IOptions bereitgestellt (wo möglich).
- **Keine statischen Helferklassen** außer für klar abgegrenzte Konstanten.
- **HttpClientFactory statt HttpClient** für alle HTTP‑Operationen.
- **NLog**: Wir loggen mittels NLog über die Microsoft.Extensions.Logging Extension.
- **Logging über Microsoft.Extensions.Logging** mit strukturierten Logeinträgen.
- **Fehlerbehandlung**: Keine swallowed Exceptions; immer Logging + klare Rückgabewerte.

---

## 3. Code‑Standards

- C# 10
- Async/await konsequent nutzen
- Keine `Task.Result` oder `.Wait()`
- Pattern Matching bevorzugen
- Records für immutable Datenstrukturen
- `Span<T>` nur bei klarer Performance‑Notwendigkeit
- EditorConfig‑Regeln respektieren (Formatierung, Naming, Imports)
- Wir nutzen Fail Fast Prinzipien: Bei Fehlern sofort mit klaren Fehlermeldungen reagieren, anstatt Fehler zu verschleiern oder zu ignorieren.
- In Methoden nutzen wir Ardalis.GuardClauses, um Eingabeparameter zu validieren und unerwartete Werte frühzeitig abzufangen. Das erhöht die Robustheit und Lesbarkeit des Codes, da die Validierungslogik klar und konsistent ist.

---

## 4. Projektstruktur

- `Program.cs`: Einstiegspunkt, minimal halten
- `ServiceMonitor.Application/`: Application Layer
- `ServiceMonitor.Domain/`: Domain Layer, Business logic
- `ServiceMonitor.Infrastructure/`: Infrastructure Layer, Implementierungen für I/O, Systemzugriffe, HTTP, Filesystem
- `ServiceMonitor.Infrastructure/Configuration/`: Options Classes, Bindings
- `ServiceMonitor.Presentation/`: Presentation
- `ServiceMonitor.Presentation/DependencyInjection`: Dependency Injection stuff
- `ServiceMonitor.Presentation/Hosting`: ConsoleHosting
- `Tests/`: Unit‑ und Integrationstests

Copilot soll neue Dateien entsprechend dieser Struktur vorschlagen.

---

## 5. Branching & Deployment

- Hauptbranch: `master` (stabil, releasebereit)
- Entwicklungsbranch: `develop` (aktuelle Entwicklung, nicht stabil)
- Featurebranches: `feature/<feature-name>` (für neue Features)

Die Branching Strategie sieht so aus:

- `master` ist der stabile Branch, von dem Releases erstellt werden. Alle Änderungen müssen über Pull Requests in `develop` gemerged werden, bevor sie in `master` gelangen. In 'master' gelangt der Code nur über Pull Requests. Keine direkten Commits.
- 'develop' ist der Hauptentwicklungsbranch, in dem alle neuen Features und Bugfixes integriert werden. Er sollte immer in einem funktionsfähigen Zustand sein, auch wenn er nicht so stabil wie `master` sein muss.
- 'feature/<feature-name>' Branches werden für die Entwicklung neuer Features oder Bugfixes erstellt. Sobald die Arbeit an einem Feature abgeschlossen ist, wird ein Pull Request erstellt, um die Änderungen in `develop` zu integrieren. Featurebranches sollten regelmäßig mit `develop` synchronisiert werden, um Merge-Konflikte zu minimieren.

- Releases werden von `master` (via Versiontag) erstellt, nachdem alle Änderungen in `develop` integriert und getestet wurden. Es wird empfohlen, vor dem Merge in `master` einen Release-Branch zu erstellen, um letzte Tests und Vorbereitungen für die Veröffentlichung durchzuführen.


---

## 6. Logging‑Richtlinien

- Verwende strukturiertes Logging:
  ```csharp
  _logger.LogInformation("Service {ServiceName} started", name);
  ```
- Keine String‑Interpolation in Log‑Nachrichten

- LogLevel:

  - Trace: sehr detailliert (in Debugumgebung) sonst Error LogLevel.

  - Debug: Entwicklungsdetails (in Debugumgebung) sonst normal.

  - Information: normaler Ablauf

  - Warning: Unerwartete, aber tolerierbare Zustände

  - Error: Fehler, aber Programm läuft weiter

  - Critical: Programm muss beendet werden
  
## 7. Fehlerbehandlung

- Exceptions niemals ignorieren.

- Exceptions nur fangen, wenn sinnvoller Kontext hinzugefügt wird.

- Keine generischen catch (Exception) ohne Logging.

- Bei erwartbaren Fehlern (z. B. Datei nicht gefunden) klare Rückgabewerte statt Exceptions.

## 8. Tests

- xUnit als Testframework

- FluentAssertions für Assertions

- Tests sollen deterministisch und unabhängig voneinander sein

- Keine externen Ressourcen ohne Mocks/Fakes

- Copilot soll bei Testgenerierung:

  - Arrange‑Act‑Assert‑Pattern nutzen

  - Randfälle berücksichtigen

  - Dependency Injection über Mocks simulieren
  
## 9. Build & Deployment

- Build über dotnet build oder dotnet publish

- Tests über dotnet test

- Release‑Builds sind self‑contained

- Versionierung über Git‑Tags

- Keine manuelle Deployment‑Dokumentation; alles automatisiert

## 10. Stil und Qualität

Copilot soll Code erzeugen, der:

- klar, lesbar und wartbar ist

- keine unnötige Komplexität einführt

- SOLID‑Prinzipien respektiert

- Single Responsibility bevorzugt

- gut kommentiert ist, wenn Logik nicht selbsterklärend ist

## 11. Was Copilot vermeiden soll

- Generierung von Code, der nicht testbar ist

- Statische Abhängigkeiten

- Vermischung von Domänenlogik und Infrastruktur

- Hardcodierte Pfade oder Umgebungsabhängigkeiten

- Unnötige externe Libraries vorschlagen

- Inline‑Konfiguration statt Options‑Pattern

## 12. Prompt‑Erwartungen

Wenn Copilot Fragen stellt oder Vorschläge macht, soll es:

- Architekturentscheidungen respektieren

- Alternativen nennen, aber die bevorzugte Lösung markieren

- Code kommentieren, wenn er komplex ist

- Bei Unsicherheit nachfragen statt zu raten

## 13. Projektvision

ServiceMonitor soll langfristig:

- modular erweiterbar sein

- als Basis für weitere systemnahe Tools dienen

- in CI/CD‑Pipelines integriert werden

- stabil und reproduzierbar laufen

Copilot soll Vorschläge machen, die diese Vision unterstützen.

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.
