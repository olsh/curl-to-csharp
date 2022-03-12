using System;
using System.Collections.Generic;

using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing;

public class KeyParameterEvaluator : ParameterEvaluator
{
    public KeyParameterEvaluator()
    {
        Keys = new HashSet<string> { "--key" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        var value = commandLine.ReadValue();
        if (value.IsEmpty)
        {
            convertResult.Warnings.Add("Unable to parse key");

            return;
        }

        convertResult.Data.KeyFileName = value.ToString();
    }
}
