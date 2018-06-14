using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

using CurlToSharp.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Net.Http.Headers;

namespace CurlToSharp.Services
{
    public class ConverterService : IConverterService
    {
        private const string RequestVariableName = "request";

        private const string HttpClientVariableName = "httpClient";

        public string ToCsharp(CurlOptions curlOptions)
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

            var sendStatement = CreateSendStatement();
            innerBlock = innerBlock.AddStatements(sendStatement);

            var httpClientUsing = CreateHttpClientUsing();

            return httpClientUsing.WithStatement(SyntaxFactory.Block(requestUsing.WithStatement(innerBlock)))
                .NormalizeWhitespace()
                .ToFullString();
        }

        private static ExpressionStatementSyntax CreateContentAssignmentExpression(CurlOptions curlOptions)
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

        private static IEnumerable<ExpressionStatementSyntax> CreateHeaderAssignmentStatements(CurlOptions options)
        {
            if (!options.Headers.Any())
            {
                return Enumerable.Empty<ExpressionStatementSyntax>();
            }

            var headerAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(RequestVariableName),
                SyntaxFactory.IdentifierName("Headers"));
            var headerAddExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                headerAccess,
                SyntaxFactory.IdentifierName("TryAddWithoutValidation"));

            var statements = new LinkedList<ExpressionStatementSyntax>();
            foreach (var header in options.Headers)
            {
                if (header.Key == HeaderNames.ContentType)
                {
                    continue;
                }

                var separatedSyntaxList = new SeparatedSyntaxList<ArgumentSyntax>();
                separatedSyntaxList = separatedSyntaxList.Add(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(header.Key))));

                separatedSyntaxList = separatedSyntaxList.Add(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(header.Value))));

                var invocationExpressionSyntax = SyntaxFactory.InvocationExpression(
                    headerAddExpression,
                    SyntaxFactory.ArgumentList(separatedSyntaxList));

                statements.AddLast(SyntaxFactory.ExpressionStatement(invocationExpressionSyntax));
            }

            return statements;
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
            var httpMethodArgument = SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(nameof(HttpMethod)),
                SyntaxFactory.IdentifierName(curlOptions.HttpMethod.ToString())));

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
            var assignmentExpressionSyntax = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(HttpClientVariableName),
                SyntaxFactory.IdentifierName("SendAsync"));

            var separatedSyntaxList = new SeparatedSyntaxList<ArgumentSyntax>();
            separatedSyntaxList = separatedSyntaxList.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(RequestVariableName)));

            var invocationExpressionSyntax = SyntaxFactory.InvocationExpression(
                assignmentExpressionSyntax,
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
    }
}
