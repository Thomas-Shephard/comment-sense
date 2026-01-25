# CommentSense
CommentSense is a Roslyn-based diagnostic analyzer for C# designed to ensure that public-facing APIs are consistently and meaningfully documented.

## Configuration
You can configure the analyzer behavior using an `.editorconfig` file in your project root or solution directory.

### Low Quality Terms
Specify a comma-separated list of terms that are considered "low quality" in summaries, parameters, or return value descriptions.
```ini
[*.cs]
comment_sense.low_quality_terms = TODO, TBD, FixMe
```

### Ignored Exceptions
Specify a comma-separated list of exception types (by name or full name) that should be ignored by the missing exception documentation rule.
```ini
[*.cs]
comment_sense.ignored_exceptions = System.ArgumentNullException, ArgumentOutOfRangeException
```

### Internal Member Analysis
Enable analysis for `internal` and `private protected` members (disabled by default).
```ini
[*.cs]
comment_sense.analyze_internal = true
```

## Contributions
Contributions are welcome! Read the [contributing guide](CONTRIBUTING.md) to get started.

## License
This project is licensed under the [MIT License](LICENSE).
