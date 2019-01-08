using System;
using System.Linq;

using CurlToCSharp.Models;
using CurlToCSharp.Models.Parsing;

using Xunit;

namespace CurlToCSharp.UnitTests.Models.Parsing
{
    public class HeaderParameterEvaluatorTests
    {
        [Fact]
        public void Evaluate_InvalidHeaderWithLeadingSeparator_WarningAdded()
        {
            var evaluator = new HeaderParameterEvaluator();
            var span = new Span<char>(":User-Agent: curl/7.60.0".ToCharArray());
            var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

            evaluator.Evaluate(ref span, convertResult);

            Assert.Empty(convertResult.Data.Headers);
            Assert.Equal(1, convertResult.Warnings.Count);
        }
    }
}
