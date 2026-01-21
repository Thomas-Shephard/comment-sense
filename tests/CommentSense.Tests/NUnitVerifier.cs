using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Tests;

public class NUnitVerifier : IVerifier
{
    public void Empty<T>(string collectionName, IEnumerable<T> collection)
    {
        Assert.That(collection, Is.Empty, $"'{collectionName}' should be empty.");
    }

    public void Equal<T>(T expected, T actual, string? message = null)
    {
        Assert.That(actual, Is.EqualTo(expected), message);
    }

    public void True(bool assert, string? message = null)
    {
        Assert.That(assert, Is.True, message);
    }

    public void False(bool assert, string? message = null)
    {
        Assert.That(assert, Is.False, message);
    }

    [DoesNotReturn]
    public void Fail(string? message = null)
    {
        Assert.Fail(message ?? "Verification failed");
        throw new InvalidOperationException("Unreachable");
    }

    public void LanguageIsSupported(string language)
    {
        Assert.That(language, Is.EqualTo(LanguageNames.CSharp).Or.EqualTo(LanguageNames.VisualBasic));
    }

    public void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
    {
        Assert.That(collection, Is.Not.Empty, $"'{collectionName}' should not be empty.");
    }

    public void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? equalityComparer = null, string? message = null)
    {
        var constraint = Is.EqualTo(expected);
        if (equalityComparer != null)
            constraint = constraint.Using(equalityComparer);
        Assert.That(actual, constraint, message);
    }

    public IVerifier PushContext(string context)
    {
        return this;
    }
}
