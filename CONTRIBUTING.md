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
To ensure that changes work as expected, follow these steps:
- Use the provided NUnit test framework to write tests
- Write tests for all new features or bug fixes
- Ensure all tests pass before submitting a pull request

## Release Process
This project uses [MinVer](https://github.com/adamralph/minver) for versioning.

Versions are automatically determined by Git tags in the format `vMAJOR.MINOR.PATCH`.
To create a new release, use the GitHub UI to create a new "Release", which will automatically create the required Git tag and trigger the deployment workflow.

## License
By contributing to CommentSense, you agree that your contributions will be licensed under the MIT License.
