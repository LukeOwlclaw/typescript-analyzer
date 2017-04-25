﻿using Newtonsoft.Json.Linq;

namespace WebLinter
{
    internal class CoffeeLinter : LinterBase
    {
        public CoffeeLinter(ISettings settings) : base(settings)
        {
            Name = "CoffeeLint";
            ConfigFileName = "coffeelint.json";
            IsEnabled = Settings.CoffeeLintEnable;
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
                    if (error["name"] == null) // It's a compiler error
                        continue;

                    var le = new LintingError(fileName);
                    le.Message = error["message"]?.Value<string>();
                    le.LineNumber = error["lineNumber"]?.Value<int>() - 1 ?? 0;
                    le.ColumnNumber = 0;
                    le.IsError = error["level"]?.Value<string>() == "error";
                    le.ErrorCode = error["rule"]?.Value<string>();
                    le.HelpLink = $"http://www.coffeelint.org/?rule={le.ErrorCode}#options";
                    le.Provider = this;
                    Result.Errors.Add(le);
                }
            }
        }
    }
}
