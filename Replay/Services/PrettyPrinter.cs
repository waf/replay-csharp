using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Replay.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Replay.Services
{
    class PrettyPrinter
    {
        private CSharpObjectFormatter objectFormatter;

        public PrettyPrinter()
        {
            this.objectFormatter = CSharpObjectFormatter.Instance;
        }

        public LineOutput Format(EvaluationResult evaluationResult)
        {
            return new LineOutput(
                FormatObject(evaluationResult.ScriptResult?.ReturnValue),
                evaluationResult.Exception?.Message,
                evaluationResult.StandardOutput
            );
        }

        private string FormatObject(object obj)
        {
            if(obj == null)
            {
                // right now there's no way to determine the difference between "no value" and "null value"
                // intercept all nulls and return null, instead of the string "null"
                // because otherwise every single assignment expression would print "null"
                return null;
            }
            return objectFormatter.FormatObject(obj);
        }
    }
}
