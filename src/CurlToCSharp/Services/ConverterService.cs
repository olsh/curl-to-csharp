using System;
using System.Collections.Generic;
using System.IO;
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

        private const string HandlerVariableName = "handler";

        private const string RequestContentPropertyName = "Content";

        public ConvertResult<string> ToCsharp(CurlOptions curlOptions)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit();

            var result = new ConvertResult<string>();

            AddWarningsIfAny(curlOptions, result);
            if (ShouldGenerateHandler(curlOptions))
            {
                var configureHandlerStatements = ConfigureHandlerStatements(curlOptions);
                compilationUnit = compilationUnit.AddMembers(configureHandlerStatements.ToArray());
            }

            var httpClientUsing = CreateHttpClientUsing(curlOptions);
            var requestUsingStatements = CreateRequestUsingStatements(curlOptions);

            httpClientUsing = httpClientUsing.WithStatement(SyntaxFactory.Block(requestUsingStatements));
            result.Data = compilationUnit.AddMembers(SyntaxFactory.GlobalStatement(httpClientUsing))
                .NormalizeWhitespace()
                .ToFullString();

            return result;
        }

        private void AddWarningsIfAny(CurlOptions curlOptions, ConvertResult<string> result)
        {
            if (curlOptions.HasProxy && !IsSupportedProxy(curlOptions.ProxyUri))
            {
                result.Warnings.Add($"Proxy scheme \"{curlOptions.ProxyUri.Scheme}\" is not supported");
            }

            if (curlOptions.HasCertificate && !IsSupportedCertificate(curlOptions.CertificateType))
            {
                result.Warnings.Add($"Certificate type \"{curlOptions.CertificateType.ToString()}\" is not supported");
            }
        }

        private bool IsSupportedProxy(Uri proxyUri)
        {
            if (Uri.UriSchemeHttp == proxyUri.Scheme || Uri.UriSchemeHttps == proxyUri.Scheme)
            {
                return true;
            }

            return false;
        }

        private bool IsSupportedCertificate(CertificateType certificateType)
        {
            return certificateType == CertificateType.P12;
        }

        private bool ShouldGenerateHandler(CurlOptions curlOptions)
        {
            return curlOptions.HasCookies
                   || (curlOptions.HasProxy && IsSupportedProxy(curlOptions.ProxyUri))
                   || ((curlOptions.HasCertificate && IsSupportedCertificate(curlOptions.CertificateType))
                   || curlOptions.Insecure);
        }

        /// <summary>
        /// Generates the string content creation statements.
        /// </summary>
        /// <param name="curlOptions">The curl options.</param>
        /// <returns>Collection of <see cref="StatementSyntax"/>.</returns>
        /// <remarks>
        /// request.Content = new StringContent("{\"status\": \"resolved\"}", Encoding.UTF8, "application/json");
        /// </remarks>
        private IEnumerable<StatementSyntax> CreateStringContentAssignmentStatement(CurlOptions curlOptions)
        {
            var expressions = new LinkedList<ExpressionSyntax>();

            foreach (var data in curlOptions.UploadData)
            {
                if (data.IsUrlEncoded)
                {
                    ExpressionSyntax dataExpression;
                    if (data.IsFile)
                    {
                        dataExpression = CreateFileReadAllTextExpression(data.Content);
                    }
                    else
                    {
                        dataExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(data.Content));
                    }

                    dataExpression = RoslynExtensions.CreateInvocationExpression("Uri", "EscapeDataString", SyntaxFactory.Argument(dataExpression));

                    if (data.HasName)
                    {
                        dataExpression =
                            RoslynExtensions.CreateInterpolatedStringExpression($"{data.Name}=", dataExpression);
                    }

                    expressions.AddLast(dataExpression);

                    continue;
                }

                if (data.Type == UploadDataType.BinaryFile)
                {
                    var readFileExpression = CreateFileReadAllTextExpression(data.Content);
                    expressions.AddLast(readFileExpression);

                    continue;
                }

                if (data.Type == UploadDataType.InlineFile)
                {
                    var readFileExpression = CreateFileReadAllTextExpression(data.Content);
                    var replaceNewLines = RoslynExtensions.CreateInvocationExpression(
                        "Regex",
                        "Replace",
                        SyntaxFactory.Argument(readFileExpression),
                        RoslynExtensions.CreateStringLiteralArgument(@"(?:\r\n|\n|\r)"),
                        SyntaxFactory.Argument(RoslynExtensions.CreateMemberAccessExpression("string", "Empty")));
                    expressions.AddLast(replaceNewLines);

                    continue;
                }

                expressions.AddLast(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(data.Content)));
            }

            var statements = new LinkedList<StatementSyntax>();
            ArgumentSyntax stringContentArgumentSyntax;
            if (expressions.Count > 1)
            {
                var contentListVariableName = "contentList";
                statements.AddLast(
                    SyntaxFactory.LocalDeclarationStatement(
                        RoslynExtensions.CreateVariableFromNewObjectExpression(contentListVariableName, "List<string>")));

                foreach (var expression in expressions)
                {
                    statements.AddLast(
                        SyntaxFactory.ExpressionStatement(
                            RoslynExtensions.CreateInvocationExpression(
                                contentListVariableName,
                                "Add",
                                SyntaxFactory.Argument(expression))));
                }

                stringContentArgumentSyntax = SyntaxFactory.Argument(
                    RoslynExtensions.CreateInvocationExpression(
                        "string",
                        "Join",
                        RoslynExtensions.CreateStringLiteralArgument("&"),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(contentListVariableName))));
            }
            else
            {
                stringContentArgumentSyntax = SyntaxFactory.Argument(expressions.First.Value);
            }

            var stringContentCreation = CreateStringContentCreation(
                stringContentArgumentSyntax,
                curlOptions);
            statements.AddLast(
                SyntaxFactory.ExpressionStatement(
                    RoslynExtensions.CreateMemberAssignmentExpression(
                        RequestVariableName,
                        RequestContentPropertyName,
                        stringContentCreation)));

            statements.TryAppendWhiteSpaceAtEnd();

            return statements;
        }

                /// <summary>
        /// Generates the multipart content statements.
        /// </summary>
        /// <param name="curlOptions">The curl options.</param>
        /// <returns>Collection of <see cref="StatementSyntax"/>.</returns>
        /// <remarks>
        /// var multipartContent = new MultipartFormDataContent();
        /// multipartContent.Add(new StringContent("John"), "name");
        /// multipartContent.Add(new ByteArrayContent(File.ReadAllBytes("D:\\text.txt")), "shoesize", Path.GetFileName("D:\\text.txt"));
        /// request.Content = multipartContent;
        /// </remarks>
        private IEnumerable<StatementSyntax> CreateMultipartContentStatements(CurlOptions curlOptions)
        {
            var statements = new LinkedList<StatementSyntax>();

            const string MultipartVariableName = "multipartContent";
            const string MultipartAddMethodName = "Add";

            statements.AddLast(
                SyntaxFactory.LocalDeclarationStatement(
                    RoslynExtensions.CreateVariableFromNewObjectExpression(
                        MultipartVariableName,
                        nameof(MultipartFormDataContent))));

            foreach (var data in curlOptions.FormData)
            {
                StatementSyntax addStatement;
                if (data.Type == UploadDataType.Inline)
                {
                    var contentExpression = RoslynExtensions.CreateObjectCreationExpression(
                        nameof(StringContent),
                        RoslynExtensions.CreateStringLiteralArgument(data.Content));

                    addStatement = SyntaxFactory.ExpressionStatement(
                        RoslynExtensions.CreateInvocationExpression(
                            MultipartVariableName,
                            MultipartAddMethodName,
                            SyntaxFactory.Argument(contentExpression),
                            RoslynExtensions.CreateStringLiteralArgument(data.Name)));
                }
                else if (data.Type == UploadDataType.BinaryFile)
                {
                    var contentExpression = CreateNewByteArrayContentExpression(data.Content);
                    var getFileNameSyntax = RoslynExtensions.CreateInvocationExpression(
                        nameof(Path),
                        nameof(Path.GetFileName),
                        RoslynExtensions.CreateStringLiteralArgument(data.Content));

                    addStatement = SyntaxFactory.ExpressionStatement(
                        RoslynExtensions.CreateInvocationExpression(
                            MultipartVariableName,
                            MultipartAddMethodName,
                            SyntaxFactory.Argument(contentExpression),
                            RoslynExtensions.CreateStringLiteralArgument(data.Name),
                            SyntaxFactory.Argument(getFileNameSyntax)));
                }
                else
                {
                    var contentExpression = RoslynExtensions.CreateObjectCreationExpression(
                        nameof(StringContent),
                        SyntaxFactory.Argument(CreateFileReadAllTextExpression(data.Content)));

                    addStatement = SyntaxFactory.ExpressionStatement(
                        RoslynExtensions.CreateInvocationExpression(
                            MultipartVariableName,
                            MultipartAddMethodName,
                            SyntaxFactory.Argument(contentExpression),
                            RoslynExtensions.CreateStringLiteralArgument(data.Name)));
                }

                statements.AddLast(addStatement);
            }

            statements.AddLast(SyntaxFactory.ExpressionStatement(
                RoslynExtensions.CreateMemberAssignmentExpression(
                    RequestVariableName,
                    RequestContentPropertyName,
                    SyntaxFactory.IdentifierName(MultipartVariableName))));

            statements.TryAppendWhiteSpaceAtEnd();

            return statements;
        }

        /// <summary>
        /// Generates the file read all text expression.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>
        ///   <see cref="InvocationExpressionSyntax" /> expression.
        /// </returns>
        /// <remarks>
        /// File.ReadAllText("file name.txt")
        /// </remarks>
        private InvocationExpressionSyntax CreateFileReadAllTextExpression(string fileName)
        {
            return RoslynExtensions.CreateInvocationExpression(
                "File",
                "ReadAllText",
                RoslynExtensions.CreateStringLiteralArgument(fileName));
        }

        /// <summary>
        /// Generates the new byte array content expression.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns><see cref="ObjectCreationExpressionSyntax"/> expression.</returns>
        /// <remarks>
        /// new ByteArrayContent(File.ReadAllBytes("file1.txt"))
        /// </remarks>
        private ObjectCreationExpressionSyntax CreateNewByteArrayContentExpression(string fileName)
        {
            var fileReadExpression = RoslynExtensions.CreateInvocationExpression(
                "File",
                "ReadAllBytes",
                RoslynExtensions.CreateStringLiteralArgument(fileName));

            var byteArrayContentExpression = RoslynExtensions.CreateObjectCreationExpression(
                "ByteArrayContent",
                SyntaxFactory.Argument(fileReadExpression));
            return byteArrayContentExpression;
        }

        /// <summary>
        /// Generates the string content creation expression.
        /// </summary>
        /// <param name="contentSyntax">The content syntax.</param>
        /// <param name="curlOptions">The curl options.</param>
        /// <returns>
        ///   <see cref="ObjectCreationExpressionSyntax" /> expression.
        /// </returns>
        /// <remarks>
        /// new StringContent("{\"status\": \"resolved\"}", Encoding.UTF8, "application/json")
        /// </remarks>
        private ObjectCreationExpressionSyntax CreateStringContentCreation(ArgumentSyntax contentSyntax, CurlOptions curlOptions)
        {
            var arguments = new LinkedList<ArgumentSyntax>();
            arguments.AddLast(contentSyntax);

            var contentHeader = curlOptions.Headers.GetCommaSeparatedValues(HeaderNames.ContentType).FirstOrDefault();
            if (!string.IsNullOrEmpty(contentHeader))
            {
                var contentTypeValues = contentHeader.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                arguments.AddLast(SyntaxFactory.Argument(RoslynExtensions.CreateMemberAccessExpression("Encoding", "UTF8")));
                arguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(contentTypeValues[0].Trim()));
            }

            var stringContentCreation = RoslynExtensions.CreateObjectCreationExpression("StringContent", arguments.ToArray());
            return stringContentCreation;
        }

        /// <summary>
        /// Generates the header assignment statements.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Collection of <see cref="StatementSyntax"/></returns>
        /// <remarks>
        /// request.Headers.TryAddWithoutValidation("Accept", "application/json");
        /// request.Headers.TryAddWithoutValidation("User-Agent", "curl/7.60.0");
        /// </remarks>
        private IEnumerable<StatementSyntax> CreateHeaderAssignmentStatements(CurlOptions options)
        {
            if (!options.Headers.Any() && !options.HasCookies)
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

            if (options.HasCookies)
            {
                statements.AddLast(
                    CreateTryAddHeaderStatement(
                        RoslynExtensions.CreateStringLiteralArgument("Cookie"),
                        RoslynExtensions.CreateStringLiteralArgument(options.CookieValue)));
            }

            statements.TryAppendWhiteSpaceAtEnd();

            return statements;
        }

        /// <summary>
        /// Generates the basic authorization statements.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Collection of <see cref="StatementSyntax"/>.</returns>
        /// <remarks>
        /// var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes("username:password"));
        /// request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
        /// </remarks>
        private IEnumerable<StatementSyntax> CreateBasicAuthorizationStatements(CurlOptions options)
        {
            var authorizationEncodingStatement = CreateBasicAuthorizationEncodingStatement(options);

            var interpolatedStringExpression = RoslynExtensions.CreateInterpolatedStringExpression("Basic ", SyntaxFactory.IdentifierName(Base64AuthorizationVariableName));
            var tryAddHeaderStatement = CreateTryAddHeaderStatement(
                    RoslynExtensions.CreateStringLiteralArgument("Authorization"),
                    SyntaxFactory.Argument(interpolatedStringExpression))
                .AppendWhiteSpace();

            return new StatementSyntax[] { authorizationEncodingStatement, tryAddHeaderStatement };
        }

        /// <summary>
        /// Generates the basic authorization encoding statement.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns><see cref="LocalDeclarationStatementSyntax"/> statement.</returns>
        /// <remarks>
        /// var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes("username:password"));
        /// </remarks>
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

        /// <summary>
        /// Generates the headers adding statement.
        /// </summary>
        /// <param name="keyArgumentSyntax">The header key argument syntax.</param>
        /// <param name="valueArgumentSyntax">The header value argument syntax.</param>
        /// <returns><see cref="ExpressionStatementSyntax"/> statement.</returns>
        /// <remarks>
        /// request.Headers.TryAddWithoutValidation("Accept", "application/json");
        /// </remarks>
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

        /// <summary>
        /// Generate the HttpClient using statements with empty using block.
        /// </summary>
        /// <param name="curlOptions">The curl options.</param>
        /// <returns>Collection of <see cref="UsingStatementSyntax"/>.</returns>
        /// <remarks>
        /// using (var httpClient = new HttpClient())
        /// {
        /// }
        /// </remarks>
        private UsingStatementSyntax CreateHttpClientUsing(CurlOptions curlOptions)
        {
            var argumentSyntax = ShouldGenerateHandler(curlOptions)
                                     ? new[] { SyntaxFactory.Argument(SyntaxFactory.IdentifierName(HandlerVariableName)) }
                                     : new ArgumentSyntax[0];
            return RoslynExtensions.CreateUsingStatement(HttpClientVariableName, nameof(HttpClient), argumentSyntax);
        }

        /// <summary>
        /// Generate the HttpRequestMessage using statements with statements inside the using blocks.
        /// </summary>
        /// <param name="curlOptions">The curl options.</param>
        /// <returns>Collection of <see cref="UsingStatementSyntax"/>.</returns>
        /// <remarks>
        /// using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://github.com/"))
        /// {
        ///     var response = await httpClient.SendAsync(request);
        /// }
        /// </remarks>
        private IEnumerable<UsingStatementSyntax> CreateRequestUsingStatements(CurlOptions curlOptions)
        {
            var innerBlock = SyntaxFactory.Block();

            var methodNameArgument = RoslynExtensions.CreateStringLiteralArgument(curlOptions.HttpMethod);
            var httpMethodArgument = RoslynExtensions.CreateObjectCreationExpression(nameof(HttpMethod), methodNameArgument);

            var urlArgument = RoslynExtensions.CreateStringLiteralArgument(curlOptions.Url.ToString());
            var requestUsingStatement = RoslynExtensions.CreateUsingStatement(
                RequestVariableName,
                nameof(HttpRequestMessage),
                SyntaxFactory.Argument(httpMethodArgument),
                urlArgument);

            var statements = CreateHeaderAssignmentStatements(curlOptions);
            innerBlock = innerBlock.AddStatements(statements.ToArray());

            if (!string.IsNullOrEmpty(curlOptions.UserPasswordPair))
            {
                var basicAuthorizationStatements = CreateBasicAuthorizationStatements(curlOptions);
                innerBlock = innerBlock.AddStatements(basicAuthorizationStatements.ToArray());
            }

            var requestInnerBlocks = new LinkedList<UsingStatementSyntax>();
            if (curlOptions.HasDataPayload)
            {
                var assignmentExpression = CreateStringContentAssignmentStatement(curlOptions);
                requestInnerBlocks.AddLast(
                    requestUsingStatement.WithStatement(innerBlock.AddStatements(assignmentExpression.ToArray())));
            }
            else if (curlOptions.HasFormPayload)
            {
                var multipartContentStatements = CreateMultipartContentStatements(curlOptions);
                requestInnerBlocks.AddLast(
                    requestUsingStatement.WithStatement(innerBlock.AddStatements(multipartContentStatements.ToArray())));
            }
            else if (curlOptions.HasFilePayload)
            {
                foreach (var file in curlOptions.UploadFiles)
                {
                    // NOTE that you must use a trailing / on the last directory to really prove to
                    // Curl that there is no file name or curl will think that your last directory name is the remote file name to use.
                    if (!string.IsNullOrEmpty(curlOptions.Url.PathAndQuery)
                        && curlOptions.Url.PathAndQuery.EndsWith('/'))
                    {
                        var objectCreationExpressionSyntaxs = requestUsingStatement.DescendantNodes()
                            .OfType<ObjectCreationExpressionSyntax>()
                            .First(
                                t => t.Type is IdentifierNameSyntax identifier
                                     && identifier.Identifier.ValueText == nameof(HttpRequestMessage));

                        var s = objectCreationExpressionSyntaxs.ArgumentList.Arguments.Last();

                        requestUsingStatement = requestUsingStatement.ReplaceNode(
                            s,
                            RoslynExtensions.CreateStringLiteralArgument(curlOptions.GetUrlForFileUpload(file).ToString()));
                    }

                    var byteArrayContentExpression = CreateNewByteArrayContentExpression(file);
                    requestInnerBlocks.AddLast(requestUsingStatement.WithStatement(innerBlock.AddStatements(
                        SyntaxFactory.ExpressionStatement(
                                RoslynExtensions.CreateMemberAssignmentExpression(
                                    RequestVariableName,
                                    RequestContentPropertyName,
                                    byteArrayContentExpression))
                            .AppendWhiteSpace())));
                }
            }

            var sendStatement = CreateSendStatement();
            if (!requestInnerBlocks.Any())
            {
                return new List<UsingStatementSyntax> { requestUsingStatement.WithStatement(innerBlock.AddStatements(sendStatement)) };
            }

            return requestInnerBlocks.Select(i => i.WithStatement(((BlockSyntax)i.Statement).AddStatements(sendStatement)));
        }

        /// <summary>
        /// Generate the statements for sending a HttpRequestMessage.
        /// </summary>
        /// <returns><see cref="LocalDeclarationStatementSyntax"/> statement.</returns>
        /// <remarks>
        /// var response = await httpClient.SendAsync(request);
        /// </remarks>
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

        /// <summary>
        /// Generate the statements for HttpClient handler configuration.
        /// </summary>
        /// <param name="curlOptions">The curl options.</param>
        /// <returns>Collection of <see cref="MemberDeclarationSyntax" />.</returns>
        /// <remarks>
        /// var handler = new HttpClientHandler();
        /// handler.UseCookies = false;
        /// </remarks>
        private IEnumerable<MemberDeclarationSyntax> ConfigureHandlerStatements(CurlOptions curlOptions)
        {
            var statementSyntaxs = new LinkedList<MemberDeclarationSyntax>();

            var handlerInitialization = RoslynExtensions.CreateVariableFromNewObjectExpression(
                HandlerVariableName,
                nameof(HttpClientHandler));
            statementSyntaxs.AddLast(
                SyntaxFactory.GlobalStatement(SyntaxFactory.LocalDeclarationStatement(handlerInitialization)));

            if (curlOptions.HasCookies)
            {
                var memberAssignmentExpression = RoslynExtensions.CreateMemberAssignmentExpression(
                    HandlerVariableName,
                    "UseCookies",
                    SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
                statementSyntaxs.AddLast(
                    SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(memberAssignmentExpression)));
            }

            if (curlOptions.HasProxy && IsSupportedProxy(curlOptions.ProxyUri))
            {
                var memberAssignmentExpression = CreateProxyStatements(curlOptions);

                statementSyntaxs.AddLast(
                    SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(memberAssignmentExpression)));
            }

            if (curlOptions.HasCertificate && IsSupportedCertificate(curlOptions.CertificateType))
            {
                var memberAssignmentExpression = RoslynExtensions.CreateMemberAssignmentExpression(
                    HandlerVariableName,
                    "ClientCertificateOptions",
                    RoslynExtensions.CreateMemberAccessExpression("ClientCertificateOption", "Manual"));

                statementSyntaxs.AddLast(
                    SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(memberAssignmentExpression)));

                var newCertificateArguments = new LinkedList<ArgumentSyntax>();
                newCertificateArguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(curlOptions.CertificateFileName));
                if (curlOptions.HasCertificatePassword)
                {
                    newCertificateArguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(curlOptions.CertificatePassword));
                }

                var newCertificateExpression = RoslynExtensions.CreateObjectCreationExpression(
                    "X509Certificate2",
                    newCertificateArguments.ToArray());
                var certificateAssignmentExpression = RoslynExtensions.CreateInvocationExpression(
                    HandlerVariableName,
                    "ClientCertificates",
                    "Add",
                    SyntaxFactory.Argument(newCertificateExpression));
                statementSyntaxs.AddLast(
                    SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(certificateAssignmentExpression)));
            }

            if (curlOptions.Insecure)
            {
                var parameterListSyntax = RoslynExtensions.CreateParameterListSyntax(
                    "requestMessage",
                    "certificate",
                    "chain",
                    "policyErrors");
                var lambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression(
                    parameterListSyntax,
                    SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression, SyntaxFactory.Token(SyntaxKind.TrueKeyword)));

                statementSyntaxs.AddLast(
                    SyntaxFactory.GlobalStatement(
                        SyntaxFactory.ExpressionStatement(
                            RoslynExtensions.CreateMemberAssignmentExpression(
                                HandlerVariableName,
                                "ServerCertificateCustomValidationCallback",
                                lambdaExpression))));
            }

            statementSyntaxs.TryAppendWhiteSpaceAtEnd();

            return statementSyntaxs;
        }

        /// <summary>
        /// Generate the statements for WebProxy object configuration.
        /// </summary>
        /// <param name="curlOptions">The curl options.</param>
        /// <returns>Collection of <see cref="MemberDeclarationSyntax" />.</returns>
        /// <remarks>
        /// handler.Proxy = new WebProxy("http://localhost:1080/") { UseDefaultCredentials = true }
        /// </remarks>
        private AssignmentExpressionSyntax CreateProxyStatements(CurlOptions curlOptions)
        {
            InitializerExpressionSyntax initializer = null;
            if (curlOptions.UseDefaultProxyCredentials)
            {
                var defaultCredentialsAssignment = SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName("UseDefaultCredentials"),
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.TrueLiteralExpression,
                        SyntaxFactory.Token(SyntaxKind.TrueKeyword)));
                var syntaxList = new SeparatedSyntaxList<ExpressionSyntax>();
                syntaxList = syntaxList.Add(defaultCredentialsAssignment);

                initializer = SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, syntaxList);
            }

            if (curlOptions.HasProxyUserName)
            {
                var proxyCredentialsArguments = new LinkedList<ArgumentSyntax>();
                proxyCredentialsArguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(curlOptions.ProxyUserName));
                proxyCredentialsArguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(curlOptions.ProxyPassword));

                var defaultCredentialsAssignment = SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName("Credentials"),
                    RoslynExtensions.CreateObjectCreationExpression("NetworkCredential", proxyCredentialsArguments.ToArray()));
                var syntaxList = new SeparatedSyntaxList<ExpressionSyntax>().Add(defaultCredentialsAssignment);
                initializer = SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, syntaxList);
            }

            var memberAssignmentExpression = RoslynExtensions.CreateMemberAssignmentExpression(
                HandlerVariableName,
                "Proxy",
                RoslynExtensions.CreateObjectCreationExpression(
                    "WebProxy",
                    initializer,
                    RoslynExtensions.CreateStringLiteralArgument(curlOptions.ProxyUri.ToString())));

            return memberAssignmentExpression;
        }
    }
}
