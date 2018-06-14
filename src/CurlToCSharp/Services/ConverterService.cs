using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

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
            var requestUsing = CreateRequestUsing(curlOptions);
            var innerBlock = SyntaxFactory.Block();
            if (!string.IsNullOrWhiteSpace(curlOptions.Payload))
            {
                var assignmentExpression = CreateContentAssignmentExpression(curlOptions);
                innerBlock = innerBlock.AddStatements(assignmentExpression);

                var statements = CreateHeaderAssignmentStatements(curlOptions);
                innerBlock = innerBlock.AddStatements(statements.ToArray());
            }

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

        private ExpressionStatementSyntax CreateContentAssignmentExpression(CurlOptions curlOptions)
        {
            var stringContentCreation = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("StringContent"));

            var payloadArgument = SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(curlOptions.Payload)));
            var argumentSyntaxs = new SeparatedSyntaxList<ArgumentSyntax>();
            argumentSyntaxs = argumentSyntaxs.Add(payloadArgument);

            var contentHeader = curlOptions.Headers.GetCommaSeparatedValues(HeaderNames.ContentType);
            if (contentHeader.Any())
            {
                argumentSyntaxs = argumentSyntaxs.Add(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));

                argumentSyntaxs = argumentSyntaxs.Add(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(contentHeader.First()))));
            }

            stringContentCreation = stringContentCreation.WithArgumentList(SyntaxFactory.ArgumentList(argumentSyntaxs));

            var contentAccessExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(RequestVariableName),
                SyntaxFactory.IdentifierName("Content"));

            var assignmentExpressionSyntax = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                contentAccessExpression,
                stringContentCreation);

            return SyntaxFactory.ExpressionStatement(assignmentExpressionSyntax);
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
                    CreateStringLiteralArgument(header.Key),
                    CreateStringLiteralArgument(header.Value));

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
            var tryAddHeaderStatement = CreateTryAddHeaderStatement(CreateStringLiteralArgument("Authorization"), interpolatedStringArgument);

            return new StatementSyntax[] { authorizationEncodingStatement, tryAddHeaderStatement };
        }

        private LocalDeclarationStatementSyntax CreateBasicAuthorizationEncodingStatement(CurlOptions options)
        {
            var asciiGetBytesAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Encoding"),
                    SyntaxFactory.IdentifierName("ASCII")),
                SyntaxFactory.IdentifierName("GetBytes"));

            var asciiGetBytesArguments = new SeparatedSyntaxList<ArgumentSyntax>();
            asciiGetBytesArguments = asciiGetBytesArguments.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(options.UserPasswordPair))));

            var asciiGetBytesInvocation = SyntaxFactory.InvocationExpression(
                asciiGetBytesAccess,
                SyntaxFactory.ArgumentList(asciiGetBytesArguments));

            var convertToBase64Access = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Convert"),
                SyntaxFactory.IdentifierName("ToBase64String"));

            var convertToBase64AccessArguments = new SeparatedSyntaxList<ArgumentSyntax>();
            convertToBase64AccessArguments =
                convertToBase64AccessArguments.Add(SyntaxFactory.Argument(asciiGetBytesInvocation));

            var convertToBase64Invocation = SyntaxFactory.InvocationExpression(
                convertToBase64Access,
                SyntaxFactory.ArgumentList(convertToBase64AccessArguments));

            var declarationSyntax = SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .AddVariables(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(Base64AuthorizationVariableName),
                        null,
                        SyntaxFactory.EqualsValueClause(convertToBase64Invocation)));

            return SyntaxFactory.LocalDeclarationStatement(declarationSyntax);
        }

        private ExpressionStatementSyntax CreateTryAddHeaderStatement(ArgumentSyntax keyArgumentSyntax, ArgumentSyntax valueArgumentSyntax)
        {
            var headerAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(RequestVariableName),
                SyntaxFactory.IdentifierName("Headers"));
            var headerAddExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                headerAccess,
                SyntaxFactory.IdentifierName("TryAddWithoutValidation"));

            var separatedSyntaxList = new SeparatedSyntaxList<ArgumentSyntax>();
            separatedSyntaxList = separatedSyntaxList.Add(keyArgumentSyntax);
            separatedSyntaxList = separatedSyntaxList.Add(valueArgumentSyntax);

            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    headerAddExpression,
                    SyntaxFactory.ArgumentList(separatedSyntaxList)));
        }

        private UsingStatementSyntax CreateHttpClientUsing()
        {
            var objectCreationExpressionSyntax = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(nameof(HttpClient)),
                SyntaxFactory.ArgumentList(),
                null);

            var variableDeclaratorSyntax = SyntaxFactory.VariableDeclarator(
                SyntaxFactory.Identifier(HttpClientVariableName),
                null,
                SyntaxFactory.EqualsValueClause(objectCreationExpressionSyntax));

            var variableDeclarationSyntax = SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .AddVariables(variableDeclaratorSyntax);

            return SyntaxFactory.UsingStatement(variableDeclarationSyntax, null, SyntaxFactory.Block());
        }

        private UsingStatementSyntax CreateRequestUsing(CurlOptions curlOptions)
        {
            var methodNameArgument = SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(curlOptions.HttpMethod)));
            var methodArgumentList = new SeparatedSyntaxList<ArgumentSyntax>().Add(methodNameArgument);
            var httpMethodArgument = SyntaxFactory.Argument(
                SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.IdentifierName(nameof(HttpMethod)),
                    SyntaxFactory.ArgumentList(methodArgumentList),
                    null));

            var urlArgument = SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(curlOptions.Url.ToString())));

            var separatedSyntaxList = new SeparatedSyntaxList<ArgumentSyntax>().Add(httpMethodArgument);
            separatedSyntaxList = separatedSyntaxList.Add(urlArgument);

            var objectCreationExpressionSyntax = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(nameof(HttpRequestMessage)),
                SyntaxFactory.ArgumentList(separatedSyntaxList),
                null);

            var variableDeclaratorSyntax = SyntaxFactory.VariableDeclarator(
                SyntaxFactory.Identifier(RequestVariableName),
                null,
                SyntaxFactory.EqualsValueClause(objectCreationExpressionSyntax));

            var variableDeclarationSyntax = SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .AddVariables(variableDeclaratorSyntax);

            return SyntaxFactory.UsingStatement(variableDeclarationSyntax, null, SyntaxFactory.Block());
        }

        private LocalDeclarationStatementSyntax CreateSendStatement()
        {
            var sendAsyncAccessSyntax = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(HttpClientVariableName),
                SyntaxFactory.IdentifierName("SendAsync"));

            var separatedSyntaxList = new SeparatedSyntaxList<ArgumentSyntax>();
            separatedSyntaxList = separatedSyntaxList.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(RequestVariableName)));

            var invocationExpressionSyntax = SyntaxFactory.InvocationExpression(
                sendAsyncAccessSyntax,
                SyntaxFactory.ArgumentList(separatedSyntaxList));

            var awaitExpression = SyntaxFactory.AwaitExpression(invocationExpressionSyntax);

            var declarationSyntax = SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .AddVariables(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier("response"),
                        null,
                        SyntaxFactory.EqualsValueClause(awaitExpression)));

            return SyntaxFactory.LocalDeclarationStatement(declarationSyntax);
        }

        private ArgumentSyntax CreateStringLiteralArgument(string argumentName)
        {
            return SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(argumentName)));
        }
    }
}
