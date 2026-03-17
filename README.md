# CommentSense

CommentSense is a Roslyn analyzer for C# that helps keep XML documentation complete, consistent, and useful.

![CommentSense in Action](https://raw.githubusercontent.com/Thomas-Shephard/comment-sense/main/docs/images/Animation.gif)

The project is provided as a NuGet package that is published on [nuget.org](https://www.nuget.org/packages/CommentSense), and it can be installed by running:

```bash
dotnet add package CommentSense
```

## Requirements

CommentSense requires compiler XML documentation parsing:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

If this is disabled, CommentSense reports `CSENSE000`.

## Quick start

Add a baseline `.editorconfig`:

```ini
[*.cs]
comment_sense.visibility_level = protected
comment_sense.require_capitalization = true
comment_sense.require_ending_punctuation = true
```

View [docs/getting-started.md](https://github.com/Thomas-Shephard/comment-sense/blob/main/docs/getting-started.md) for more information.

## Common Rules

CommentSense includes over 25 diagnostics, commonly used rules include:

| Rule                      | Purpose                                     |
|---------------------------|---------------------------------------------|
| `CSENSE001`               | Missing symbol documentation                |
| `CSENSE002` / `CSENSE004` | Missing parameter / type parameter docs     |
| `CSENSE006`               | Missing return documentation                |
| `CSENSE012`               | Missing exception documentation             |
| `CSENSE016`               | Low-quality documentation                   |
| `CSENSE018`               | Missing explicit inheritdoc (when required) |

## Documentation

Detailed documentation:

- Getting started: [docs/getting-started.md](https://github.com/Thomas-Shephard/comment-sense/blob/main/docs/getting-started.md)
- Rules reference: [docs/rules-reference.md](https://github.com/Thomas-Shephard/comment-sense/blob/main/docs/rules-reference.md)
- Configuration reference: [docs/configuration.md](https://github.com/Thomas-Shephard/comment-sense/blob/main/docs/configuration.md)
- Practical examples: [docs/examples.md](https://github.com/Thomas-Shephard/comment-sense/blob/main/docs/examples.md)

## Contributions

Contributions are welcome! Read the [contributing guide](CONTRIBUTING.md) to get started.

## License

This project is licensed under the [MIT License](LICENSE).
