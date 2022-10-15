using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

using Curl.CommandLine.Parser;
using Curl.CommandLine.Parser.Constants;
using Curl.CommandLine.Parser.Enums;
using Curl.HttpClient.Converter.Extensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Curl.HttpClient.Converter
{
    public class CurlHttpClientConverter : ICurlConverter
    {
        private const string RequestVariableName = "request";

        private const string HttpClientVariableName = "httpClient";

        private const string Base64AuthorizationVariableName = "base64authorization";

        private const string HandlerVariableName = "handler";

        private const string X509Certificate2ClassName = "X509Certificate2";

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

            if (curlOptions.HasCertificate)
            {
                if (!IsSupportedCertificate(curlOptions.CertificateType))
                {
                    result.Warnings.Add($"Certificate type \"{curlOptions.CertificateType.ToString()}\" is not supported");
                }

                if (curlOptions.CertificateType == CertificateType.P12 && curlOptions.HasKey)
                {
                    result.Warnings.Add("Key parameter is not supported when using a P12 certificate. The key parameter will be ignored");
                }
            }

            if (curlOptions.HasKey && !curlOptions.HasCertificate)
            {
                result.Warnings.Add("Key parameter cannot be used without a certificate. The key parameter will be ignored");
            }

            if (curlOptions.HasKey && !IsSupportedKey(curlOptions.KeyType))
            {
                result.Warnings.Add($"Key type \"{curlOptions.KeyType.ToString()}\" is not supported");
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
            return certificateType == CertificateType.P12 || certificateType == CertificateType.Pem;
        }

        private bool IsSupportedKey(KeyType keyType)
        {
            return keyType is KeyType.Pem;
        }

        private bool ShouldGenerateHandler(CurlOptions curlOptions)
        {
            return curlOptions.HasCookies
                   || (curlOptions.HasProxy && IsSupportedProxy(curlOptions.ProxyUri))
                   || ((curlOptions.HasCertificate && IsSupportedCertificate(curlOptions.CertificateType))
                       || curlOptions.Insecure)
                   || curlOptions.IsCompressed;
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
                // ReSharper disable once PossibleNullReferenceException
                stringContentArgumentSyntax = SyntaxFactory.Argument(expressions.First.Value);
            }

            var stringContentCreation = CreateStringContentCreation(
                stringContentArgumentSyntax);
            statements.AddLast(
                SyntaxFactory.ExpressionStatement(
                    RoslynExtensions.CreateMemberAssignmentExpression(
                        RequestVariableName,
                        RequestContentPropertyName,
                        stringContentCreation)));

            var memberAccessExpressionSyntax = RoslynExtensions.CreateMemberAccessExpression(
                RoslynExtensions.CreateMemberAccessExpression(
                    RoslynExtensions.CreateMemberAccessExpression(RequestVariableName, RequestContentPropertyName),
                    "Headers"),
                "ContentType");
            statements.AddLast(
                SyntaxFactory.ExpressionStatement(
                    RoslynExtensions.CreateMemberAssignmentExpression(
                        memberAccessExpressionSyntax,
                        RoslynExtensions.CreateInvocationExpression("MediaTypeHeaderValue", "Parse", RoslynExtensions.CreateStringLiteralArgument(curlOptions.GetHeader(HeaderNames.ContentType))))));

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

            const string multipartVariableName = "multipartContent";
            const string multipartAddMethodName = "Add";

            statements.AddLast(
                SyntaxFactory.LocalDeclarationStatement(
                    RoslynExtensions.CreateVariableFromNewObjectExpression(
                        multipartVariableName,
                        nameof(MultipartFormDataContent))));

            int fileCounter = 1;
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
                            multipartVariableName,
                            multipartAddMethodName,
                            SyntaxFactory.Argument(contentExpression),
                            RoslynExtensions.CreateStringLiteralArgument(data.Name)));
                }
                else if (data.Type == UploadDataType.BinaryFile)
                {
                    var getFileNameArgument = string.IsNullOrEmpty(data.FileName)
                        ? SyntaxFactory.Argument(
                            RoslynExtensions.CreateInvocationExpression(
                                nameof(Path),
                                nameof(Path.GetFileName),
                                RoslynExtensions.CreateStringLiteralArgument(data.Content)))
                        : RoslynExtensions.CreateStringLiteralArgument(data.FileName);

                    // If the file has content type, we should add it to ByteArrayContent headers
                    var contentExpression = CreateNewByteArrayContentExpression(data.Content);
                    ExpressionSyntax contentArgumentExpression;
                    if (string.IsNullOrEmpty(data.ContentType))
                    {
                        contentArgumentExpression = contentExpression;
                    }
                    else
                    {
                        var byteArrayVariableName = "file" + fileCounter;
                        var byteArrayContentInitialization = RoslynExtensions.CreateVariableInitializationExpression(byteArrayVariableName, contentExpression);
                        statements.AddLast(SyntaxFactory.LocalDeclarationStatement(byteArrayContentInitialization));
                        statements.AddLast(
                            SyntaxFactory.ExpressionStatement(
                                RoslynExtensions.CreateInvocationExpression(
                                    byteArrayVariableName,
                                    "Headers",
                                    "Add",
                                    RoslynExtensions.CreateStringLiteralArgument("Content-Type"),
                                    RoslynExtensions.CreateStringLiteralArgument(data.ContentType))));
                        contentArgumentExpression = SyntaxFactory.IdentifierName(byteArrayVariableName);
                    }

                    addStatement = SyntaxFactory.ExpressionStatement(
                        RoslynExtensions.CreateInvocationExpression(
                            multipartVariableName,
                            multipartAddMethodName,
                            SyntaxFactory.Argument(contentArgumentExpression),
                            RoslynExtensions.CreateStringLiteralArgument(data.Name),
                            getFileNameArgument));
                }
                else
                {
                    var contentExpression = RoslynExtensions.CreateObjectCreationExpression(
                        nameof(StringContent),
                        SyntaxFactory.Argument(CreateFileReadAllTextExpression(data.Content)));

                    addStatement = SyntaxFactory.ExpressionStatement(
                        RoslynExtensions.CreateInvocationExpression(
                            multipartVariableName,
                            multipartAddMethodName,
                            SyntaxFactory.Argument(contentExpression),
                            RoslynExtensions.CreateStringLiteralArgument(data.Name)));
                }

                statements.AddLast(addStatement);
            }

            statements.AddLast(SyntaxFactory.ExpressionStatement(
                RoslynExtensions.CreateMemberAssignmentExpression(
                    RequestVariableName,
                    RequestContentPropertyName,
                    SyntaxFactory.IdentifierName(multipartVariableName))));

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
        /// <returns>
        ///   <see cref="ObjectCreationExpressionSyntax" /> expression.
        /// </returns>
        /// <remarks>
        /// new StringContent("{\"status\": \"resolved\"}", Encoding.UTF8, "application/json")
        /// </remarks>
        private ObjectCreationExpressionSyntax CreateStringContentCreation(ArgumentSyntax contentSyntax)
        {
            var arguments = new LinkedList<ArgumentSyntax>();
            arguments.AddLast(contentSyntax);

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
            if (!options.HasHeaders && !options.HasCookies)
            {
                return Enumerable.Empty<ExpressionStatementSyntax>();
            }

            var statements = new LinkedList<ExpressionStatementSyntax>();
            foreach (var header in options.Headers)
            {
                if (string.Equals(header.Key, HeaderNames.ContentType, StringComparison.InvariantCultureIgnoreCase))
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
        private StatementSyntax[] CreateBasicAuthorizationStatements(CurlOptions options)
        {
            var authorizationEncodingStatement = CreateBasicAuthorizationEncodingStatement(options);

            var interpolatedStringExpression = RoslynExtensions.CreateInterpolatedStringExpression("Basic ", SyntaxFactory.IdentifierName(Base64AuthorizationVariableName));
            var tryAddHeaderStatement = CreateTryAddHeaderStatement(
                    RoslynExtensions.CreateStringLiteralArgument("Authorization"),
                    SyntaxFactory.Argument(interpolatedStringExpression))
                .AppendWhiteSpace();

            return new StatementSyntax[] { authorizationEncodingStatement, tryAddHeaderStatement };
        }

        private ExpressionStatementSyntax CreateSetHttpVersionStatement(CurlOptions options)
        {
            var arguments = new LinkedList<ArgumentSyntax>();
            var majorVersionArgument = options.HttpVersion switch
            {
                HttpVersion.Http09 => RoslynExtensions.CreateIntLiteralArgument(0),
                HttpVersion.Http10 => RoslynExtensions.CreateIntLiteralArgument(1),
                HttpVersion.Http11 => RoslynExtensions.CreateIntLiteralArgument(1),
                HttpVersion.Http20 => RoslynExtensions.CreateIntLiteralArgument(2),
                HttpVersion.Http30 => RoslynExtensions.CreateIntLiteralArgument(3),
                _ => throw new ArgumentOutOfRangeException()
            };
            arguments.AddLast(majorVersionArgument);

            var minorVersionArgument = options.HttpVersion switch
            {
                HttpVersion.Http09 => RoslynExtensions.CreateIntLiteralArgument(9),
                HttpVersion.Http11 => RoslynExtensions.CreateIntLiteralArgument(1),
                _ => RoslynExtensions.CreateIntLiteralArgument(0)
            };
            arguments.AddLast(minorVersionArgument);

            var versionObjectCreationExpression =
                RoslynExtensions.CreateObjectCreationExpression("Version", arguments.ToArray());

            return SyntaxFactory.ExpressionStatement(
                RoslynExtensions.CreateMemberAssignmentExpression(
                    RequestVariableName,
                    "Version",
                    versionObjectCreationExpression));
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
            var usingStatement = RoslynExtensions.CreateUsingStatement(HttpClientVariableName, nameof(System.Net.Http.HttpClient), argumentSyntax);

            return usingStatement
                .PrependComment("// In production code, don't destroy the HttpClient through using, but better use IHttpClientFactory factory or at least reuse an existing HttpClient instance"
                                + Chars.NewLineString + "// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests"
                                + Chars.NewLineString + "// https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/");
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

            var urlArgument = RoslynExtensions.CreateStringLiteralArgument(curlOptions.GetFullUrl());
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
                innerBlock = innerBlock.AddStatements(basicAuthorizationStatements);
            }

            var requestInnerBlocks = new LinkedList<UsingStatementSyntax>();
            if (curlOptions.HasDataPayload && !curlOptions.ForceGet)
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
                            .First(t => t.Type is IdentifierNameSyntax { Identifier: { ValueText: nameof(HttpRequestMessage) } });

                        requestUsingStatement = requestUsingStatement.ReplaceNode(
                            // ReSharper disable once PossibleNullReferenceException
                            objectCreationExpressionSyntaxs.ArgumentList.Arguments.Last(),
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
            else if (curlOptions.HttpVersionSpecified)
            {
                var httpVersionStatement = CreateSetHttpVersionStatement(curlOptions);
                innerBlock = innerBlock.AddStatements(httpVersionStatement);
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

            if (curlOptions.IsCompressed)
            {
                var memberAssignmentExpression = RoslynExtensions.CreateMemberAssignmentExpression(
                    HandlerVariableName,
                    "AutomaticDecompression",
                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.BitwiseNotExpression, RoslynExtensions.CreateMemberAccessExpression("DecompressionMethods", "None")));

                memberAssignmentExpression = memberAssignmentExpression
                    .PrependComment(Chars.NewLineString + "// If you are using .NET Core 3.0+ you can replace `~DecompressionMethods.None` to `DecompressionMethods.All`");

                statementSyntaxs.AddLast(SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(memberAssignmentExpression)));
            }

            if (curlOptions.HasCertificate && IsSupportedCertificate(curlOptions.CertificateType))
            {
                foreach (var declarationSyntax in CreateCertificateStatement(curlOptions))
                {
                    statementSyntaxs.AddLast(declarationSyntax);
                }
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

        private static IEnumerable<MemberDeclarationSyntax> CreateCertificateStatement(CurlOptions curlOptions)
        {
            var statementSyntaxs = new LinkedList<MemberDeclarationSyntax>();

            var memberAssignmentExpression = RoslynExtensions.CreateMemberAssignmentExpression(
                HandlerVariableName,
                "ClientCertificateOptions",
                RoslynExtensions.CreateMemberAccessExpression("ClientCertificateOption", "Manual"));

            statementSyntaxs.AddLast(
                SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(memberAssignmentExpression)));

            var certificateCreationStatement = curlOptions.CertificateType switch
            {
                CertificateType.P12 => CreateP12CertificateStatements(curlOptions),
                CertificateType.Pem => CreatePemCertificateStatements(curlOptions),
                _ => throw new ArgumentOutOfRangeException(nameof(curlOptions.CertificateType), $"Unsupported certificate type {curlOptions.CertificateType}")
            };

            var certificateAssignmentExpression = RoslynExtensions.CreateInvocationExpression(
                HandlerVariableName,
                "ClientCertificates",
                "Add",
                SyntaxFactory.Argument(certificateCreationStatement));

            if (curlOptions.CertificateType == CertificateType.Pem)
            {
                certificateAssignmentExpression = certificateAssignmentExpression
                    .PrependComment(
                        Chars.NewLineString + "// PEM certificates support requires .NET 5 and higher" +
                        Chars.NewLineString + "// Export to PFX is needed because of this bug https://github.com/dotnet/runtime/issues/23749#issuecomment-747407051"
                    );
            }

            statementSyntaxs.AddLast(SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(certificateAssignmentExpression)));

            return statementSyntaxs;
        }

        /// <summary>
        /// Generate the statements for p12 certificate configuration.
        /// </summary>
        /// <param name="curlOptions">The curl options.</param>
        /// <returns>Collection of <see cref="MemberDeclarationSyntax" />.</returns>
        /// <remarks>>
        /// new X509Certificate2("certificate.crt", "password")
        /// </remarks>
        private static ExpressionSyntax CreateP12CertificateStatements(CurlOptions curlOptions)
        {
            var newCertificateArguments = new LinkedList<ArgumentSyntax>();
            newCertificateArguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(curlOptions.CertificateFileName));
            if (curlOptions.HasCertificatePassword)
            {
                newCertificateArguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(curlOptions.CertificatePassword));
            }

            var newCertificateExpression = RoslynExtensions.CreateObjectCreationExpression(
                X509Certificate2ClassName,
                newCertificateArguments.ToArray());

            return newCertificateExpression;
        }

        /// <summary>
        /// Generate the statements for pem certificate configuration.
        /// </summary>
        /// <param name="curlOptions">The curl options.</param>
        /// <returns>Collection of <see cref="MemberDeclarationSyntax" />.</returns>
        /// <remarks>>
        /// X509Certificate2.CreateFromPemFile("cert.pem", "private.pem");
        /// </remarks>
        private static ExpressionSyntax CreatePemCertificateStatements(CurlOptions curlOptions)
        {
            var newCertificateArguments = new LinkedList<ArgumentSyntax>();
            newCertificateArguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(curlOptions.CertificateFileName));

            string methodName;
            if (curlOptions.HasCertificatePassword)
            {
                newCertificateArguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(curlOptions.CertificatePassword));
                methodName = "CreateFromEncryptedPemFile";
            }
            else
            {
                methodName = "CreateFromPemFile";
            }

            if (curlOptions.HasKey)
            {
                newCertificateArguments.AddLast(RoslynExtensions.CreateStringLiteralArgument(curlOptions.KeyFileName));
            }

            var createFromPemInvocationExpression = RoslynExtensions.CreateInvocationExpression(X509Certificate2ClassName, methodName, newCertificateArguments.ToArray());
            var pfxTypeExpression = RoslynExtensions.CreateMemberAccessExpression("X509ContentType", "Pfx");
            createFromPemInvocationExpression = RoslynExtensions.CreateInvocationExpression(createFromPemInvocationExpression, "Export", SyntaxFactory.Argument(pfxTypeExpression));

            return RoslynExtensions.CreateObjectCreationExpression(X509Certificate2ClassName, SyntaxFactory.Argument(createFromPemInvocationExpression));
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
