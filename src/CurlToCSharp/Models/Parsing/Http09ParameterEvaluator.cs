using System;
using System.Collections.Generic;

namespace CurlToCSharp.Models.Parsing;

public class Http09ParameterEvaluator : ParameterEvaluator
{
    public Http09ParameterEvaluator()
    {
        Keys = new HashSet<string> { "--http0.9" };
    }

    protected override bool CanBeEmpty => true;

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        convertResult.Data.HttpVersion = HttpVersion.Http09;
    }
}
