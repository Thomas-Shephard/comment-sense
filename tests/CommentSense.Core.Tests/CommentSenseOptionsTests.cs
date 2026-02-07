using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace CommentSense.Core.Tests;

public class CommentSenseOptionsTests
{
    [Test]
    public void AnalyzerOptionsGlobalFallback()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>());
        var globalOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "15",
            ["comment_sense.require_ending_punctuation"] = "true",
            ["comment_sense.similarity_threshold"] = "0.75",
            ["comment_sense.low_quality_terms"] = "BAD, TERMS",
            ["comment_sense.ignored_exceptions"] = "Ex1, Ex2",
            ["comment_sense.ignore_system_exceptions"] = "true",
            ["comment_sense.ignored_exception_namespaces"] = "System.Text",
            ["comment_sense.visibility_level"] = "Internal",
            ["comment_sense.allow_implicit_inheritdoc"] = "false",
            ["comment_sense.exclude_constants"] = "true"
        });

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.MinSummaryLength, Is.EqualTo(15));
            Assert.That(options.RequireEndingPunctuation, Is.True);
            Assert.That(options.SimilarityThreshold, Is.EqualTo(0.75));
            Assert.That(options.LowQualityTerms, Contains.Item("BAD"));
            Assert.That(options.IgnoredExceptions, Contains.Item("Ex1"));
            Assert.That(options.IgnoreSystemExceptions, Is.True);
            Assert.That(options.IgnoredExceptionNamespaces, Contains.Item("System.Text"));
            Assert.That(options.VisibilityLevel, Is.EqualTo(VisibilityLevel.Internal));
            Assert.That(options.AllowImplicitInheritDoc, Is.False);
            Assert.That(options.ExcludeConstants, Is.True);
        }
    }

    [Test]
    public void AnalyzerOptionsLocalOverGlobal()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "20",
            ["comment_sense.visibility_level"] = "Public"
        });
        var globalOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "15",
            ["comment_sense.visibility_level"] = "Internal"
        });

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.MinSummaryLength, Is.EqualTo(20));
            Assert.That(options.VisibilityLevel, Is.EqualTo(VisibilityLevel.Public));
        }
    }

    [Test]
    public void AnalyzerOptionsFallbackOnInvalidLocal()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "not-an-int",
            ["comment_sense.visibility_level"] = "InvalidLevel",
            ["comment_sense.require_ending_punctuation"] = "not-a-bool"
        });
        var globalOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "15",
            ["comment_sense.visibility_level"] = "Internal",
            ["comment_sense.require_ending_punctuation"] = "true"
        });

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.MinSummaryLength, Is.EqualTo(15));
            Assert.That(options.VisibilityLevel, Is.EqualTo(VisibilityLevel.Internal));
            Assert.That(options.RequireEndingPunctuation, Is.True);
        }
    }

    [Test]
    public void AnalyzerOptionsEmptyAndWhiteSpace()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "",
            ["comment_sense.visibility_level"] = "   "
        });
        var globalOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "15",
            ["comment_sense.visibility_level"] = "Internal"
        });

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.MinSummaryLength, Is.EqualTo(15));
            Assert.That(options.VisibilityLevel, Is.EqualTo(VisibilityLevel.Internal));
        }
    }

    [Test]
    public void AnalyzerOptionsSimilarityThresholdEdgeCases()
    {
        var configLow = new Dictionary<string, string> { ["comment_sense.similarity_threshold"] = "-0.5" };
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var optionsLow = CommentSenseOptions.GetOptions(new CustomProvider(new MapOptions(configLow), new MapOptions(new Dictionary<string, string>())), null!);

        var configHigh = new Dictionary<string, string> { ["comment_sense.similarity_threshold"] = "1.5" };
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var optionsHigh = CommentSenseOptions.GetOptions(new CustomProvider(new MapOptions(configHigh), new MapOptions(new Dictionary<string, string>())), null!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(optionsLow.SimilarityThreshold, Is.Zero);
            Assert.That(optionsHigh.SimilarityThreshold, Is.EqualTo(1.0));
        }
    }

    [Test]
    public void AnalyzerOptionsGlobalInvalidFallback()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>());
        var globalOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "not-an-int",
            ["comment_sense.visibility_level"] = "   "
        });

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.MinSummaryLength, Is.Zero);
            Assert.That(options.VisibilityLevel, Is.EqualTo(VisibilityLevel.Protected));
        }
    }

    [Test]
    public void AnalyzerOptionsGhostReferenceMode()
    {
        var globalOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.ghost_references.mode"] = "Strict"
        });

        // Test global fallback
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var optionsGlobal = CommentSenseOptions.GetOptions(new CustomProvider(new MapOptions(new Dictionary<string, string>()), globalOptions), null!);
        Assert.That(optionsGlobal.GhostReferenceMode, Is.EqualTo(GhostReferenceMode.Strict));

        // Test local override
        var localOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.ghost_references.mode"] = "Off"
        });
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var optionsLocal = CommentSenseOptions.GetOptions(new CustomProvider(localOptions, globalOptions), null!);
        Assert.That(optionsLocal.GhostReferenceMode, Is.EqualTo(GhostReferenceMode.Off));

        // Test invalid fallback (should use global if valid, or default if global also invalid)
        var invalidLocal = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.ghost_references.mode"] = "InvalidValue"
        });
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var optionsInvalid = CommentSenseOptions.GetOptions(new CustomProvider(invalidLocal, globalOptions), null!);
        Assert.That(optionsInvalid.GhostReferenceMode, Is.EqualTo(GhostReferenceMode.Strict));

        // Test default fallback
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var optionsDefault = CommentSenseOptions.GetOptions(new CustomProvider(new MapOptions(new Dictionary<string, string>()), new MapOptions(new Dictionary<string, string>())), null!);
        Assert.That(optionsDefault.GhostReferenceMode, Is.EqualTo(GhostReferenceMode.Safe));
    }

    [Test]
    public void VisibilityLevelBackwardCompatibility()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.analyze_internal"] = "true"
        });
        var globalOptions = new MapOptions(new Dictionary<string, string>());

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);

        Assert.That(options.VisibilityLevel, Is.EqualTo(VisibilityLevel.Internal));
    }

    [Test]
    public void AnalyzerOptionsVisibilityLevelBackwardCompatibilityNoOption()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.analyze_internal"] = "false"
        });
        var globalOptions = new MapOptions(new Dictionary<string, string>());

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);

        Assert.That(options.VisibilityLevel, Is.EqualTo(VisibilityLevel.Protected));
    }

    [Test]
    public void VisibilityLevelPrecedenceOverLegacyOption()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.visibility_level"] = "Public",
            ["comment_sense.analyze_internal"] = "true"
        });
        var globalOptions = new MapOptions(new Dictionary<string, string>());

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);

        Assert.That(options.VisibilityLevel, Is.EqualTo(VisibilityLevel.Public));
    }

    [Test]
    public void VisibilityLevelLegacyOptionInGlobal()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>());
        var globalOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.analyze_internal"] = "true"
        });

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);

        Assert.That(options.VisibilityLevel, Is.EqualTo(VisibilityLevel.Internal));
    }

    [Test]
    public void OptionExistsOnlyInGlobal()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>());
        var globalOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.require_ending_punctuation"] = "true",
            ["comment_sense.low_quality_terms"] = "BadGlobal"
        });

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.RequireEndingPunctuation, Is.True);
            Assert.That(options.LowQualityTerms, Contains.Item("BadGlobal"));
        }
    }

    private sealed class MapOptions(IDictionary<string, string> map) : AnalyzerConfigOptions
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        public override bool TryGetValue(string key, out string value) => map.TryGetValue(key, out value!);
    }

    private sealed class CustomProvider(AnalyzerConfigOptions local, AnalyzerConfigOptions global) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => global;
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => local;
        public override AnalyzerConfigOptions GetOptions(AdditionalText text) => local;
    }
}
