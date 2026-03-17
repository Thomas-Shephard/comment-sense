# Getting Started with CommentSense

This guide is to help teams start using CommentSense in an existing or new C# codebase.

## Install the package

```bash
dotnet add package CommentSense
```

Or add it directly in your project file:

```xml
<ItemGroup>
  <PackageReference Include="CommentSense" Version="x.y.z" />
</ItemGroup>
```

## Enable XML documentation generation

CommentSense relies on compiler XML documentation parsing.

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

Without this, you will get a `CSENSE000` warning.

## Start with a baseline `.editorconfig`

```ini
[*.cs]
comment_sense.visibility_level = protected
comment_sense.require_capitalization = true
comment_sense.require_ending_punctuation = true
```

## Run and apply fixes

After package restore, diagnostics appear in your IDE/build output.

Common first diagnostics:

- `CSENSE001`: missing symbol documentation
- `CSENSE002` / `CSENSE004`: missing parameter/type parameter documentation
- `CSENSE006`: missing return value documentation
- `CSENSE012`: missing exception documentation
- `CSENSE016`: low-quality documentation

## Roll out gradually (recommended)

For large repositories, start narrow and tighten over time:

1. Start with `visibility_level = public`.
2. Move to `protected` (default) after initial cleanup.
3. Enable stricter options such as `similarity_threshold` and `require_property_patterns`.

You can also tune per-rule severity:

```ini
[*.cs]
dotnet_diagnostic.CSENSE014.severity = warning
dotnet_diagnostic.CSENSE027.severity = none
```
