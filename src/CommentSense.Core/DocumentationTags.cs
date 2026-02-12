namespace CommentSense.Core;

internal static class DocumentationTags
{
    public const string Summary = "summary";
    public const string Remarks = "remarks";
    public const string Returns = "returns";
    public const string Value = "value";
    public const string Param = "param";
    public const string TypeParam = "typeparam";
    public const string Exception = "exception";
    public const string Example = "example";
    public const string SeeAlso = "seealso";
    public const string Permission = "permission";
    public const string InheritDoc = "inheritdoc";
    public const string Include = "include";
    public const string Code = "code";
    public const string C = "c";
    public const string ParamRef = "paramref";
    public const string TypeParamRef = "typeparamref";
    public const string See = "see";

    public static readonly IReadOnlyDictionary<string, int> TagOrder = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [InheritDoc] = -1,
        [Summary] = 0,
        [TypeParam] = 1,
        [Param] = 2,
        [Returns] = 3,
        [Value] = 3,
        [Exception] = 4,
        [Remarks] = 5,
        [Example] = 6,
        [SeeAlso] = 7,
        [Permission] = 8
    };
}
