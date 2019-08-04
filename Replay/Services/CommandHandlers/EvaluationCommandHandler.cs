﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Replay.Model;

namespace Replay.Services.CommandHandlers
{
    class EvaluationCommandHandler : ICommandHandler
    {
        private readonly ScriptEvaluator scriptEvaluator;
        private readonly WorkspaceManager workspaceManager;
        private readonly PrettyPrinter prettyPrinter;

        public EvaluationCommandHandler(ScriptEvaluator scriptEvaluator, WorkspaceManager workspaceManager, PrettyPrinter prettyPrinter)
        {
            this.scriptEvaluator = scriptEvaluator;
            this.workspaceManager = workspaceManager;
            this.prettyPrinter = prettyPrinter;
        }

        public bool CanHandle(string input) => true;

        public async Task<LineEvaluationResult> HandleAsync(int lineId, string text, IReplLogger logger)
        {
            // bail out if it's not a complete statement, but first try automatic completions
            var (success, newTree) = await scriptEvaluator.TryCompleteStatementAsync(text);
            if(!success)
            {
                return LineEvaluationResult.IncompleteInput;
            }
            text = (await newTree.GetRootAsync())
                .NormalizeWhitespace()
                .ToFullString();

            // track the submission in our workspace. We won't need the
            // result for script evaluation, but other roslyn APIs like
            // code completion and syntax highlighting will need it.
            var submission = await workspaceManager.CreateOrUpdateSubmissionAsync(lineId, text);
            var scriptResult = await scriptEvaluator.EvaluateAsync(text);
            var output = await prettyPrinter.FormatAsync(submission.Document, scriptResult);

            return output;
        }
    }
}