using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace CommentSense.Core.Tests;

public class CommentSenseOptionsTests
{
    [Test]
    public void GetOptionsAllPropertiesPresentReturnsCorrectOptions()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.langwords"] = "word1, word2",
            ["comment_sense.exclude_enums"] = "true",
            ["comment_sense.enable_conditional_suppression"] = "true",
            ["comment_sense.scan_called_methods_for_exceptions"] = "true",
            ["comment_sense.low_quality_terms"] = "LocalTerm"
        });
        var globalOptions = new MapOptions(new Dictionary<string, string>());

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.Langwords, Contains.Item("word1"));
            Assert.That(options.Langwords, Contains.Item("word2"));
            Assert.That(options.ExcludeEnums, Is.True);
            Assert.That(options.EnableConditionalSuppression, Is.True);
            Assert.That(options.ScanCalledMethodsForExceptions, Is.True);
            Assert.That(options.LowQualityTerms, Contains.Item("LocalTerm"));
        }
    }

    [Test]
    public void GetOptionsGlobalFallbackReturnsGlobalValues()
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
    public void GetOptionsLocalOverGlobalReturnsLocalValues()
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
    public void GetOptionsInvalidLocalFallbackReturnsGlobalValues()
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
    public void GetOptionsEmptyAndWhiteSpaceReturnsGlobalValues()
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
    public void GetOptionsSimilarityThresholdEdgeCasesClampsToValidRange()
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
    public void GetOptionsGlobalInvalidFallbackReturnsDefaultValues()
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
    public void GetOptionsGhostReferenceModeFollowsPrecedenceAndFallback()
    {
        var globalOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.ghost_references.mode"] = "Strict"
        });

        // ReSharper disable once NullableWarningSuppressionIsUsed
        var optionsGlobal = CommentSenseOptions.GetOptions(new CustomProvider(new MapOptions(new Dictionary<string, string>()), globalOptions), null!);
        Assert.That(optionsGlobal.GhostReferenceMode, Is.EqualTo(GhostReferenceMode.Strict));

        var localOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.ghost_references.mode"] = "Off"
        });
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var optionsLocal = CommentSenseOptions.GetOptions(new CustomProvider(localOptions, globalOptions), null!);
        Assert.That(optionsLocal.GhostReferenceMode, Is.EqualTo(GhostReferenceMode.Off));

        var invalidLocal = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.ghost_references.mode"] = "InvalidValue"
        });
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var optionsInvalid = CommentSenseOptions.GetOptions(new CustomProvider(invalidLocal, globalOptions), null!);
        Assert.That(optionsInvalid.GhostReferenceMode, Is.EqualTo(GhostReferenceMode.Strict));

        // ReSharper disable once NullableWarningSuppressionIsUsed
        var optionsDefault = CommentSenseOptions.GetOptions(new CustomProvider(new MapOptions(new Dictionary<string, string>()), new MapOptions(new Dictionary<string, string>())), null!);
        Assert.That(optionsDefault.GhostReferenceMode, Is.EqualTo(GhostReferenceMode.Safe));
    }

    [Test]
    public void VisibilityLevelLegacyAnalyzeInternalMaintainsBackwardCompatibility()
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
    public void VisibilityLevelLegacyAnalyzeInternalFalseDefaultsToProtected()
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
    public void VisibilityLevelModernOptionPrecedenceOverridesLegacyOption()
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
    public void VisibilityLevelLegacyOptionInGlobalReturnsInternal()
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
    public void DefaultPropertyAccessReturnsDefaultValues()
    {
        var options = CommentSenseOptions.Default;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.Langwords, Is.Not.Empty);
            Assert.That(options.ExcludeEnums, Is.False);
            Assert.That(options.EnableConditionalSuppression, Is.False);
            Assert.That(options.ScanCalledMethodsForExceptions, Is.False);
        }
    }

    [Test]
    public void ParseSetEdgeCasesReturnsCorrectSet()
    {
        var set = CommentSenseOptionsLoader.ParseSet("  term1  ,  ,  term2  ");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(set, Has.Count.EqualTo(2));
            Assert.That(set, Contains.Item("term1"));
            Assert.That(set, Contains.Item("term2"));
        }
    }

    [Test]
    public void GetDoubleOptionNonExistentKeyReturnsDefault()
    {
        var options = new MapOptions(new Dictionary<string, string>());
        var global = new MapOptions(new Dictionary<string, string>());
        var val = CommentSenseOptionsLoader.GetDoubleOption(options, global, "nonexistent", 0.5);
        Assert.That(val, Is.EqualTo(0.5));
    }

    [Test]
    public void GetIntOptionNonExistentKeyReturnsDefault()
    {
        var options = new MapOptions(new Dictionary<string, string>());
        var global = new MapOptions(new Dictionary<string, string>());
        var val = CommentSenseOptionsLoader.GetIntOption(options, global, "nonexistent", 42);
        Assert.That(val, Is.EqualTo(42));
    }

    [Test]
    public void GetBoolOptionNonExistentKeyReturnsDefault()
    {
        var options = new MapOptions(new Dictionary<string, string>());
        var global = new MapOptions(new Dictionary<string, string>());
        var val = CommentSenseOptionsLoader.GetBoolOption(options, global, "nonexistent", true);
        Assert.That(val, Is.True);
    }

    [Test]
    public void GetBoolOptionKeyInGlobalReturnsGlobalValue()
    {
        var options = new MapOptions(new Dictionary<string, string>());
        var global = new MapOptions(new Dictionary<string, string> { ["comment_sense.test"] = "true" });
        var val = CommentSenseOptionsLoader.GetBoolOption(options, global, "test");
        Assert.That(val, Is.True);
    }

    [Test]
    public void GetEnumOptionKeyInGlobalReturnsGlobalValue()
    {
        var options = new MapOptions(new Dictionary<string, string>());
        var global = new MapOptions(new Dictionary<string, string> { ["comment_sense.visibility_level"] = "Internal" });
        var val = CommentSenseOptionsLoader.GetEnumOption(options, global, "visibility_level", VisibilityLevel.Public);
        Assert.That(val, Is.EqualTo(VisibilityLevel.Internal));
    }

    [Test]
    public void GetSetOptionKeyInGlobalReturnsGlobalValue()
    {
        var options = new MapOptions(new Dictionary<string, string>());
        var global = new MapOptions(new Dictionary<string, string> { ["comment_sense.terms"] = "a,b" });
        var val = CommentSenseOptionsLoader.GetSetOption(options, global, "terms", ImmutableHashSet<string>.Empty);
        Assert.That(val, Contains.Item("a"));
    }

    [Test]
    public void HasOptionVariousBranchesReturnsCorrectResults()
    {
        var options = new MapOptions(new Dictionary<string, string> { ["comment_sense.local"] = "val" });
        var global = new MapOptions(new Dictionary<string, string> { ["comment_sense.global"] = "val" });
        var empty = new MapOptions(new Dictionary<string, string>());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CommentSenseOptionsLoader.HasOption(options, empty, "local"), Is.True);
            Assert.That(CommentSenseOptionsLoader.HasOption(empty, global, "global"), Is.True);
            Assert.That(CommentSenseOptionsLoader.HasOption(empty, empty, "none"), Is.False);
        }
    }

    [Test]
    public void RecordMethodsEqualityAndDestructuringBehavesCorrectly()
    {
        var o1 = CommentSenseOptions.Default;
        var o2 = o1 with { MinSummaryLength = 100 };
        var o3 = CommentSenseOptions.Default;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(o1, Is.Not.EqualTo(o2));
            Assert.That(o1, Is.EqualTo(o3));
            Assert.That(o1.GetHashCode(), Is.EqualTo(o3.GetHashCode()));
            Assert.That(o1.ToString(), Is.Not.Null);
            var (_, _, _, _, _, _, _, minSummaryLength, _, _, _, _, _, _, _, _, _) = o1;
            Assert.That(minSummaryLength, Is.EqualTo(o1.MinSummaryLength));
        }
    }

    [Test]
    public void LoadRenameSimilarityThreshold()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.rename_similarity_threshold"] = "0.75"
        });
        var globalOptions = new MapOptions(new Dictionary<string, string>());
        var provider = new CustomProvider(localOptions, globalOptions);

        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = CommentSenseOptions.GetOptions(provider, null!);
        Assert.That(options.RenameSimilarityThreshold, Is.EqualTo(0.75));
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
