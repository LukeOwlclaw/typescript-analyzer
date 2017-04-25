﻿using Newtonsoft.Json.Linq;

namespace WebLinter
{
    internal class CssLinter : LinterBase
    {
        public CssLinter(ISettings settings) : base(settings)
        {
            Name = "CssLint";
            ConfigFileName = ".csslintrc";
            IsEnabled = Settings.CssLintEnable;
        }

        protected override void ParseErrors(string output)
        {
            var root = JObject.Parse(output);

            foreach (JProperty obj in root.Children<JProperty>())
            {
                string fileName = obj.Name;

                if (string.IsNullOrEmpty(fileName))
                    continue;

                foreach (JObject error in obj.Value)
                {
                    if (error["rollup"] != null)
                        continue;

                    var le = new LintingError(fileName);
                    le.Message = error["message"]?.Value<string>();
                    le.LineNumber = error["line"]?.Value<int>() - 1 ?? 0;
                    le.ColumnNumber = error["col"]?.Value<int>() - 1 ?? 0;
                    le.IsError = error["type"]?.Value<string>() == "error";
                    le.ErrorCode = error["rule"]?["id"]?.Value<string>();
                    le.HelpLink = $"https://github.com/CSSLint/csslint/wiki/Rules/#{le.ErrorCode}";
                    le.Provider = this;
                    Result.Errors.Add(le);
                }
            }
        }
    }
}
