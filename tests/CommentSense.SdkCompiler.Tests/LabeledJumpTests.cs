using NUnit.Framework;

namespace CommentSense.SdkCompiler.Tests;

public class LabeledJumpTests
{
    [TestCase("continue outer;", false)]
    [TestCase("break outer;", false)]
    [TestCase("continue;", false)]
    [TestCase("break;", false)]
    [TestCase("continue outer;", true)]
    [TestCase("break outer;", true)]
    public async Task PreservesExceptionAnalysis(string jump, bool documented)
    {
        var tag = documented ? "/// <exception cref=\"System.ArgumentException\">Invalid entry.</exception>" : "";
        using var test = new CompilerTest($$"""
            /// <summary>Processes entries.</summary>
            public class Worker
            {
                /// <summary>Processes available entries.</summary>
                {{tag}}
                public void Execute()
                {
                    outer: for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            if (j == 0) { {{jump}} }
                            if (System.DateTime.Now.Ticks > 0) throw new System.ArgumentException();
                        }
                    }
                }
            }
            """);
        await test.VerifyAsync(documented ? [] : ["CSENSE012"]);
    }

    [Test]
    public async Task SupportsLabeledSwitch()
    {
        using var test = new CompilerTest("""
            /// <summary>Processes entries.</summary>
            public class Worker
            {
                /// <summary>Processes available entries.</summary>
                public void Execute()
                {
                    outer: switch (System.DateTime.Now.Ticks)
                    {
                        case 0:
                            for (int i = 0; i < 2; i++) { break outer; }
                            break;
                        default: throw new System.ArgumentException();
                    }
                }
            }
            """);
        await test.VerifyAsync("CSENSE012");
    }
}
