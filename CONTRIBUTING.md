# Contributing to CommentSense
Contributions to CommentSense are welcome!

CommentSense is a Roslyn-based diagnostic analyzer for C# designed to ensure that public-facing APIs are consistently and meaningfully documented.

This guide explains how to contribute to the project.

## How to Contribute
There are several ways to contribute to CommentSense:
- **Report bugs:** Find an issue and describe the problem you encountered
- **Fix bugs:** Submit a pull request with a proposed fix
- **Request features:** Suggest a new feature or improvement
- **Improve documentation:** Submit a pull request with improved documentation

## Submitting a Pull Request
To submit a pull request, follow these steps:
- Fork the repository
- Create a new branch for your changes
- Make your changes and write tests if applicable
- Commit and push your changes to your fork
- Submit a pull request to the `main` branch of this repository

### Pull Request Title Convention
To maintain a clean and automated changelog, this project requires pull request titles to follow the [Conventional Commits specification](https://www.conventionalcommits.org/en/v1.0.0/). Titles should be formatted as follows:
`<type>: <description>`

Common types include:
- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation only changes
- `style`: Changes that do not affect the meaning of the code (white-space, formatting, etc.)
- `refactor`: A code change that neither fixes a bug nor adds a feature
- `test`: Adding missing tests or correcting existing tests
- `chore`: Changes to the build process or auxiliary tools and libraries such as documentation generation

## Testing Guidelines
Use the SDK pinned in [global.json](global.json).

To ensure that changes work as expected, follow these steps:
- Use the provided NUnit test framework to write tests
- Write tests for all new features or bug fixes
- Ensure all tests pass before submitting a pull request

```bash
dotnet build CommentSense.slnx --configuration Release
dotnet test --solution CommentSense.slnx --configuration Release --no-build --coverlet --results-directory ./coverage
```

## Performance Guidelines
Maintaining high performance is critical for a Roslyn analyzer.
- **Run Benchmarks:** If you modify logic in `src/CommentSense.Analyzers/Logic`, run the performance suite.
  ```bash
  dotnet run -c Release --project benchmarks -- --filter '*'
  ```
- **Avoid Regressions:** Ensure your changes do not significantly increase memory allocations or execution time.
- **Dogfooding:** Use the `DogfoodBenchmarks` to verify impact on the real codebase.

CI warns when allocations increase by more than 5%; benchmark errors fail the build.

## Release Process
CI and publishing test the NuGet package against the SDKs in the [package-consumption workflow](.github/workflows/package-consumption.yml). To run locally, use an empty output directory:

```powershell
dotnet pack CommentSense.slnx --configuration Release --output artifacts/package-check
./eng/package-consumption-check.ps1 -PackageOutputPath artifacts/package-check
```

This checks package contents and analyzer diagnostics, not editor code fixes or older SDKs. Publishing also requires 100% line and branch coverage.

This project uses [MinVer](https://github.com/adamralph/minver) for versioning.

Versions are automatically determined by Git tags in the format `vMAJOR.MINOR.PATCH`.
Publish a GitHub release with a `vMAJOR.MINOR.PATCH` tag to trigger the publishing workflow.

## License
By contributing to CommentSense, you agree that your contributions will be licensed under the MIT License.
