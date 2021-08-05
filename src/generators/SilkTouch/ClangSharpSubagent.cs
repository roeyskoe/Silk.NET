﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Silk.NET.SilkTouch.Scraper.Subagent;
using Ultz.Extensions.Logging;

namespace SilkTouch
{
    /// <summary>
    /// Spawns ClangSharp in a subprocess. This allows for parallelism (Clang doesn't like threads)
    /// </summary>
    internal class ClangSharpSubagent : ISubagent
    {
        /// <inheritdoc />
        public async Task<int> RunClangSharpAsync(SubagentOptions opts, List<string>? errors = null)
        {
            // get the command line arguments this process was started with
            // using ArraySegment (lesser span) here instead of Span because it's enumerable.
            ArraySegment<string> args = Environment.GetCommandLineArgs();
            
            // trim off the arguments *we* received
            args = args[..^Program.Args.Length];

            // serialize the options and escape the quotes to send on the command line.
            var optsStr = JsonSerializer.Serialize(opts).Replace("\"", "\\\"");
            
            // the remainder is what we use to start the subprocesses.
            using var proc = new Process
            {
                StartInfo = new
                (
                    args[0],
                    $"\"{string.Join("\" \"", args[1..].Select(x => x.Replace("\"", "\\\"")))}\" \"{optsStr}\""
                )
                {
                    WorkingDirectory = Environment.CurrentDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            // run the subprocess.
            if (!proc.Start())
            {
                return (int) ExitCodes.SubagentFailedToStart;
            }

            // log its logs to the log
            while (!proc.StandardOutput.EndOfStream)
            {
                var line = await proc.StandardOutput.ReadLineAsync();
                if (line is null)
                {
                    continue;
                }

                switch (line[..2])
                {
                    case "I:":
                    {
                        Log.Information($"{opts.NamespaceName}: {line[2..]}");
                        break;
                    }
                    case "W:":
                    {
                        Log.Warning($"{opts.NamespaceName}: {line[2..]}");
                        break;
                    }
                    case "T:":
                    {
                        Log.Trace($"{opts.NamespaceName}: {line[2..]}");
                        break;
                    }
                    case "E:":
                    {
                        Log.Error($"{opts.NamespaceName}: {line[2..]}");
                        errors?.Add(line[2..]);
                        break;
                    }
                    default:
                    {
                        Log.Debug($"{opts.NamespaceName}: {line}");
                        break;
                    }
                }
            }
            
            await proc.WaitForExitAsync();
            return proc.ExitCode;
        }
    }
}