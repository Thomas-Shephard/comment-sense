# GitHub Copilot Instructions for CommentSense

You are an expert code reviewer and software engineer assisting with the `CommentSense` project. Your goal is to help write, review, and maintain high-quality, secure, and idiomatic C# code for this Roslyn analyzer.

## 1. High Level Details
*   **Project:** A Roslyn-based diagnostic analyzer for C# designed to enforce consistent and meaningful API documentation (XML comments).
*   **Type:** Roslyn Analyzer (NuGet package).
*   **Frameworks:**
    *   **Analyzer:** .NET Standard 2.0.
    *   **Tests:** .NET 8.0, .NET 9.0, .NET 10.0.
*   **Package Management:** Central Package Management via `Directory.Packages.props`.
*   **Key Libraries:** `Microsoft.CodeAnalysis`, `NUnit`, `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`.

## 2. Build and Validate
Always verify changes using these commands.
*   **Build:** `dotnet build CommentSense.slnx --configuration Release`
*   **Test:** `dotnet test CommentSense.slnx --configuration Release --no-build --settings .runsettings --results-directory ./coverage`
*   **Lint/Style:** Use `.editorconfig` rules. Build with `/warnaserror` when possible.

**Build Status:** Trust the user or actual build/test logs provided in the chat context regarding compilation status. Do not claim code will not compile based on internal static analysis if the context or the user indicates otherwise. Avoid providing unsolicited "fix" suggestions for non-existent build errors.

## 3. Project Layout
*   **`src/CommentSense.Analyzers/`**: The main analyzer project. Contains the diagnostic analyzer implementations, rule definitions, and specialized analyzer logic.
*   **`src/CommentSense.Core/`**: Shared core logic. Contains common utilities for accessibility checks and XML documentation parsing.
*   **`tests/CommentSense.Analyzers.Tests/`**: Integration tests for the diagnostic rules.
*   **`tests/CommentSense.Core.Tests/`**: Unit tests for the core utilities.
*   **`tests/CommentSense.TestHelpers/`**: Shared testing infrastructure, including `CommentSenseAnalyzerTestBase<T>` and `RoslynTestUtils`.
*   **`artifacts/`**: Unified build output location (bin, obj, package) configured via `Directory.Build.props`.
*   **`.github/workflows/`**: CI/CD pipelines for building, testing, linting, and publishing.

## 4. Coding & Architectural Standards
*   **Roslyn Best Practices:**
    *   Use `ImmutableArray` for collections.
    *   Register actions in `Initialize` (e.g., `context.RegisterSymbolAction`).
    *   Avoid state in the analyzer class itself (use the `AnalysisContext`).
*   **Testing:**
    *   Use `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` with `NUnit`.
    *   Inherit from `CommentSenseAnalyzerTestBase<T>` for analyzer tests.
    *   Tests should verify both positive (diagnostic reported) and negative (no diagnostic) cases.
    *   Use `[| ... |]` markup in test strings to indicate expected diagnostic locations.
*   **XML Parsing:**
    *   Use `DocumentationExtensions` for parsing XML comments to ensure resilience against malformed XML.
    *   Handle `inheritdoc` and `include` tags gracefully (currently treated as "valid" without deep validation).
*   **Style:**
    *   **Namespaces:** Use file-scoped namespaces (e.g., `namespace CommentSense.Analyzers;`).
    *   **Formatting:** 4 spaces indentation, CRLF line endings.
    *   **Naming:** PascalCase for types and members.

## 5. Security Guidelines
*   **XML Processing:** Be cautious when expanding XML parsing logic. Use safe parsing settings (handled in `DocumentationExtensions`) to prevent XXE.
*   **Dependencies:** Check `Directory.Packages.props` for versions. Do not introduce vulnerable dependencies.

## 6. Review Checklist
When reviewing code or suggesting changes, you **MUST** check for the following:

1.  **Documentation Updates (CRITICAL):**
    *   If changes were made to public APIs, configuration logic, or core architecture:
    *   **Action:** Verify that ALL relevant documentation is updated. This includes `README.md`, `CONTRIBUTING.md`, XML documentation comments (`/// <summary>`), and these `copilot-instructions.md` themselves. If any documentation is missing or outdated, **explicitly flag this** in your review.
2.  **Code Consistency:**
    *   Verify that `Analyzers` inherit from `CommentSenseAnalyzerBase` where applicable.
    *   Diagnostic messages should generally use `SymbolDisplayFormat.MinimallyQualifiedFormat` when including type names to ensure readability (e.g. `List<T>` instead of just `List` or `System.Collections.Generic.List<T>`).
3.  **Diagnostic IDs:** Ensure new diagnostics follow the `CSENSExxx` naming convention and are added to `SupportedDiagnostics`.
4.  **Test Coverage:** Ensure new rules or logic branches have corresponding `[Test]` cases in the relevant test projects.
5.  **Performance:** Ensure `AnalyzeSymbol` is efficient and returns early for ineligible symbols (using `AnalyzerExtensions.IsEligibleForAnalysis`).
6.  **Backward Compatibility:** Do not change existing diagnostic IDs or significantly alter their triggering logic without a major version bump considerations.
