using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

using CurlToCSharp.Extensions;
using CurlToCSharp.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Net.Http.Headers;

namespace CurlToCSharp.Services
{
    public class ConverterService : IConverterService
    {
        private const string RequestVariableName = "request";

        private const string HttpClientVariableName = "httpClient";

        private const string Base64AuthorizationVariableName = "base64authorization";

        public ConvertResult<string> ToCsharp(CurlOptions curlOptions)
        {
            var requestUsing = CreateRequestUsingStatement(curlOptions);
            var innerBlock = SyntaxFactory.Block();

            if (curlOptions.Files.Any())
            {
                var multipartContentStatements = CreateMultipartContentStatements(curlOptions);
                innerBlock = innerBlock.AddStatements(multipartContentStatements.ToArray());
            }
            else if (!string.IsNullOrWhiteSpace(curlOptions.Payload))
            {
                var assignmentExpression = CreateStringContentAssignmentExpression(curlOptions);
                innerBlock = innerBlock.AddStatements(assignmentExpression);
            }

            var statements = CreateHeaderAssignmentStatements(curlOptions);
            innerBlock = innerBlock.AddStatements(statements.ToArray());

            if (!string.IsNullOrEmpty(curlOptions.UserPasswordPair))
            {
                var basicAuthorizationStatements = CreateBasicAuthorizationStatements(curlOptions);
                innerBlock = innerBlock.AddStatements(basicAuthorizationStatements.ToArray());
            }

            var sendStatement = CreateSendStatement();
            innerBlock = innerBlock.AddStatements(sendStatement);

            var httpClientUsing = CreateHttpClientUsing();

            var csharp = httpClientUsing.WithStatement(SyntaxFactory.Block(requestUsing.WithStatement(innerBlock)))
                .NormalizeWhitespace()
                .ToFullString();

            return new ConvertResult<string>(csharp);
        }

        private ExpressionStatementSyntax CreateStringContentAssignmentExpression(CurlOptions curlOptions)
        {
            var stringContentCreation = CreateStringContentCreation(curlOptions);

            var contentAccessExpression = RoslynExtensions.CreateMemberAccessExpression(RequestVariableName, "Content");

            var assignmentExpressionSyntax = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                contentAccessExpression,
                stringContentCreation);

            return SyntaxFactory.ExpressionStatement(assignmentExpressionSyntax);
        }

        private IEnumerable<StatementSyntax> CreateMultipartContentStatements(CurlOptions curlOptions)
        {
            var statements = new LinkedList<StatementSyntax>();

            const string MultipartVariableName = "multipartContent";
            const string MultipartAddMethodName = "Add";

            statements.AddLast(
                SyntaxFactory.LocalDeclarationStatement(
                    RoslynExtensions.CreateVariableFromNewObjectExpression(
                        MultipartVariableName,
                        nameof(MultipartContent))));

            if (!string.IsNullOrEmpty(curlOptions.Payload))
            {
                var stringContentCreation = CreateStringContentCreation(curlOptions);
                var addStatement = SyntaxFactory.ExpressionStatement(
                    RoslynExtensions.CreateInvocationExpression(
                        MultipartVariableName,
                        MultipartAddMethodName,
                        SyntaxFactory.Argument(stringContentCreation)));
                statements.AddLast(addStatement);
            }

            foreach (var file in curlOptions.Files)
            {
                var fileReadExpression = RoslynExtensions.CreateInvocationExpression(
                    "File",
                    "ReadAllBytes",
                    RoslynExtensions.CreateStringLiteralArgument(file));

                var byteArrayContentExpression = RoslynExtensions.CreateObjectCreationExpression(
                    "ByteArrayContent",
                    SyntaxFactory.Argument(fileReadExpression));

                var addStatement = SyntaxFactory.ExpressionStatement(
                    RoslynExtensions.CreateInvocationExpression(
                        MultipartVariableName,
                        MultipartAddMethodName,
                        SyntaxFactory.Argument(byteArrayContentExpression)));

                statements.AddLast(addStatement);
            }

            return statements;
        }

        private ObjectCreationExpressionSyntax CreateStringContentCreation(CurlOptions curlOptions)
        {
            var arguments = new LinkedList<ArgumentSyntax>();
            arguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(curlOptions.Payload));

            var contentHeader = curlOptions.Headers.GetCommaSeparatedValues(HeaderNames.ContentType);
            if (contentHeader.Any())
            {
                arguments.AddLast(SyntaxFactory.Argument(RoslynExtensions.CreateMemberAccessExpression("Encoding", "UTF8")));
                arguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(contentHeader.First()));
            }

            var stringContentCreation = RoslynExtensions.CreateObjectCreationExpression("StringContent", arguments.ToArray());
            return stringContentCreation;
        }

        private IEnumerable<StatementSyntax> CreateHeaderAssignmentStatements(CurlOptions options)
        {
            if (!options.Headers.Any())
            {
                return Enumerable.Empty<ExpressionStatementSyntax>();
            }

            var statements = new LinkedList<ExpressionStatementSyntax>();
            foreach (var header in options.Headers)
            {
                if (header.Key == HeaderNames.ContentType)
                {
                    continue;
                }

                var tryAddHeaderStatement = CreateTryAddHeaderStatement(
                    RoslynExtensions.CreateStringLiteralArgument(header.Key),
                    RoslynExtensions.CreateStringLiteralArgument(header.Value));

                statements.AddLast(tryAddHeaderStatement);
            }

            return statements;
        }

        private IEnumerable<StatementSyntax> CreateBasicAuthorizationStatements(CurlOptions options)
        {
            var authorizationEncodingStatement = CreateBasicAuthorizationEncodingStatement(options);
            var stringStartToken = SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken);

            var interpolatedStringContentSyntaxs = new SyntaxList<InterpolatedStringContentSyntax>()
                .Add(SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken, "Basic ", null, SyntaxTriviaList.Empty)))
                .Add(SyntaxFactory.Interpolation(SyntaxFactory.IdentifierName(Base64AuthorizationVariableName)));

            var interpolatedStringArgument = SyntaxFactory.Argument(SyntaxFactory.InterpolatedStringExpression(stringStartToken, interpolatedStringContentSyntaxs));
            var tryAddHeaderStatement = CreateTryAddHeaderStatement(RoslynExtensions.CreateStringLiteralArgument("Authorization"), interpolatedStringArgument);

            return new StatementSyntax[] { authorizationEncodingStatement, tryAddHeaderStatement };
        }

        private LocalDeclarationStatementSyntax CreateBasicAuthorizationEncodingStatement(CurlOptions options)
        {
            var asciiGetBytesInvocation = RoslynExtensions.CreateInvocationExpression(
                "Encoding",
                "ASCII",
                "GetBytes",
                RoslynExtensions.CreateStringLiteralArgument(options.UserPasswordPair));

            var convertToBase64Invocation = RoslynExtensions.CreateInvocationExpression(
                "Convert",
                "ToBase64String",
                SyntaxFactory.Argument(asciiGetBytesInvocation));

            var declarationSyntax = RoslynExtensions.CreateVariableInitializationExpression(
                Base64AuthorizationVariableName,
                convertToBase64Invocation);

            return SyntaxFactory.LocalDeclarationStatement(declarationSyntax);
        }

        private ExpressionStatementSyntax CreateTryAddHeaderStatement(ArgumentSyntax keyArgumentSyntax, ArgumentSyntax valueArgumentSyntax)
        {
            var invocationExpressionSyntax = RoslynExtensions.CreateInvocationExpression(
                RequestVariableName,
                "Headers",
                "TryAddWithoutValidation",
                keyArgumentSyntax,
                valueArgumentSyntax);

            return SyntaxFactory.ExpressionStatement(invocationExpressionSyntax);
        }

        private UsingStatementSyntax CreateHttpClientUsing()
        {
            return RoslynExtensions.CreateUsingStatement(HttpClientVariableName, nameof(HttpClient));
        }

        private UsingStatementSyntax CreateRequestUsingStatement(CurlOptions curlOptions)
        {
            var methodNameArgument = RoslynExtensions.CreateStringLiteralArgument(curlOptions.HttpMethod);
            var httpMethodArgument = RoslynExtensions.CreateObjectCreationExpression(nameof(HttpMethod), methodNameArgument);

            var urlArgument = RoslynExtensions.CreateStringLiteralArgument(curlOptions.Url.ToString());

            return RoslynExtensions.CreateUsingStatement(
                RequestVariableName,
                nameof(HttpRequestMessage),
                SyntaxFactory.Argument(httpMethodArgument),
                urlArgument);
        }

        private LocalDeclarationStatementSyntax CreateSendStatement()
        {
            var invocationExpressionSyntax = RoslynExtensions.CreateInvocationExpression(
                HttpClientVariableName,
                "SendAsync",
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(RequestVariableName)));

            var awaitExpression = SyntaxFactory.AwaitExpression(invocationExpressionSyntax);

            var declarationSyntax = RoslynExtensions.CreateVariableInitializationExpression("response", awaitExpression);

            return SyntaxFactory.LocalDeclarationStatement(declarationSyntax);
        }
    }
}
