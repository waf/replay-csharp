using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;

namespace Replay.Services
{
    public class ScriptEvaluator
    {
        private ScriptState<object> state;

        public async Task<ScriptState<object>> Evaluate(string script)
        {
            if(state == null)
            {
                state = await CSharpScript.RunAsync(script);
                return state;
            }
            state = await state.ContinueWithAsync(script);
            return state;
        }
    }
}
