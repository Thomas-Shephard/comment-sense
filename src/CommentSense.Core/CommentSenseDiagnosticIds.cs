namespace CommentSense.Core;

internal static class CommentSenseDiagnosticIds
{
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
}
