# Examples

This page shows common before/after patterns when adopting CommentSense.

## Missing member documentation

Before:

```csharp
public class Calculator
{
    public int Add(int left, int right) => left + right;
}
```

After:

```csharp
/// <summary>Provides arithmetic helpers.</summary>
public class Calculator
{
    /// <summary>Adds two numbers.</summary>
    public int Add(int left, int right) => left + right;
}
```

## Missing parameter tags

Before:

```csharp
/// <summary>Adds two numbers.</summary>
/// <param name="lft">First value.</param>
public int Add(int left, int right) => left + right;
```

After:

```csharp
/// <summary>Adds two numbers.</summary>
/// <param name="left">First value.</param>
/// <param name="right">Second value.</param>
public int Add(int left, int right) => left + right;
```

## Returns and property value

Before:

```csharp
/// <summary>Gets the current count.</summary>
public int Count { get; }

/// <summary>Creates a cache key.</summary>
public string BuildKey() => "k";
```

After:

```csharp
/// <summary>Gets the current count.</summary>
/// <value>The number of cached entries.</value>
public int Count { get; }

/// <summary>Creates a cache key.</summary>
/// <returns>The generated cache key.</returns>
public string BuildKey() => "k";
```

## Missing exception documentation

Before:

```csharp
/// <summary>Loads a user profile.</summary>
public Profile Load(string id)
{
    if (string.IsNullOrWhiteSpace(id))
    {
        throw new ArgumentException("Id is required.", nameof(id));
    }

    return repository.Load(id);
}
```

After:

```csharp
/// <summary>Loads a user profile.</summary>
/// <param name="id">The profile identifier.</param>
/// <returns>The loaded profile.</returns>
/// <exception cref="System.ArgumentException">Thrown when <paramref name="id" /> is empty.</exception>
public Profile Load(string id)
{
    if (string.IsNullOrWhiteSpace(id))
    {
        throw new ArgumentException("Id is required.", nameof(id));
    }

    return repository.Load(id);
}
```

## Langword and parameter references

Before:

```csharp
/// <summary>Returns true when item is null.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="item">The item to inspect.</param>
public static bool IsMissing<T>(T item) => item is null;
```

After:

```csharp
/// <summary>Returns <see langword="true" /> when <paramref name="item" /> is <see langword="null" />.</summary>
/// <typeparam name="T">The type being inspected.</typeparam>
/// <param name="item">The item to inspect.</param>
public static bool IsMissing<T>(T item) => item is null;
```

## Explicit inheritance docs

If you require explicit inheritance docs:

```ini
[*.cs]
comment_sense.allow_implicit_inheritdoc = false
```

Before:

```csharp
public class DerivedService : BaseService
{
    public override void Execute() { }
}
```

After:

```csharp
public class DerivedService : BaseService
{
    /// <inheritdoc />
    public override void Execute() { }
}
```

## Property summary pattern

Enable pattern checking:

```ini
[*.cs]
comment_sense.require_property_patterns = true
```

Before:

```csharp
/// <summary>Whether retries are enabled.</summary>
public bool IsRetryEnabled { get; set; }
```

After:

```csharp
/// <summary>Gets or sets a value indicating whether retries are enabled.</summary>
public bool IsRetryEnabled { get; set; }
```
