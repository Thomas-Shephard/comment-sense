namespace CommentSense.Core;

internal static class CommentSenseDiagnosticIds
{
    // Rules
    public const string DisabledDocumentationParsingId = "CSENSE000";
    public const string MissingDocumentationId = "CSENSE001";
    public const string MissingParameterDocumentationId = "CSENSE002";
    public const string StrayParameterDocumentationId = "CSENSE003";
    public const string MissingTypeParameterDocumentationId = "CSENSE004";
    public const string StrayTypeParameterDocumentationId = "CSENSE005";
    public const string MissingReturnValueDocumentationId = "CSENSE006";
    public const string UnresolvedCrefId = "CSENSE007";
    public const string ParameterOrderMismatchId = "CSENSE008";
    public const string DuplicateParameterDocumentationId = "CSENSE009";
    public const string TypeParameterOrderMismatchId = "CSENSE010";
    public const string DuplicateTypeParameterDocumentationId = "CSENSE011";
    public const string MissingExceptionDocumentationId = "CSENSE012";
    public const string StrayReturnValueDocumentationId = "CSENSE013";
    public const string MissingValueDocumentationId = "CSENSE014";
    public const string StrayValueDocumentationId = "CSENSE015";
    public const string LowQualityDocumentationId = "CSENSE016";
    public const string InvalidExceptionTypeId = "CSENSE017";
    public const string MissingInheritDocId = "CSENSE018";
    public const string UseLangwordId = "CSENSE019";
    public const string GhostParameterReferenceId = "CSENSE020";
    public const string GhostTypeParameterReferenceId = "CSENSE021";
    public const string StraySummaryDocumentationId = "CSENSE022";
    public const string StrayExceptionDocumentationId = "CSENSE023";
    public const string DocumentationTagOrderMismatchId = "CSENSE024";
    public const string InaccessibleCrefId = "CSENSE025";
    public const string InvalidInheritDocTargetId = "CSENSE026";
    public const string PropertySummaryPatternId = "CSENSE027";

    // Suppressions
    public const string SuppressMissingXmlCommentId = "CSENSESUP001";
    public const string SuppressMissingParamTagId = "CSENSESUP002";
    public const string SuppressStrayParamTagId = "CSENSESUP003";
    public const string SuppressDuplicateParamTagId = "CSENSESUP004";
    public const string SuppressInvalidCrefId = "CSENSESUP005";
    public const string SuppressUnresolvedCrefId = "CSENSESUP006";
    public const string SuppressInvalidCrefSecondaryId = "CSENSESUP007";
}
