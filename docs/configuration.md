# Configuration Reference

CommentSense options are configured via `.editorconfig`.

```ini
[*.cs]
comment_sense.visibility_level = protected
```

## Option reference

| Option                                             | Type                                                | Default                                                                                                   | Purpose                                                                                          |
|----------------------------------------------------|-----------------------------------------------------|-----------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------|
| `comment_sense.visibility_level`                   | enum (`public`, `protected`, `internal`, `private`) | `protected`                                                                                               | Sets minimum visibility that is analyzed.                                                        |
| `comment_sense.allow_implicit_inheritdoc`          | bool                                                | `true`                                                                                                    | Allows overrides/implementations to skip explicit docs when inheriting documentation implicitly. |
| `comment_sense.low_quality_terms`                  | CSV list                                            | empty                                                                                                     | Flags exact low-quality terms in documentation text.                                             |
| `comment_sense.min_summary_length`                 | int                                                 | `0`                                                                                                       | Minimum normalized documentation text length.                                                    |
| `comment_sense.require_ending_punctuation`         | bool                                                | `false`                                                                                                   | Requires docs to end with `.`, `!`, or `?`.                                                      |
| `comment_sense.require_capitalization`             | bool                                                | `false`                                                                                                   | Requires docs to start with a capital letter when starting with a letter.                        |
| `comment_sense.similarity_threshold`               | double (`0.0`-`1.0`)                                | `0.0`                                                                                                     | Flags text overly similar to symbol name. `0.0` disables similarity checks.                      |
| `comment_sense.rename_similarity_threshold`        | double (`0.0`-`1.0`)                                | `0.5`                                                                                                     | Controls fuzzy rename suggestions for stray tags/exception names.                                |
| `comment_sense.langwords`                          | CSV list                                            | `true,false,null,void`                                                                                    | Keywords that should use `<see langword="..." />`.                                               |
| `comment_sense.ghost_references.mode`              | enum (`safe`, `strict`, `off`)                      | `safe`                                                                                                    | Controls ghost reference strictness for parameter/type parameter names.                          |
| `comment_sense.ignored_exceptions`                 | CSV list                                            | empty                                                                                                     | Exception types ignored by missing exception documentation checks.                               |
| `comment_sense.ignore_system_exceptions`           | bool                                                | `false`                                                                                                   | Ignores all exceptions under `System.*`.                                                         |
| `comment_sense.ignored_exception_namespaces`       | CSV list                                            | empty                                                                                                     | Ignores exceptions under listed namespaces (and sub-namespaces).                                 |
| `comment_sense.scan_called_methods_for_exceptions` | bool                                                | `false`                                                                                                   | Includes documented exceptions from called methods/constructors.                                 |
| `comment_sense.require_property_patterns`          | bool                                                | `false`                                                                                                   | Enforces property summary prefixes (`Gets`, `Sets`, etc.).                                       |
| `comment_sense.exclude_constants`                  | bool                                                | `false`                                                                                                   | Excludes constant fields from documentation requirements.                                        |
| `comment_sense.exclude_enums`                      | bool                                                | `false`                                                                                                   | Excludes enum members from documentation requirements.                                           |
| `comment_sense.tag_order`                          | CSV list                                            | `inheritdoc, summary, typeparam, param, returns, value, exception, remarks, example, seealso, permission` | Defines expected top-level XML tag order.                                                        |
| `comment_sense.enable_conditional_suppression`     | bool                                                | `false`                                                                                                   | Suppresses overlapping compiler warnings only where CommentSense is active.                      |
| `comment_sense.analyze_internal` (deprecated)      | bool                                                | `false`                                                                                                   | Legacy option. Prefer `comment_sense.visibility_level = internal`.                               |

## Rule severity overrides

Use standard Roslyn rule severity configuration:

```ini
[*.cs]
dotnet_diagnostic.CSENSE014.severity = warning
dotnet_diagnostic.CSENSE027.severity = none
```

View [docs/getting-started.md](https://github.com/Thomas-Shephard/comment-sense/blob/main/docs/getting-started.md) for recommended settings.
