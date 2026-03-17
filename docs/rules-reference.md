# Rules Reference

CommentSense defines 28 diagnostics (`CSENSE000` to `CSENSE027`).

Default severities are shown below. You can override any rule with `dotnet_diagnostic.<ID>.severity` in `.editorconfig`.

| ID          | Category      | Default  | Code fix | Description                                                                    |
|-------------|---------------|----------|----------|--------------------------------------------------------------------------------|
| `CSENSE000` | Configuration | Warning  | No       | XML documentation parsing is disabled for part or all of the project.          |
| `CSENSE001` | General       | Warning  | Yes      | Missing documentation for an eligible symbol.                                  |
| `CSENSE002` | Parameters    | Warning  | Yes      | Missing `<param>` documentation.                                               |
| `CSENSE003` | Parameters    | Warning  | Yes      | Stray `<param>` documentation (name no longer exists or is misplaced).         |
| `CSENSE004` | Parameters    | Warning  | Yes      | Missing `<typeparam>` documentation.                                           |
| `CSENSE005` | Parameters    | Warning  | Yes      | Stray `<typeparam>` documentation.                                             |
| `CSENSE006` | Returns       | Warning  | Yes      | Missing `<returns>` documentation for a value-returning member.                |
| `CSENSE007` | References    | Warning  | Partial  | Unresolved XML `cref` reference.                                               |
| `CSENSE008` | Parameters    | Warning  | Yes      | `<param>` tags are out of signature order.                                     |
| `CSENSE009` | Parameters    | Warning  | Yes      | Duplicate `<param>` documentation for the same parameter.                      |
| `CSENSE010` | Parameters    | Warning  | Yes      | `<typeparam>` tags are out of signature order.                                 |
| `CSENSE011` | Parameters    | Warning  | Yes      | Duplicate `<typeparam>` documentation for the same type parameter.             |
| `CSENSE012` | Exceptions    | Warning  | Yes      | Exception thrown by code but not documented with `<exception>`.                |
| `CSENSE013` | Returns       | Warning  | Yes      | Stray or duplicate `<returns>` documentation.                                  |
| `CSENSE014` | Properties    | Disabled | Yes      | Missing `<value>` documentation for property/indexer.                          |
| `CSENSE015` | Properties    | Warning  | Yes      | Stray or duplicate `<value>` documentation.                                    |
| `CSENSE016` | General       | Warning  | Yes      | Low-quality documentation content.                                             |
| `CSENSE017` | Exceptions    | Warning  | Partial  | `<exception cref="...">` does not reference an exception type.                 |
| `CSENSE018` | Inheritance   | Warning  | Yes      | Inheriting member missing explicit docs when explicit inheritance is required. |
| `CSENSE019` | General       | Warning  | Yes      | Keyword should use `<see langword="..." />`.                                   |
| `CSENSE020` | Parameters    | Warning  | Yes      | Parameter name appears as plain text (ghost parameter reference).              |
| `CSENSE021` | Parameters    | Warning  | Yes      | Type parameter name appears as plain text (ghost type parameter reference).    |
| `CSENSE022` | General       | Warning  | Yes      | Stray, nested, or duplicate `<summary>` tag.                                   |
| `CSENSE023` | Exceptions    | Warning  | Yes      | Stray, nested, or duplicate `<exception>` tag.                                 |
| `CSENSE024` | General       | Warning  | Yes      | Top-level XML documentation tag order mismatch.                                |
| `CSENSE025` | References    | Warning  | No       | `cref` points to a symbol less accessible than the documented member.          |
| `CSENSE026` | Inheritance   | Warning  | No       | `<inheritdoc />` target is invalid or unresolved.                              |
| `CSENSE027` | Properties    | Warning  | No       | Property summary does not match the expected accessor pattern.                 |

## Notes on code fixes

- Most rules support **Fix All** (document/project/solution).
- `CSENSE007` and `CSENSE017` provide fixes only when they occur within an `<exception>` tag and CommentSense can infer a valid replacement.

## Automatic suppression

CommentSense suppresses overlapping compiler diagnostics by default:

- CS1591: Missing XML comment for publicly visible type or member.
- CS1573: Parameter has no matching param tag in the XML comment.
- CS1572: XML comment has a param tag for a non-existent parameter.
- CS1571: XML comment has a duplicate param tag.
- CS1584: XML comment has syntactically incorrect cref attribute.
- CS1574: XML comment has cref attribute that could not be resolved.
- CS1658: Error in XML comment (e.g. syntax error in cref).

Use `comment_sense.enable_conditional_suppression = true` to suppress those only for members actively analyzed by CommentSense.
