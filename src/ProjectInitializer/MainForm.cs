using Shared;
using System.ComponentModel;
using System.Text;

namespace ProjectInitializer;

public sealed class MainForm : Form
{
    private readonly TextBox _txtTemplateRoot = new();
    private readonly TextBox _txtOwner = new();
    private readonly TextBox _txtProjectName = new();
    private readonly TextBox _txtParentPath = new();
    private readonly CheckBox _chkCreateGitHubRepo = new();
    private readonly ComboBox _cmbVisibility = new();
    private readonly DataGridView _grid = new();
    private readonly RichTextBox _log = new();

    private readonly BindingList<SubmoduleRow> _rows = new();

    private Button _btnReload = null!;
    private Button _btnResolveBranches = null!;
    private Button _btnInit = null!;

    public MainForm()
    {
        Text = "prj-initer / Project Initializer";
        Width = 1300;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        LoadDefaults();
        ReloadSubmodules();
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

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 4
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        AddLabeledControl(form, 0, "Template Root", _txtTemplateRoot);
        AddLabeledControl(form, 1, "GitHub Owner", _txtOwner);
        AddLabeledControl(form, 2, "Project Name", _txtProjectName);
        AddLabeledControl(form, 3, "Parent Path", _txtParentPath);

        root.Controls.Add(form, 0, 0);

        var actionRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        _chkCreateGitHubRepo.Text = "Create GitHub Repo";
        _chkCreateGitHubRepo.Checked = true;
        _chkCreateGitHubRepo.Width = 170;

        _cmbVisibility.Width = 100;
        _cmbVisibility.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbVisibility.Items.AddRange(new object[] { "private", "public" });
        _cmbVisibility.SelectedIndex = 0;

        _btnReload = new Button { Text = "Reload Submodules", Width = 160, Height = 30 };
        _btnResolveBranches = new Button { Text = "Resolve Default Branch", Width = 180, Height = 30 };
        _btnInit = new Button { Text = "Start Initialization", Width = 180, Height = 32 };

        _btnReload.Click += (_, _) => ReloadSubmodules();
        _btnResolveBranches.Click += async (_, _) => await ResolveDefaultBranchesAsync();
        _btnInit.Click += async (_, _) => await StartInitializationAsync();

        actionRow.Controls.Add(_chkCreateGitHubRepo);
        actionRow.Controls.Add(new Label { Text = "Visibility", AutoSize = true, Padding = new Padding(8, 8, 0, 0) });
        actionRow.Controls.Add(_cmbVisibility);
        actionRow.Controls.Add(_btnReload);
        actionRow.Controls.Add(_btnResolveBranches);
        actionRow.Controls.Add(_btnInit);

        root.Controls.Add(actionRow, 0, 1);

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
            DataPropertyName = nameof(SubmoduleRow.Include),
            Width = 50
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name",
            DataPropertyName = nameof(SubmoduleRow.Name),
            Width = 220,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Url",
            DataPropertyName = nameof(SubmoduleRow.Url),
            Width = 360,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Target Path",
            DataPropertyName = nameof(SubmoduleRow.Path),
            Width = 260,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Base Branch",
            DataPropertyName = nameof(SubmoduleRow.Branch),
            Width = 140
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "New Branch (Optional)",
            DataPropertyName = nameof(SubmoduleRow.NewBranch),
            Width = 180
        });

        root.Controls.Add(_grid, 0, 2);

        _log.Dock = DockStyle.Fill;
        _log.ReadOnly = true;
        _log.Font = new Font("Consolas", 10);
        root.Controls.Add(_log, 0, 3);
    }

    private static void AddLabeledControl(TableLayoutPanel panel, int row, string label, Control control)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lbl = new Label
        {
            Text = label,
            AutoSize = true,
            Padding = new Padding(0, 8, 8, 0)
        };

        control.Dock = DockStyle.Fill;

        panel.Controls.Add(lbl, 0, row);
        panel.Controls.Add(control, 1, row);

        // Keep column 3 available for future controls without shifting current layout.
        panel.SetColumnSpan(control, 3);
    }

    private void LoadDefaults()
    {
        _txtTemplateRoot.Text = @"I:\project\aisoft\driver_man";
        _txtOwner.Text = "ahairsoft";
        _txtProjectName.Text = string.Empty;
        _txtParentPath.Text = @"I:\project\aisoft";
    }

    private void ReloadSubmodules()
    {
        _rows.Clear();

        var root = _txtTemplateRoot.Text.Trim();
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            AppendLog($"Template root not found: {root}");
            return;
        }

        var modules = GitHelper.ParseGitModules(root);
        foreach (var m in modules)
        {
            _rows.Add(new SubmoduleRow
            {
                Include = true,
                Name = m.Name,
                Url = m.Url,
                Path = m.Path,
                Branch = string.IsNullOrWhiteSpace(m.Branch) ? "main" : m.Branch,
                NewBranch = string.Empty
            });
        }

        AppendLog($"Loaded {_rows.Count} submodules from .gitmodules");
    }

    private async Task ResolveDefaultBranchesAsync()
    {
        await RunWithUiLock(async () =>
        {
            AppendLog("Resolving default branches from remote repositories...");

            await Task.Run(() =>
            {
                foreach (var row in _rows)
                {
                    if (!row.Include || string.IsNullOrWhiteSpace(row.Url))
                    {
                        continue;
                    }

                    var branch = GitHelper.GetRemoteDefaultBranch(row.Url);
                    BeginInvoke(() => row.Branch = branch);
                    AppendLog($"{row.Name}: default branch = {branch}");
                }
            });

            AppendLog("Default branch resolution finished.");
        });
    }

    private async Task StartInitializationAsync()
    {
        await RunWithUiLock(async () =>
        {
            var owner = _txtOwner.Text.Trim();
            var projectName = _txtProjectName.Text.Trim();
            var parentPath = _txtParentPath.Text.Trim();
            var createRepo = _chkCreateGitHubRepo.Checked;
            var visibility = (_cmbVisibility.SelectedItem?.ToString() ?? "private").Trim();

            if (string.IsNullOrWhiteSpace(projectName))
            {
                MessageBox.Show("Project Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(parentPath))
            {
                MessageBox.Show("Parent Path is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (createRepo && string.IsNullOrWhiteSpace(owner))
            {
                MessageBox.Show("GitHub Owner is required when creating remote repo.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedModules = _rows.Where(r => r.Include).ToList();
            var projectRoot = Path.Combine(parentPath, projectName);

            await Task.Run(() =>
            {
                AppendLog($"Target project path: {projectRoot}");

                if (Directory.Exists(projectRoot) && Directory.EnumerateFileSystemEntries(projectRoot).Any())
                {
                    throw new InvalidOperationException("Target directory already exists and is not empty.");
                }

                Directory.CreateDirectory(projectRoot);
                WriteBaseFiles(projectRoot, projectName);

                RunOrThrow(GitHelper.RunGit("init -b main", projectRoot), "git init", projectRoot);

                if (createRepo)
                {
                    AppendLog("Creating GitHub remote repository...");
                    var ghCmd = $"repo create {owner}/{projectName} --{visibility} --confirm";
                    RunOrThrow(GitHelper.RunGh(ghCmd, projectRoot), ghCmd, projectRoot);

                    var remoteUrl = $"https://github.com/{owner}/{projectName}.git";
                    var addOriginResult = GitHelper.RunGit($"remote add origin {GitHelper.EscapeArg(remoteUrl)}", projectRoot);
                    if (!addOriginResult.IsSuccess && !addOriginResult.StdErr.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Failed to add origin: {addOriginResult.StdErr}");
                    }
                }

                foreach (var module in selectedModules)
                {
                    var baseBranch = string.IsNullOrWhiteSpace(module.Branch) ? "main" : module.Branch.Trim();
                    AppendLog($"Adding submodule: {module.Name} ({baseBranch})");

                    var addCmd = $"submodule add -b {GitHelper.EscapeArg(baseBranch)} {GitHelper.EscapeArg(module.Url)} {GitHelper.EscapeArg(module.Path)}";
                    RunOrThrow(GitHelper.RunGit(addCmd, projectRoot), addCmd, projectRoot);

                    if (!string.IsNullOrWhiteSpace(module.NewBranch))
                    {
                        var newBranch = module.NewBranch.Trim();
                        var modulePath = Path.Combine(projectRoot, module.Path.Replace('/', Path.DirectorySeparatorChar));
                        AppendLog($"Creating new branch for {module.Name}: {newBranch} from {baseBranch}");

                        RunOrThrow(GitHelper.RunGit($"fetch origin {GitHelper.EscapeArg(baseBranch)}", modulePath), "fetch base branch", modulePath);
                        RunOrThrow(GitHelper.RunGit($"checkout -B {GitHelper.EscapeArg(newBranch)} origin/{baseBranch}", modulePath), "checkout new branch", modulePath);

                        var pushResult = GitHelper.RunGit($"push -u origin {GitHelper.EscapeArg(newBranch)}", modulePath);
                        if (!pushResult.IsSuccess)
                        {
                            AppendLog($"WARN: push new branch failed for {module.Name}: {pushResult.StdErr}");
                        }
                    }
                }

                RunOrThrow(GitHelper.RunGit("add .", projectRoot), "git add", projectRoot);
                var commitResult = GitHelper.RunGit("commit -m \"Initial project setup\"", projectRoot);
                if (!commitResult.IsSuccess && !commitResult.StdErr.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"git commit failed: {commitResult.StdErr}");
                }

                if (createRepo)
                {
                    RunOrThrow(GitHelper.RunGit("push -u origin main", projectRoot), "git push", projectRoot);
                }
            });

            AppendLog("Initialization completed successfully.");
            MessageBox.Show("Project initialization completed.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private static void WriteBaseFiles(string projectRoot, string projectName)
    {
        var readmePath = Path.Combine(projectRoot, "README.md");
        var gitignorePath = Path.Combine(projectRoot, ".gitignore");

        var readme = new StringBuilder();
        readme.AppendLine($"# {projectName}");
        readme.AppendLine();
        readme.AppendLine("Initialized by prj-initer.");
        readme.AppendLine();
        readme.AppendLine("## Usage");
        readme.AppendLine("1. git submodule update --init --recursive");

        var gitignore = new StringBuilder();
        gitignore.AppendLine(".vs/");
        gitignore.AppendLine("**/bin/");
        gitignore.AppendLine("**/obj/");
        gitignore.AppendLine("*.user");
        gitignore.AppendLine("*.suo");

        File.WriteAllText(readmePath, readme.ToString(), Encoding.UTF8);
        File.WriteAllText(gitignorePath, gitignore.ToString(), Encoding.UTF8);
    }

    private void RunOrThrow(CommandResult result, string commandName, string workDir)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"{commandName} failed in {workDir}\n{result.StdErr}");
        }

        if (!string.IsNullOrWhiteSpace(result.StdOut))
        {
            AppendLog(result.StdOut);
        }
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
        _btnReload.Enabled = enabled;
        _btnResolveBranches.Enabled = enabled;
        _btnInit.Enabled = enabled;
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

    private sealed class SubmoduleRow : INotifyPropertyChanged
    {
        private bool _include;
        private string _name = string.Empty;
        private string _url = string.Empty;
        private string _path = string.Empty;
        private string _branch = string.Empty;
        private string _newBranch = string.Empty;

        public bool Include
        {
            get => _include;
            set { _include = value; OnPropertyChanged(nameof(Include)); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(nameof(Url)); }
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

        public string NewBranch
        {
            get => _newBranch;
            set { _newBranch = value; OnPropertyChanged(nameof(NewBranch)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
