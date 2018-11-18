using System.Collections.Generic;

namespace CurlToCSharp.Models.Parsing
{
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
                           new FormParameterEvaluator(),
                           new CertificateTypeEvaluator(),
                           new CertificateEvaluator()
                       };
        }
    }
}
