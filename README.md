# CommentSense
CommentSense is a Roslyn-based diagnostic analyzer for C# designed to ensure that public-facing APIs are consistently and meaningfully documented.

## Requirements

For CommentSense to analyze your documentation, your project must have XML documentation generation enabled. Add the following property to your `.csproj` file:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

## Rules

### Project Configuration
*   **CSENSE000**: Warns when XML documentation parsing is disabled for the project. CommentSense relies on the compiler's documentation parsing to analyze your code. Enable it by following the [Requirements](#requirements) section above.

### General Documentation
*   **CSENSE001**: Ensures public members have XML documentation (e.g., `<summary>`, `<inheritdoc />`, or other content tags).
    *   *Note:* Using `<inheritdoc />` (without a `cref`) on a member that does not override or implement a base member will trigger this warning.
    *   *Default:* Analyzes `public`, `protected`, and `protected internal` members.
    *   *Configurable:* Enable for `internal` members using `comment_sense.analyze_internal`.
*   **CSENSE018**: Warns when a method, property, or event that overrides or implements a base member is missing documentation.
    *   *Note:* This specific rule suggests adding `<inheritdoc />` or custom documentation.
    *   *Configurable:* Allow skipping documentation for these members entirely using `comment_sense.allow_implicit_inheritdoc`.
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

### Implicit Documentation Inheritance
Allow skipping documentation entirely for methods, properties, and events that override or implement base members (enabled by default). This does not apply to types (classes, interfaces, etc.), which always require explicit documentation.
```ini
[*.cs]
comment_sense.allow_implicit_inheritdoc = false
```

## Contributions
Contributions are welcome! Read the [contributing guide](CONTRIBUTING.md) to get started.

## License
This project is licensed under the [MIT License](LICENSE).
