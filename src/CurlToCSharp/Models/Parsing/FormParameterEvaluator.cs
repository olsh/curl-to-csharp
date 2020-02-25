using System;
using System.Collections.Generic;

using CurlToCSharp.Constants;
using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing
{
    public class FormParameterEvaluator : ParameterEvaluator
    {
        public FormParameterEvaluator()
        {
            Keys = new HashSet<string> { "-F", "--form" };
        }

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            var input = commandLine.ReadValue();
            if (!input.TrySplit(FormSeparatorChar, out Span<char> key, out Span<char> value))
            {
                convertResult.Warnings.Add($"Unable to parse form value \"{input.ToString()}\"");

                return;
            }

            if (value.Length > 0)
            {
                var firstPropertySeparatorIndex = value.IndexOf(';');
                Dictionary<string, string> additionalProperties;
                if (firstPropertySeparatorIndex > 0)
                {
                    additionalProperties = GetAdditionalProperties(convertResult, value);
                    value = value.Slice(0, firstPropertySeparatorIndex);
                }
                else
                {
                    additionalProperties = new Dictionary<string, string>();
                }

                var firstCharOfValue = value[0];
                if (firstCharOfValue == '<')
                {
                    convertResult.Data.FormData.Add(CreateFormData(key, value, UploadDataType.InlineFile, additionalProperties));

                    return;
                }

                if (firstCharOfValue == '@')
                {
                    convertResult.Data.FormData.Add(CreateFormData(key, value, UploadDataType.BinaryFile, additionalProperties));

                    return;
                }
            }

            convertResult.Data.FormData.Add(new FormData(key.ToString(), value.ToString(), UploadDataType.Inline));
        }

        private static Dictionary<string, string> GetAdditionalProperties(ConvertResult<CurlOptions> convertResult, Span<char> value)
        {
            var stringValue = value.ToString();
            var valueProperties = stringValue.Split(';');

            Dictionary<string, string> additionalProperties = new Dictionary<string, string>();
            for (int i = 1; i < valueProperties.Length; i++)
            {
                var valueProperty = valueProperties[i];
                var keyValue = valueProperty.Split("=");
                if (keyValue.Length != 2)
                {
                    convertResult.Warnings.Add($"Unable to parse part of form value \"{valueProperty}\"");

                    continue;
                }

                additionalProperties.TryAdd(keyValue[0].ToLower(), TrimValue(keyValue[1]));
            }

            return additionalProperties;
        }

        private static string TrimValue(string value)
        {
            return value.Trim(Chars.Space, Chars.DoubleQuote, Chars.SingleQuote);
        }

        private static FormData CreateFormData(Span<char> key, Span<char> value, UploadDataType type, Dictionary<string, string> additionalProperties)
        {
            return new FormData(
                key.ToString(),
                TrimValue(value.Slice(1).ToString()),
                type,
                additionalProperties.GetValueOrDefault("type"),
                additionalProperties.GetValueOrDefault("filename"));
        }
    }
}
