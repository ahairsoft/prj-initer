using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Shared;

public sealed class CommandResult
{
    public int ExitCode { get; init; }
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;

    public bool IsSuccess => ExitCode == 0;
}

public sealed class SubmoduleInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
}

public sealed class RepoInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string RemoteUrl { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Head { get; set; } = string.Empty;
}

public static class GitHelper
{
    public static CommandResult RunProcess(string fileName, string arguments, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var proc = new Process { StartInfo = psi };
        proc.Start();
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        return new CommandResult
        {
            ExitCode = proc.ExitCode,
            StdOut = stdout.Trim(),
            StdErr = stderr.Trim()
        };
    }

    public static CommandResult RunGit(string arguments, string repoPath)
    {
        return RunProcess("git", arguments, repoPath);
    }

    public static CommandResult RunGh(string arguments, string workingDirectory)
    {
        return RunProcess("gh", arguments, workingDirectory);
    }

    public static List<SubmoduleInfo> ParseGitModules(string rootPath)
    {
        var gitmodulesPath = Path.Combine(rootPath, ".gitmodules");
        var result = new List<SubmoduleInfo>();

        if (!File.Exists(gitmodulesPath))
        {
            return result;
        }

        var lines = File.ReadAllLines(gitmodulesPath);
        SubmoduleInfo? current = null;
        var headerRegex = new Regex("^\\[submodule \\\"(.+)\\\"\\]$", RegexOptions.Compiled);

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var match = headerRegex.Match(line);
            if (match.Success)
            {
                if (current != null)
                {
                    result.Add(current);
                }

                current = new SubmoduleInfo { Name = match.Groups[1].Value };
                continue;
            }

            if (current == null)
            {
                continue;
            }

            if (line.StartsWith("path =", StringComparison.OrdinalIgnoreCase))
            {
                current.Path = line.Substring("path =".Length).Trim();
            }
            else if (line.StartsWith("url =", StringComparison.OrdinalIgnoreCase))
            {
                current.Url = line.Substring("url =".Length).Trim();
            }
            else if (line.StartsWith("branch =", StringComparison.OrdinalIgnoreCase))
            {
                current.Branch = line.Substring("branch =".Length).Trim();
            }
        }

        if (current != null)
        {
            result.Add(current);
        }

        return result;
    }

    public static string GetCurrentBranch(string repoPath)
    {
        var result = RunGit("rev-parse --abbrev-ref HEAD", repoPath);
        return result.IsSuccess ? result.StdOut : "N/A";
    }

    public static string GetHeadShort(string repoPath)
    {
        var result = RunGit("log --oneline -1", repoPath);
        return result.IsSuccess ? result.StdOut : "N/A";
    }

    public static string GetRemoteUrl(string repoPath)
    {
        var result = RunGit("remote get-url origin", repoPath);
        return result.IsSuccess ? result.StdOut : string.Empty;
    }

    public static string GetRemoteDefaultBranch(string url)
    {
        var result = RunProcess("git", $"ls-remote --symref {EscapeArg(url)} HEAD");
        if (!result.IsSuccess)
        {
            return "main";
        }

        var firstLine = result.StdOut.Split('\n').FirstOrDefault() ?? string.Empty;
        var match = Regex.Match(firstLine, @"refs/heads/([^\s]+)");
        return match.Success ? match.Groups[1].Value : "main";
    }

    public static List<string> ListRemoteBranches(string url)
    {
        var result = RunProcess("git", $"ls-remote --heads {EscapeArg(url)}");
        if (!result.IsSuccess)
        {
            return new List<string>();
        }

        var branches = new List<string>();
        foreach (var line in result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = line.IndexOf("refs/heads/", StringComparison.Ordinal);
            if (idx < 0)
            {
                continue;
            }

            var branch = line[(idx + "refs/heads/".Length)..].Trim();
            if (!string.IsNullOrWhiteSpace(branch))
            {
                branches.Add(branch);
            }
        }

        return branches.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
    }

    public static List<string> ListLocalTags(string repoPath)
    {
        var result = RunGit("tag -l", repoPath);
        if (!result.IsSuccess)
        {
            return new List<string>();
        }

        return result.StdOut
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    public static string EscapeArg(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        if (!value.Contains(' ') && !value.Contains('"'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}
