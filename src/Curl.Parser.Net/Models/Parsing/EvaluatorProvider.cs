namespace Curl.Parser.Net.Models.Parsing;

public static class EvaluatorProvider
{
    public static List<ParameterEvaluator> All(ParsingOptions parsingOptions)
    {
        return new List<ParameterEvaluator>
        {
            new RequestParameterEvaluator(),
            new HeaderParameterEvaluator(),
            new CookieParameterEvaluator(),
            new DataParameterEvaluator(),
            new DataRawParameterEvaluator(),
            new DataBinaryParameterEvaluator(),
            new DataUrlEncodeParameterEvaluator(),
            new UserParameterEvaluator(),
            new UploadFileParameterEvaluator(parsingOptions),
            new UrlParameterEvaluator(),
            new ProxyParameterEvaluator(),
            new HeadParameterEvaluator(),
            new InsecureParameterEvaluator(),
            new FormParameterEvaluator(),
            new CertificateTypeParameterEvaluator(),
            new CertificateParameterEvaluator(),
            new KeyTypeParameterEvaluator(),
            new KeyParameterEvaluator(),
            new UserAgentParameterEvaluator(),
            new ProxyCredentialsParameterEvaluator(),
            new GetParameterEvaluator(),
            new CompressedParameterEvaluator(),
            new RefererParameterEvaluator(),
            new Http09ParameterEvaluator(),
            new Http10ParameterEvaluator(),
            new Http11ParameterEvaluator(),
            new Http20ParameterEvaluator(),
            new Http30ParameterEvaluator()
        };
    }
}
