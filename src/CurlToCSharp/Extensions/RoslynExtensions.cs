using System.Collections.Generic;
using System.Linq;

using CurlToCSharp.Constants;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CurlToCSharp.Extensions
{
    public static class RoslynExtensions
    {
        public static InvocationExpressionSyntax CreateInvocationExpression(
            string leftPart,
            string rightPart,
            params ArgumentSyntax[] argument)
        {
            return CreateInvocationExpression(SyntaxFactory.IdentifierName(leftPart), rightPart, argument);
        }

        public static InvocationExpressionSyntax CreateInvocationExpression(
            string firstPart,
            string secondPart,
            string thirdPart,
            params ArgumentSyntax[] arguments)
        {
            var memberAccessExpression = CreateMemberAccessExpression(firstPart, secondPart);

            return CreateInvocationExpression(memberAccessExpression, thirdPart, arguments);
        }

        public static MemberAccessExpressionSyntax CreateMemberAccessExpression(string leftPart, string rightPart)
        {
            return CreateMemberAccessExpression(SyntaxFactory.IdentifierName(leftPart), rightPart);
        }

        public static InvocationExpressionSyntax CreateInvocationExpression(
            ExpressionSyntax leftPart,
            string rightPart,
            params ArgumentSyntax[] argument)
        {
            var expression = CreateMemberAccessExpression(leftPart, rightPart);
            var separatedSyntaxList = new SeparatedSyntaxList<ArgumentSyntax>();
            separatedSyntaxList = separatedSyntaxList.AddRange(argument);

            return SyntaxFactory.InvocationExpression(expression, SyntaxFactory.ArgumentList(separatedSyntaxList));
        }

        public static ObjectCreationExpressionSyntax CreateObjectCreationExpression(
            string objectName,
            params ArgumentSyntax[] arguments)
        {
            return CreateObjectCreationExpression(objectName, null, arguments);
        }

        public static ObjectCreationExpressionSyntax CreateObjectCreationExpression(
            string objectName,
            InitializerExpressionSyntax initializerExpression,
            params ArgumentSyntax[] arguments)
        {
            var methodArgumentList = new SeparatedSyntaxList<ArgumentSyntax>().AddRange(arguments);
            return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(objectName),
                SyntaxFactory.ArgumentList(methodArgumentList),
                initializerExpression);
        }

        public static ArgumentSyntax CreateStringLiteralArgument(string argumentName)
        {
            return SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(argumentName)));
        }

        public static ParameterListSyntax CreateParameterListSyntax(params string[] parameters)
        {
            var separatedSyntaxList = new SeparatedSyntaxList<ParameterSyntax>().AddRange(
                parameters.Select(p => SyntaxFactory.Parameter(SyntaxFactory.Identifier(p))));

            return SyntaxFactory.ParameterList(separatedSyntaxList);
        }

        public static VariableDeclarationSyntax CreateVariableInitializationExpression(
            string variableName,
            ExpressionSyntax expression)
        {
            return SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .AddVariables(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(variableName),
                        null,
                        SyntaxFactory.EqualsValueClause(expression)));
        }

        public static UsingStatementSyntax CreateUsingStatement(string variableName, string disposableName, params ArgumentSyntax[] arguments)
        {
            var variableDeclaration = CreateVariableFromNewObjectExpression(variableName, disposableName, arguments);

            return SyntaxFactory.UsingStatement(variableDeclaration, null, SyntaxFactory.Block());
        }

        public static VariableDeclarationSyntax CreateVariableFromNewObjectExpression(string variableName, string newObjectName, params ArgumentSyntax[] arguments)
        {
            var objectCreationExpression = CreateObjectCreationExpression(newObjectName, arguments);
            return CreateVariableInitializationExpression(variableName, objectCreationExpression);
        }

        public static AssignmentExpressionSyntax CreateMemberAssignmentExpression(
            string leftPart,
            string rightPart,
            ExpressionSyntax expression)
        {
            var contentAccessExpression = CreateMemberAccessExpression(leftPart, rightPart);

            return SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                contentAccessExpression,
                expression);
        }

        public static AssignmentExpressionSyntax CreateMemberAssignmentExpression(
            MemberAccessExpressionSyntax memberAccessExpressionSyntax,
            ExpressionSyntax expression)
        {
            return SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                memberAccessExpressionSyntax,
                expression);
        }

        public static TSyntax PrependComment<TSyntax>(this TSyntax node, string comment) where TSyntax : SyntaxNode
        {
            return node.WithLeadingTrivia(SyntaxFactory.Comment(comment));
        }

        public static TSyntax AppendWhiteSpace<TSyntax>(this TSyntax node) where TSyntax : SyntaxNode
        {
            return node.WithTrailingTrivia(SyntaxFactory.Comment(Chars.NewLineString));
        }

        public static TSyntax PrependWhiteSpace<TSyntax>(this TSyntax node) where TSyntax : SyntaxNode
        {
            return node.WithLeadingTrivia(SyntaxFactory.Comment(Chars.NewLineString));
        }

        public static void TryAppendWhiteSpaceAtEnd<TSyntax>(this ICollection<TSyntax> statements) where TSyntax : SyntaxNode
        {
            if (statements.Count == 0)
            {
                return;
            }

            var syntax = statements.Last();
            statements.Remove(syntax);
            statements.Add(syntax.AppendWhiteSpace());
        }

        public static InterpolatedStringExpressionSyntax CreateInterpolatedStringExpression(
            string prependString,
            ExpressionSyntax expression)
        {
            var stringStartToken = SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken);

            var interpolatedStringContentSyntaxs = new SyntaxList<InterpolatedStringContentSyntax>()
                .Add(SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken, prependString, null, SyntaxTriviaList.Empty)))
                .Add(SyntaxFactory.Interpolation(expression));

            return SyntaxFactory.InterpolatedStringExpression(stringStartToken, interpolatedStringContentSyntaxs);
        }

        public static MemberAccessExpressionSyntax CreateMemberAccessExpression(
            ExpressionSyntax leftPart,
            string rightPart)
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                leftPart,
                SyntaxFactory.IdentifierName(rightPart));
        }
    }
}
