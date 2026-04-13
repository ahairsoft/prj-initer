using Shared;
using System.ComponentModel;
using System.Text;

namespace TagManager;

public sealed class MainForm : Form
{
    private readonly TextBox _txtRoot = new();
    private readonly TextBox _txtTag = new();
    private readonly CheckBox _chkDryRun = new();
    private readonly CheckBox _chkSafeRestore = new();
    private readonly TextBox _txtRestoreBranchPrefix = new();
    private readonly DataGridView _grid = new();
    private readonly RichTextBox _log = new();

    private readonly BindingList<RepoRow> _rows = new();

    private Button _btnScan = null!;
    private Button _btnTag = null!;
    private Button _btnRestore = null!;
    private Button _btnUpdateTag = null!;
    private Button _btnExportLog = null!;
    private Button _btnClearLog = null!;

    public MainForm()
    {
        Text = "prj-initer / Tag Manager";
        Width = 1300;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        _txtRoot.Text = @"I:\project\aisoft\driver_man";
        ScanRepositories();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(8)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        Controls.Add(root);

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        top.Controls.Add(new Label { Text = "Workspace Root", AutoSize = true, Padding = new Padding(0, 8, 4, 0) });

        _txtRoot.Width = 640;
        top.Controls.Add(_txtRoot);

        _btnScan = new Button { Text = "Scan", Width = 100, Height = 30 };
        _btnScan.Click += (_, _) => ScanRepositories();
        top.Controls.Add(_btnScan);

        root.Controls.Add(top, 0, 0);

        var op = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        op.Controls.Add(new Label { Text = "Tag Content", AutoSize = true, Padding = new Padding(0, 8, 4, 0) });
        _txtTag.Width = 300;
        _txtTag.Text = "DM-v1.0.0.0";
        op.Controls.Add(_txtTag);

        _btnTag = new Button { Text = "Create Tags", Width = 120, Height = 30 };
        _btnRestore = new Button { Text = "Restore To Tag", Width = 130, Height = 30 };
        _btnUpdateTag = new Button { Text = "Update Tags", Width = 120, Height = 30 };
        _btnExportLog = new Button { Text = "Export Log", Width = 110, Height = 30 };
        _btnClearLog = new Button { Text = "Clear Log", Width = 90, Height = 30 };

        _btnTag.Click += async (_, _) => await CreateTagsAsync();
        _btnRestore.Click += async (_, _) => await RestoreToTagAsync();
        _btnUpdateTag.Click += async (_, _) => await UpdateTagsAsync();
        _btnExportLog.Click += (_, _) => ExportLog();
        _btnClearLog.Click += (_, _) => _log.Clear();

        _chkSafeRestore.Text = "Safe Restore";
        _chkSafeRestore.Checked = true;
        _chkSafeRestore.Width = 110;

        _chkDryRun.Text = "Dry Run";
        _chkDryRun.Checked = false;
        _chkDryRun.Width = 90;

        _txtRestoreBranchPrefix.Width = 130;
        _txtRestoreBranchPrefix.Text = "restore";

        op.Controls.Add(_btnTag);
        op.Controls.Add(_btnRestore);
        op.Controls.Add(_btnUpdateTag);
        op.Controls.Add(_btnExportLog);
        op.Controls.Add(_btnClearLog);
        op.Controls.Add(_chkDryRun);
        op.Controls.Add(_chkSafeRestore);
        op.Controls.Add(new Label { Text = "Branch Prefix", AutoSize = true, Padding = new Padding(8, 8, 4, 0) });
        op.Controls.Add(_txtRestoreBranchPrefix);

        root.Controls.Add(op, 0, 1);

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.DataSource = _rows;

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Use",
            DataPropertyName = nameof(RepoRow.Use),
            Width = 50
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Repo",
            DataPropertyName = nameof(RepoRow.Name),
            Width = 220,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Path",
            DataPropertyName = nameof(RepoRow.Path),
            Width = 360,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Branch",
            DataPropertyName = nameof(RepoRow.Branch),
            Width = 130,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "HEAD",
            DataPropertyName = nameof(RepoRow.Head),
            Width = 260,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Remote",
            DataPropertyName = nameof(RepoRow.Remote),
            Width = 360,
            ReadOnly = true
        });

        root.Controls.Add(_grid, 0, 2);

        _log.Dock = DockStyle.Fill;
        _log.ReadOnly = true;
        _log.Font = new Font("Consolas", 10);
        root.Controls.Add(_log, 0, 3);
    }

    private void ScanRepositories()
    {
        _rows.Clear();

        var rootPath = _txtRoot.Text.Trim();
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            AppendLog($"Root directory not found: {rootPath}");
            return;
        }

        AddRepoRow("driver_man", rootPath);

        var submodules = GitHelper.ParseGitModules(rootPath);
        foreach (var module in submodules)
        {
            var subPath = Path.Combine(rootPath, module.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(subPath))
            {
                AppendLog($"Skip missing submodule path: {subPath}");
                continue;
            }

            AddRepoRow(module.Name, subPath);
        }

        AppendLog($"Scan complete. Loaded {_rows.Count} repositories.");
    }

    private void AddRepoRow(string name, string path)
    {
        _rows.Add(new RepoRow
        {
            Use = true,
            Name = name,
            Path = path,
            Branch = GitHelper.GetCurrentBranch(path),
            Head = GitHelper.GetHeadShort(path),
            Remote = GitHelper.GetRemoteUrl(path)
        });
    }

    private async Task CreateTagsAsync()
    {
        await RunWithUiLock(async () =>
        {
            var tag = GetTagOrThrow();
            var dryRun = _chkDryRun.Checked;
            var selected = _rows.Where(r => r.Use).ToList();

            await Task.Run(() =>
            {
                foreach (var repo in selected)
                {
                    AppendLog($"[{repo.Name}] create tag {tag}");
                    ExecuteGit(repo, $"tag {GitHelper.EscapeArg(tag)}", "create tag", dryRun);
                    ExecuteGit(repo, $"push origin {GitHelper.EscapeArg(tag)}", "push tag", dryRun);
                }
            });

            AppendLog("Create tags completed.");
        });
    }

    private async Task RestoreToTagAsync()
    {
        await RunWithUiLock(async () =>
        {
            var tag = GetTagOrThrow();
            var dryRun = _chkDryRun.Checked;
            var safeRestore = _chkSafeRestore.Checked;
            var branchPrefix = (_txtRestoreBranchPrefix.Text ?? string.Empty).Trim();
            if (safeRestore && string.IsNullOrWhiteSpace(branchPrefix))
            {
                branchPrefix = "restore";
            }

            var selected = _rows.Where(r => r.Use).ToList();

            await Task.Run(() =>
            {
                foreach (var repo in selected)
                {
                    if (!dryRun)
                    {
                        EnsureCleanWorkingTree(repo);
                    }
                    else
                    {
                        AppendLog($"[DRY-RUN][{repo.Name}] check clean working tree");
                    }

                    ExecuteGit(repo, "fetch --tags", "fetch tags", dryRun);

                    if (safeRestore)
                    {
                        var restoreBranch = BuildRestoreBranchName(branchPrefix, tag);
                        AppendLog($"[{repo.Name}] safe restore {tag} -> {restoreBranch}");
                        ExecuteGit(repo, $"checkout -B {GitHelper.EscapeArg(restoreBranch)} {GitHelper.EscapeArg(tag)}", "checkout restore branch", dryRun);
                    }
                    else
                    {
                        AppendLog($"[{repo.Name}] checkout {tag}");
                        ExecuteGit(repo, $"checkout {GitHelper.EscapeArg(tag)}", "checkout tag", dryRun);
                    }
                }
            });

            AppendLog("Restore completed.");
            BeginInvoke(ScanRepositories);
        });
    }

    private async Task UpdateTagsAsync()
    {
        await RunWithUiLock(async () =>
        {
            var tag = GetTagOrThrow();
            var dryRun = _chkDryRun.Checked;
            var selected = _rows.Where(r => r.Use).ToList();

            await Task.Run(() =>
            {
                foreach (var repo in selected)
                {
                    AppendLog($"[{repo.Name}] update tag {tag}");

                    ExecuteGit(repo, $"tag -d {GitHelper.EscapeArg(tag)}", "delete local tag", dryRun, ignoreFailure: true);
                    ExecuteGit(repo, $"push origin :refs/tags/{tag}", "delete remote tag", dryRun, ignoreFailure: true);
                    ExecuteGit(repo, $"tag {GitHelper.EscapeArg(tag)}", "recreate tag", dryRun);
                    ExecuteGit(repo, $"push origin {GitHelper.EscapeArg(tag)}", "push tag", dryRun);
                }
            });

            AppendLog("Update tags completed.");
        });
    }

    private string GetTagOrThrow()
    {
        var tag = _txtTag.Text.Trim();
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new InvalidOperationException("Tag content is required.");
        }

        return tag;
    }

    private static string BuildRestoreBranchName(string prefix, string tag)
    {
        static string Sanitize(string value)
        {
            var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '~', '^', ':', '?', '*', '[', '\\' }).ToHashSet();
            var chars = value
                .Select(ch => invalid.Contains(ch) || ch == '/' ? '-' : ch)
                .ToArray();

            var sanitized = new string(chars).Trim().Trim('-').Trim('.');
            return string.IsNullOrWhiteSpace(sanitized) ? "tag" : sanitized;
        }

        var safePrefix = Sanitize(prefix);
        var safeTag = Sanitize(tag);
        return $"{safePrefix}/{safeTag}";
    }

    private void EnsureCleanWorkingTree(RepoRow repo)
    {
        var status = GitHelper.RunGit("status --porcelain", repo.Path);
        if (!status.IsSuccess)
        {
            throw new InvalidOperationException($"[{repo.Name}] failed to check working tree: {status.StdErr}");
        }

        if (!string.IsNullOrWhiteSpace(status.StdOut))
        {
            throw new InvalidOperationException($"[{repo.Name}] working tree not clean. Please commit/stash changes before restore.");
        }
    }

    private void ExecuteGit(RepoRow repo, string args, string operation, bool dryRun, bool ignoreFailure = false)
    {
        if (dryRun)
        {
            AppendLog($"[DRY-RUN][{repo.Name}] git -C \"{repo.Path}\" {args}");
            return;
        }

        var result = GitHelper.RunGit(args, repo.Path);
        if (!result.IsSuccess)
        {
            if (ignoreFailure)
            {
                AppendLog($"[{repo.Name}] {operation} skipped: {result.StdErr}");
                return;
            }

            throw new InvalidOperationException($"[{repo.Name}] {operation} failed: {result.StdErr}");
        }

        if (!string.IsNullOrWhiteSpace(result.StdOut))
        {
            AppendLog($"[{repo.Name}] {result.StdOut}");
        }
    }

    private void RunOrThrow(CommandResult result, string repoName, string operation)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"[{repoName}] {operation} failed: {result.StdErr}");
        }

        if (!string.IsNullOrWhiteSpace(result.StdOut))
        {
            AppendLog($"[{repoName}] {result.StdOut}");
        }
    }

    private void ExportLog()
    {
        if (string.IsNullOrWhiteSpace(_log.Text))
        {
            MessageBox.Show("Log is empty.", "Export Log", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var defaultFileName = $"tagmanager_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        var defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        using var dialog = new SaveFileDialog
        {
            Title = "Export TagManager Log",
            Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            FileName = defaultFileName,
            InitialDirectory = Directory.Exists(defaultDir) ? defaultDir : Environment.CurrentDirectory,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var exportText = BuildExportContent();
        File.WriteAllText(dialog.FileName, exportText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        AppendLog($"Log exported: {dialog.FileName}");
        MessageBox.Show("Log exported successfully.", "Export Log", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private string BuildExportContent()
    {
        var sb = new StringBuilder();
        var selectedRepos = _rows.Where(x => x.Use).Select(x => x.Name).OrderBy(x => x).ToList();

        sb.AppendLine("# TagManager Export Log");
        sb.AppendLine($"GeneratedAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"WorkspaceRoot: {_txtRoot.Text.Trim()}");
        sb.AppendLine($"TagContent: {_txtTag.Text.Trim()}");
        sb.AppendLine($"DryRun: {_chkDryRun.Checked}");
        sb.AppendLine($"SafeRestore: {_chkSafeRestore.Checked}");
        sb.AppendLine($"RestoreBranchPrefix: {_txtRestoreBranchPrefix.Text.Trim()}");
        sb.AppendLine($"SelectedRepoCount: {selectedRepos.Count}");
        sb.AppendLine($"SelectedRepos: {(selectedRepos.Count == 0 ? "(none)" : string.Join(", ", selectedRepos))}");
        sb.AppendLine(new string('-', 80));
        sb.AppendLine(_log.Text);

        return sb.ToString();
    }

    private async Task RunWithUiLock(Func<Task> action)
    {
        SetUiEnabled(false);
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetUiEnabled(true);
        }
    }

    private void SetUiEnabled(bool enabled)
    {
        _btnScan.Enabled = enabled;
        _btnTag.Enabled = enabled;
        _btnRestore.Enabled = enabled;
        _btnUpdateTag.Enabled = enabled;
        _btnExportLog.Enabled = enabled;
        _btnClearLog.Enabled = enabled;
        _grid.Enabled = enabled;
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _log.ScrollToCaret();
    }

    private sealed class RepoRow : INotifyPropertyChanged
    {
        private bool _use;
        private string _name = string.Empty;
        private string _path = string.Empty;
        private string _branch = string.Empty;
        private string _head = string.Empty;
        private string _remote = string.Empty;

        public bool Use
        {
            get => _use;
            set { _use = value; OnPropertyChanged(nameof(Use)); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Path
        {
            get => _path;
            set { _path = value; OnPropertyChanged(nameof(Path)); }
        }

        public string Branch
        {
            get => _branch;
            set { _branch = value; OnPropertyChanged(nameof(Branch)); }
        }

        public string Head
        {
            get => _head;
            set { _head = value; OnPropertyChanged(nameof(Head)); }
        }

        public string Remote
        {
            get => _remote;
            set { _remote = value; OnPropertyChanged(nameof(Remote)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
