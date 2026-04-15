# Copilot Instructions for ServiceMonitor

This file defines the project rules, architectural principles, and expectations for GitHub Copilot when working with the ServiceMonitor repository. Copilot should use this information as context to generate consistent, maintainable, and architecturally compliant code.

---

## 1. Project Overview

ServiceMonitor is a .NET-based console application that monitors low-level URLs (web pages) and sends an email to one or more recipients in case of an outage.
Goals:

- Stable, deterministic operation
- Clear separation of domain logic, infrastructure, and I/O
- Minimal dependencies, maximally testable architecture
- Cross-platform capability (Windows, Linux)
- x64 optimization. x86 is not released.

---

## 2. Architectural Principles

- **Clean Architecture**: The domain layer is independent of infrastructure.
- **Dependency Injection**: All external dependencies are provided via DI.
- **Configuration Clarity**: Settings are loaded centrally via `IConfiguration` and provided via IOptions (where possible).
- **No static helper classes** except for clearly defined constants.
- **HttpClientFactory over HttpClient** for all HTTP operations.
- **NLog**: We log using NLog via the Microsoft.Extensions.Logging extension.
- **Logging via Microsoft.Extensions.Logging** with structured log entries.
- **Error Handling**: No swallowed exceptions; always log + clear return values.

---

## 3. Code Standards

- C# 13 features should be used where appropriate, but not at the cost of readability or maintainability.
- Async/await consistently used
- No `Task.Result` or `.Wait()`
- Prefer pattern matching
- Records for immutable data structures
- `Span<T>` only when there is a clear performance need
- Respect EditorConfig rules (formatting, naming, imports)
- We use Fail Fast principles: react immediately with clear error messages in case of errors, instead of hiding or ignoring them.
- In methods, we use Ardalis.GuardClauses to validate input parameters and catch unexpected values early. This increases the robustness and readability of the code, as the validation logic is clear and consistent.
- We are using Fluent Validation for complex validation scenarios, such as validating configuration objects or user input. This allows us to keep validation logic separate from business logic and provides a fluent API for defining validation rules.
- We are using Meziantou.Analyzer, Roslynator.Analyzers and SonarAnalyzer.CSharp to enforce code quality and best practices. These analyzers help us identify potential issues, improve code readability, and maintain a consistent coding style across the project.
- Configurations are provided via IOptions to ensure a clear separation between configuration definition and usage. This allows for better testability and flexibility, as configurations can be easily mocked or adjusted without changing the application logic.
- We use English as the primary language for code, comments, and documentation to ensure broader understanding and collaboration. All class, method, and variable names should be in English.
- We using a LINQ Select instead of a foreach loop if its possible.
- We using CancellationToken in all async methods to allow for graceful shutdown and better resource management.
- Is possible, we are using CSharpFunctionalExtensions for Result<T> patterns to handle errors in a more functional way, avoiding exceptions for expected error conditions and improving code readability. Also we are using Maybe<T> for optional values to avoid null references and make the intent of optionality clear in the code.

---

## 4. Project Structure

- `Program.cs`: Entry point, keep minimal
- `ServiceCollectionExtension`: Dependency Injection Setup
- `ServiceMonitor.Application/`: Application Layer
- `ServiceMonitor.Domain/`: Domain Layer, Business logic
- `ServiceMonitor.Infrastructure/`: Infrastructure Layer, Implementations for I/O, system access, HTTP, filesystem
- `ServiceMonitor.Infrastructure/Configuration/`: Options Classes, Bindings
- `ServiceMonitor.Infrastructure/Hosting/ConsoleHostedService`: Hosting and application lifecycle management
- `ServiceMonitor.Presentation/`: Presentation
- `Tests/`: Unit and Integration tests
Copilot should suggest new files according to this structure.

---

## 5. Branching & Deployment

- Main branch: `master` (stable, release-ready)
- Development branch: `develop` (current development, not stable)
- Feature branches: `feature/<feature-name>` (for new features)

The branching strategy is as follows:

- `master` is the stable branch from which releases are created. All changes must be merged into `develop` via pull requests before they reach `master`. No direct commits to `master`.
- `develop` is the main development branch where all new features and bug fixes are integrated. It should always be in a functional state, even if not as stable as `master`.
- `feature/<feature-name>` branches are created for the development of new features or bug fixes. Once work on a feature is complete, a pull request is created to merge the changes into `develop`. Feature branches should be regularly synchronized with `develop` to minimize merge conflicts.
- Releases are created from `master` (via version tags) after all changes have been integrated and tested in `develop`. It is recommended to create a release branch before merging into `master` to perform final tests and preparations for the release.


---

## 6. Logging Guidelines

- Use structured logging:
  ```csharp
  _logger.LogInformation("Service {ServiceName} started", name);
  ```
- Do not use string interpolation in log messages
- LogLevel:
  - Trace: very detailed (in debug environment) otherwise Error LogLevel.
  - Debug: development details (in debug environment) otherwise normal.
  - Information: normal flow
  - Warning: unexpected but tolerable conditions
  - Error: errors, but the program continues
  - Critical: the program must be terminated

---

## 7. Error Handling

- Never ignore exceptions.
- Only catch exceptions when adding meaningful context.
- Do not use generic catch (Exception) without logging.
- For expected errors (e.g., file not found), use clear return values instead of exceptions.

---

## 8. Tests

- MSTest as the test framework
- FluentAssertions for assertions
- Tests should be deterministic and independent
- No external resources without mocks/fakes
- By generating Tests, Copilot should:
  - Use the Arrange‑Act‑Assert pattern
  - Consider edge cases
  - Simulate Dependency Injection via Mocks
- Maintain 100% test coverage for Domain and Application layers

---

## 9. Build & Deployment

- Build via dotnet build or dotnet publish
- Tests via dotnet test
- Release‑Builds are self‑contained
- Versioning via Git‑Tags
- No manual deployment documentation; everything is automated
- We are using GitVersion to manage versioning based on Git history and branching strategy. This allows us to automatically generate version numbers for our builds and releases, ensuring consistency and reducing manual errors in versioning.

---

## 10. Style and Quality

Copilot should generate code that:

- is clear, readable, and maintainable
- does not introduce unnecessary complexity
- respects SOLID principles
- Prefers Single Responsibility
- is well-commented when logic is not self-explanatory

---

## 11. What Copilot should avoid    

- Generating code that is not testable
- Static dependencies
- Mixing domain logic and infrastructure
- Hardcoded paths or environment dependencies
- Suggesting unnecessary external libraries
- Inline configuration instead of Options pattern

---

## 12. Prompt‑Expections for Copilot

When Copilot asks questions or makes suggestions, it should:

- Respect architectural decisions
- Provide alternatives, but mark the preferred solution
- Comment code when it is complex
- Ask for clarification instead of guessing

---

## 13. Project Vision

ServiceMonitor should long-term:

- be modular and extendable
- serve as a basis for other system-level tools
- be integrated into CI/CD pipelines
- run stable and reproducible

Copilot should make suggestions that support this vision.

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.
