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
    *   *Default:* Analyzes members according to the `visibility_level` (default: `protected`).
    *   *Configurable:* Set the visibility threshold using `comment_sense.visibility_level`.
*   **CSENSE018**: Warns when a member that overrides or implements a base member is missing explicit documentation (when configured to require it).
    *   *Note:* By default, these members are allowed to implicitly inherit documentation.
    *   *Configurable:* Set `comment_sense.allow_implicit_inheritdoc = false` to require explicit documentation (e.g., `<inheritdoc />`) for all inheriting members.
*   **CSENSE016**: Flags "low quality" documentation.
    *   *Default:* Flags empty content or content that just repeats the symbol name.
    *   *Configurable:* Add custom terms, minimum length, punctuation requirements, capitalization requirements, and similarity thresholds.
*   **CSENSE007**: Validates that `cref` attributes in documentation point to valid symbols.
*   **CSENSE022**: Flags stray `<summary>` tags that are nested within other tags or duplicated.
    *   **Code Fix:** An automatic code fix is available to remove stray tags. This fix supports **Fix All** in document, project, or solution.
*   **CSENSE019**: Recommends using the `<see langword="..." />` tag for C# keywords (e.g., `true`, `false`, `null`, `void`) instead of plain text.
    *   **Code Fix:** An automatic code fix is available to wrap plain text keywords in `<see langword="..." />`. This fix supports **Fix All** in document, project, or solution.
    *   *Configurable:* Customize the list of keywords using `comment_sense.langwords`. Defaults to `true, false, null, void`. Specifying this option replaces the defaults.

### Parameters & Type Parameters
Ensures parameters and type parameters are correctly documented and referenced.
*   **CSENSE002 / CSENSE004**: Flags parameters or type parameters defined in code but missing from documentation.
*   **CSENSE003 / CSENSE005**: Flags "stray" tags referring to parameters that do not exist.
    *   **Code Fix:** An automatic code fix is available to remove stray tags. This fix supports **Fix All** in document, project, or solution.
*   **CSENSE008 / CSENSE010**: Enforces that the order of parameter tags in documentation matches the method signature.
    *   **Code Fix:** An automatic code fix is available to reorder tags to match the signature. This fix supports **Fix All** in document, project, or solution.
*   **CSENSE009 / CSENSE011**: Flags duplicate tags for the same parameter.
    *   **Code Fix:** An automatic code fix is available to remove duplicate tags. This fix supports **Fix All** in document, project, or solution.
*   **CSENSE020 / CSENSE021**: Flags parameter or type parameter names used in documentation text that are not wrapped in `<paramref />` or `<typeparamref />` tags.
    *   **Code Fix:** An automatic code fix is available to wrap these references in the appropriate tag. This fix supports **Fix All** in document, project, or solution.
    *   *Default:* Only flags complex names (camelCase, PascalCase, underscores, or digits).
    *   *Configurable:* Control the strictness using `comment_sense.ghost_references.mode`.

### Return Values
*   **CSENSE006**: Requires a `<returns>` tag for members that return a value (i.e., non-`void`, non-`Task`, non-`ValueTask`).
*   **CSENSE013**: Flags stray or duplicate `<returns>` tags on members that do not produce a documented return value (including `void`, `Task`, and `ValueTask` members), as well as on properties and indexers.
    *   **Code Fix:** An automatic code fix is available to remove stray tags. This fix supports **Fix All** in document, project, or solution.
### Exceptions
*   **CSENSE012**: Scans the method body for explicitly thrown exceptions (including static guard clauses like `ArgumentNullException.ThrowIfNull`) and ensures they are documented with `<exception>` tags.
    *   *Configurable:*
        *   Ignore exceptions using `comment_sense.ignored_exceptions`, `comment_sense.ignore_system_exceptions`, and `comment_sense.ignored_exception_namespaces`.
        *   Enable scanning of called methods and constructors for their documented exceptions using `comment_sense.scan_called_methods_for_exceptions = true`.
*   **CSENSE017**: Validates that the `cref` attribute in an `<exception>` tag refers to a valid Exception type.
*   **CSENSE023**: Flags stray `<exception>` tags that are nested within other tags or duplicated for the same exception type.
    *   **Code Fix:** An automatic code fix is available to remove stray tags. This fix supports **Fix All** in document, project, or solution.

### Properties
*   **CSENSE014**: Requires a `<value>` tag for properties.
    *   *Default:* Disabled.
*   **CSENSE015**: Flags stray or duplicate `<value>` tags.
    *   **Code Fix:** An automatic code fix is available to remove stray tags. This fix supports **Fix All** in document, project, or solution.

## Automatic Suppression
CommentSense automatically suppresses several built-in C# compiler diagnostics that overlap with its own rules. This prevents duplicate warnings and ensures a cleaner "Error List" experience.

The following diagnostics are suppressed globally by default:
*   **CS1591**: Missing XML comment for publicly visible type or member.
*   **CS1573**: Parameter has no matching param tag in the XML comment.
*   **CS1572**: XML comment has a param tag for a non-existent parameter.
*   **CS1571**: XML comment has a duplicate param tag.
*   **CS1584**: XML comment has syntactically incorrect cref attribute.
*   **CS1574**: XML comment has cref attribute that could not be resolved.
*   **CS1658**: Error in XML comment (e.g. syntax error in cref).

## Configuration
You can configure the analyzer behavior using an `.editorconfig` file in your project root or solution directory.

### Conditional Suppression
By default, CommentSense suppresses the above compiler warnings globally for the project. If you want to suppress them *only* for members that CommentSense is actively analyzing (based on visibility or other exclusions), enable this option.

This is useful if you exclude certain members (e.g. constants) from CommentSense but still want the standard C# compiler warnings to report issues for them.

```ini
[*.cs]
# Default: false (global suppression)
comment_sense.enable_conditional_suppression = true
```

### Low Quality Analysis
Specify criteria for what is considered "low quality" documentation.
```ini
[*.cs]
# Comma-separated list of terms (case-insensitive)
comment_sense.low_quality_terms = TODO, TBD, FixMe, None, N/A

# Minimum length for summary text (excluding trailing punctuation and whitespace)
comment_sense.min_summary_length = 10

# Whether to require summaries to end with punctuation (. ! ?)
comment_sense.require_ending_punctuation = true

# Whether to require documentation to start with a capital letter (if it starts with a letter)
comment_sense.require_capitalization = true

# Threshold (0.0 to 1.0) for similarity between documentation and member name.
# Setting this to 0.0 (default) disables similarity analysis.
# A value of 1.0 only flags documentation identical to the symbol name.
# Recommended: 0.7 to 0.8
comment_sense.similarity_threshold = 0.8
```

### Langword Analysis
Configure which C# keywords should be flagged for replacement with `<see langword="..." />`.
```ini
[*.cs]
# Comma-separated list of keywords (case-insensitive).
# Default: true, false, null, void
# Note: Specifying this option replaces the default list.
comment_sense.langwords = true, false, null, void, async, await
```

### Ghost Reference Analysis
Configure how strictly to detect parameter names mentioned in documentation text without `<paramref />` or `<typeparamref />` tags.
```ini
[*.cs]
# Options: safe, strict, off
# safe: Only flags complex names (camelCase, PascalCase, underscores, or digits). Ignores simple lowercase words (default).
# strict: Flags all matching parameter names regardless of casing or length.
# off: Disables ghost reference detection.
comment_sense.ghost_references.mode = safe
```

### Ignored Exceptions
Configure which exceptions should be ignored by the missing exception documentation rule (CSENSE012).
```ini
[*.cs]
# Comma-separated list of exception types (by name or full name)
comment_sense.ignored_exceptions = System.ArgumentNullException, ArgumentOutOfRangeException

# Whether to ignore all exceptions in the System namespace (default: false)
comment_sense.ignore_system_exceptions = true

# Comma-separated list of namespaces. Exceptions in these namespaces (or sub-namespaces) will be ignored.
comment_sense.ignored_exception_namespaces = MyProject.Internal, System.Data

# Whether to scan called methods and constructors for their documented exceptions (default: false).
# When enabled, CSENSE012 will also report exceptions documented in the XML comments of called members.
comment_sense.scan_called_methods_for_exceptions = true
```

### Visibility Level Analysis
Set the visibility threshold for members that should be analyzed.
```ini
[*.cs]
# Options: public, protected, internal, private
# public: only public members
# protected: public, protected, and protected internal (default)
# internal: public, protected, internal, and private protected
# private: all members
comment_sense.visibility_level = protected
```

### Constant Field Analysis
Skip documentation requirements for constant fields (disabled by default). Constants like `public const string Version = "1.0";` are often self-explanatory.
```ini
[*.cs]
comment_sense.exclude_constants = true
```

### Enum Member Analysis
Skip documentation requirements for enum members (disabled by default).
```ini
[*.cs]
comment_sense.exclude_enums = true
```

### Implicit Documentation Inheritance
By default, CommentSense allows skipping documentation for methods, properties, and events that override or implement base members, as they implicitly inherit documentation. This does not apply to types (classes, interfaces, etc.), which always require explicit documentation.

To disable this behavior and require explicit documentation (e.g., `<inheritdoc />`) for these members, set this option to `false`. This will trigger **CSENSE018** when documentation is missing.
```ini
[*.cs]
comment_sense.allow_implicit_inheritdoc = false
```

## Contributions
Contributions are welcome! Read the [contributing guide](CONTRIBUTING.md) to get started.

## License
This project is licensed under the [MIT License](LICENSE).
