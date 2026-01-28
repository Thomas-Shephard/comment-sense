# CommentSense
CommentSense is a Roslyn-based diagnostic analyzer for C# designed to ensure that public-facing APIs are consistently and meaningfully documented.

## Rules

### General Documentation
*   **CSENSE001**: Ensures public members have XML documentation (e.g., `<summary>`, `<inheritdoc />`, or other content tags).
    *   *Default:* Analyzes `public`, `protected`, and `protected internal` members.
    *   *Configurable:* Enable for `internal` members using `comment_sense.analyze_internal`.
*   **CSENSE016**: Flags "low quality" documentation.
    *   *Default:* Flags empty content or content that just repeats the symbol name.
    *   *Configurable:* Add custom terms using `comment_sense.low_quality_terms` (e.g., "TODO, TBD").
*   **CSENSE007**: Validates that `cref` attributes in documentation point to valid symbols.

### Parameters & Type Parameters
Ensures the `<param>` and `<typeparam>` tags match the method signature exactly.
*   **CSENSE002 / CSENSE004**: Flags parameters or type parameters defined in code but missing from documentation.
*   **CSENSE003 / CSENSE005**: Flags "stray" tags referring to parameters that do not exist.
*   **CSENSE008 / CSENSE010**: Enforces that the order of parameter tags in documentation matches the method signature.
*   **CSENSE009 / CSENSE011**: Flags duplicate tags for the same parameter.

### Return Values
*   **CSENSE006**: Requires a `<returns>` tag for members that return a value (i.e., non-`void`, non-`Task`, non-`ValueTask`).
*   **CSENSE013**: Flags stray `<returns>` tags on members that do not produce a documented return value (including `void`, `Task`, and `ValueTask` members), as well as on properties and indexers.
### Exceptions
*   **CSENSE012**: Scans the method body for explicitly thrown exceptions and ensures they are documented with `<exception>` tags.
    *   *Configurable:* Ignore specific exception types using `comment_sense.ignored_exceptions`.
*   **CSENSE017**: Validates that the `cref` attribute in an `<exception>` tag refers to a valid Exception type.

### Properties
*   **CSENSE014**: Requires a `<value>` tag for properties.
    *   *Default:* Disabled.
*   **CSENSE015**: Flags stray `<value>` tags.

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
