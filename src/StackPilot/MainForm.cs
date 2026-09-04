using System.Security.Principal;
using StackPilot.Models;
using StackPilot.Services;

namespace StackPilot;

public sealed class MainForm : Form
{
    private readonly List<PackageEntry> _packages;
    private readonly CheckedListBox _checkedList = new();
    private readonly TextBox _logBox = new();
    private readonly Button _btnInstall = new();
    private readonly Button _btnAll = new();
    private readonly Button _btnNone = new();
    private readonly Button _btnDefaults = new();
    private readonly Button _btnClose = new();
    private readonly Label _adminLabel = new();
    private CancellationTokenSource? _cts;

    public MainForm(IReadOnlyList<PackageEntry> packages)
    {
        _packages = packages.ToList();
        Text = "StackPilot";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(640, 520);
        Size = new Size(760, 640);
        Font = new Font("Segoe UI", 9F);

        var title = new Label
        {
            Text = "StackPilot",
            Font = new Font("Segoe UI Semibold", 14F),
            AutoSize = true,
            Location = new Point(16, 12)
        };

        var subtitle = new Label
        {
            Text = "Pack outils de developpement - Coche les logiciels a installer, puis clique Installer.",
            Location = new Point(18, 44),
            Size = new Size(700, 36)
        };

        _checkedList.Location = new Point(20, 88);
        _checkedList.Size = new Size(700, 260);
        _checkedList.CheckOnClick = true;
        _checkedList.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        foreach (var pkg in _packages)
        {
            var idx = _checkedList.Items.Add(pkg.DisplayLabel);
            _checkedList.SetItemChecked(idx, pkg.Default);
        }

        _btnAll.Text = "Tout cocher";
        _btnAll.Location = new Point(20, 360);
        _btnAll.Size = new Size(110, 30);
        _btnAll.Click += (_, _) => SetAll(true);

        _btnNone.Text = "Tout decocher";
        _btnNone.Location = new Point(140, 360);
        _btnNone.Size = new Size(110, 30);
        _btnNone.Click += (_, _) => SetAll(false);

        _btnDefaults.Text = "Par defaut";
        _btnDefaults.Location = new Point(260, 360);
        _btnDefaults.Size = new Size(110, 30);
        _btnDefaults.Click += (_, _) =>
        {
            for (var i = 0; i < _packages.Count; i++)
            {
                _checkedList.SetItemChecked(i, _packages[i].Default);
            }
        };

        _adminLabel.Location = new Point(400, 365);
        _adminLabel.Size = new Size(320, 24);
        if (IsAdministrator())
        {
            _adminLabel.Text = "Session: Administrateur";
            _adminLabel.ForeColor = Color.ForestGreen;
        }
        else
        {
            _adminLabel.Text = "Session: standard (UAC requis)";
            _adminLabel.ForeColor = Color.DarkOrange;
        }

        _logBox.Location = new Point(20, 400);
        _logBox.Size = new Size(700, 140);
        _logBox.Multiline = true;
        _logBox.ScrollBars = ScrollBars.Vertical;
        _logBox.ReadOnly = true;
        _logBox.Font = new Font("Consolas", 9F);
        _logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _btnInstall.Text = "Installer";
        _btnInstall.Location = new Point(490, 555);
        _btnInstall.Size = new Size(110, 32);
        _btnInstall.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnInstall.BackColor = Color.FromArgb(0, 120, 212);
        _btnInstall.ForeColor = Color.White;
        _btnInstall.FlatStyle = FlatStyle.Flat;
        _btnInstall.Click += async (_, _) => await InstallSelectedAsync().ConfigureAwait(true);

        _btnClose.Text = "Fermer";
        _btnClose.Location = new Point(610, 555);
        _btnClose.Size = new Size(110, 32);
        _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnClose.Click += (_, _) =>
        {
            _cts?.Cancel();
            Close();
        };

        Controls.Add(title);
        Controls.Add(subtitle);
        Controls.Add(_checkedList);
        Controls.Add(_btnAll);
        Controls.Add(_btnNone);
        Controls.Add(_btnDefaults);
        Controls.Add(_adminLabel);
        Controls.Add(_logBox);
        Controls.Add(_btnInstall);
        Controls.Add(_btnClose);
    }

    private void SetAll(bool value)
    {
        for (var i = 0; i < _checkedList.Items.Count; i++)
        {
            _checkedList.SetItemChecked(i, value);
        }
    }

    private async Task InstallSelectedAsync()
    {
        var selected = new List<PackageEntry>();
        for (var i = 0; i < _checkedList.Items.Count; i++)
        {
            if (_checkedList.GetItemChecked(i))
            {
                selected.Add(_packages[i]);
            }
        }

        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Selectionne au moins un outil.", "StackPilot",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true);
        _logBox.Clear();
        _cts = new CancellationTokenSource();
        var log = new Action<string>(line =>
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(() => AppendLog(line));
            }
            else
            {
                AppendLog(line);
            }
        });

        try
        {
            var orchestrator = new InstallOrchestrator();
            await orchestrator.EnsureReadyAsync(log, _cts.Token).ConfigureAwait(true);

            var ordered = InstallOrchestrator.OrderForInstall(selected);
            log($"[*] A installer ({ordered.Count}):");
            foreach (var pkg in ordered)
            {
                log($"  - {pkg.Name}");
            }

            var results = new List<InstallResult>();
            foreach (var pkg in ordered)
            {
                _cts.Token.ThrowIfCancellationRequested();
                results.Add(await orchestrator.InstallAsync(pkg, true, log, _cts.Token).ConfigureAwait(true));
            }

            log("");
            log("=== Resume ===");
            foreach (var group in results.GroupBy(r => r.Status))
            {
                log($"  {group.Key,-10} {group.Count()}");
            }

            var failed = results.Count(r => r.Status == InstallStatus.Failed);
            if (failed == 0)
            {
                MessageBox.Show(this,
                    "Installation terminee. Consulte le journal pour le detail.\nUn redemarrage peut etre necessaire (WSL/Docker).",
                    "StackPilot", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this,
                    $"Installation terminee avec {failed} erreur(s). Voir le journal.",
                    "StackPilot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (OperationCanceledException)
        {
            AppendLog("[!] Installation annulee.");
        }
        catch (Exception ex)
        {
            AppendLog($"[x] ERREUR: {ex.Message}");
            MessageBox.Show(this, ex.Message, "StackPilot", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void AppendLog(string line)
    {
        _logBox.AppendText(line + Environment.NewLine);
        _logBox.SelectionStart = _logBox.TextLength;
        _logBox.ScrollToCaret();
    }

    private void SetBusy(bool busy)
    {
        _btnInstall.Enabled = !busy;
        _btnAll.Enabled = !busy;
        _btnNone.Enabled = !busy;
        _btnDefaults.Enabled = !busy;
        _checkedList.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
