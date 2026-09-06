using System.Security.Principal;
using StackPilot.Models;
using StackPilot.Services;

namespace StackPilot;

public sealed class MainForm : Form
{
    private static readonly Color Pine = Color.FromArgb(0x0C, 0x1C, 0x18);
    private static readonly Color PineMid = Color.FromArgb(0x14, 0x30, 0x29);
    private static readonly Color Moss = Color.FromArgb(0x1F, 0x6B, 0x54);
    private static readonly Color Mint = Color.FromArgb(0x2F, 0x9E, 0x7B);
    private static readonly Color Foam = Color.FromArgb(0xE8, 0xF5, 0xEF);
    private static readonly Color Mist = Color.FromArgb(0xF3, 0xF7, 0xF5);
    private static readonly Color Ink = Color.FromArgb(0x13, 0x21, 0x1D);
    private static readonly Color Muted = Color.FromArgb(0x5B, 0x70, 0x68);
    private static readonly Color Line = Color.FromArgb(0xD5, 0xE3, 0xDC);
    private static readonly Color Warn = Color.FromArgb(0xC9, 0x78, 0x2C);
    private static readonly Color Ok = Color.FromArgb(0x1F, 0x8A, 0x5B);

    private readonly List<PackageEntry> _packages;
    private readonly List<ProfileEntry> _profiles;
    private readonly CheckedListBox _checkedList = new();
    private readonly ComboBox _profileCombo = new();
    private readonly TextBox _logBox = new();
    private readonly Button _btnInstall = new();
    private readonly Button _btnAll = new();
    private readonly Button _btnNone = new();
    private readonly Button _btnDefaults = new();
    private readonly Button _btnClose = new();
    private readonly Label _adminLabel = new();
    private readonly Label _profileHint = new();
    private readonly Label _selectionCount = new();
    private CancellationTokenSource? _cts;

    public MainForm(CatalogDocument catalog)
    {
        _packages = catalog.Packages.ToList();
        _profiles = catalog.Profiles.Count > 0
            ? catalog.Profiles.ToList()
            : new List<ProfileEntry>
            {
                new()
                {
                    Key = "base",
                    Name = "Base",
                    Description = "Paquets marques default.",
                    Packages = _packages.Where(p => p.Default).Select(p => p.Key).ToList()
                }
            };

        Text = "StackPilot";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 660);
        Size = new Size(920, 740);
        Font = new Font("Segoe UI", 9.5F);
        BackColor = Mist;
        ForeColor = Ink;
        DoubleBuffered = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Mist
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildProfileBar(), 0, 1);
        root.Controls.Add(BuildMainArea(), 0, 2);
        root.Controls.Add(BuildFooter(), 0, 3);
        Controls.Add(root);

        _checkedList.ItemCheck += (_, _) => BeginInvoke(UpdateSelectionCount);
        SelectInitialProfile();
        UpdateSelectionCount();
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Pine
        };

        var brand = new Label
        {
            Text = "StackPilot",
            Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
            ForeColor = Foam,
            AutoSize = true,
            Location = new Point(28, 16)
        };

        var accent = new Panel
        {
            BackColor = Mint,
            Size = new Size(52, 4),
            Location = new Point(30, 52)
        };

        var tagline = new Label
        {
            Text = "Pack outils de developpement — profils metier, checklist, winget.",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(0xA8, 0xCB, 0xBE),
            AutoSize = true,
            Location = new Point(30, 62)
        };

        header.Controls.Add(brand);
        header.Controls.Add(accent);
        header.Controls.Add(tagline);
        return header;
    }

    private Control BuildProfileBar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        var bottomLine = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Line
        };

        var label = new Label
        {
            Text = "PROFIL",
            Font = new Font("Segoe UI Semibold", 8F),
            ForeColor = Moss,
            AutoSize = true,
            Location = new Point(28, 14)
        };

        _profileCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _profileCombo.FlatStyle = FlatStyle.Flat;
        _profileCombo.Font = new Font("Segoe UI Semibold", 10F);
        _profileCombo.Size = new Size(260, 32);
        _profileCombo.Location = new Point(28, 36);
        _profileCombo.DisplayMember = nameof(ProfileEntry.Name);
        foreach (var profile in _profiles)
        {
            _profileCombo.Items.Add(profile);
        }

        _profileCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_profileCombo.SelectedItem is ProfileEntry profile)
            {
                ApplyProfile(profile);
            }
        };

        _profileHint.Font = new Font("Segoe UI", 9F);
        _profileHint.ForeColor = Muted;
        _profileHint.Location = new Point(304, 40);
        _profileHint.Size = new Size(360, 28);

        _adminLabel.Font = new Font("Segoe UI Semibold", 8.5F);
        _adminLabel.AutoSize = true;
        if (IsAdministrator())
        {
            _adminLabel.Text = "Session administrateur";
            _adminLabel.ForeColor = Ok;
        }
        else
        {
            _adminLabel.Text = "Session standard · UAC requis";
            _adminLabel.ForeColor = Warn;
        }

        bar.Controls.Add(label);
        bar.Controls.Add(_profileCombo);
        bar.Controls.Add(_profileHint);
        bar.Controls.Add(_adminLabel);
        bar.Controls.Add(bottomLine);

        void LayoutBar()
        {
            _profileHint.Width = Math.Max(160, bar.ClientSize.Width - 540);
            _adminLabel.Location = new Point(
                Math.Max(320, bar.ClientSize.Width - _adminLabel.PreferredWidth - 28),
                16);
        }

        bar.Resize += (_, _) => LayoutBar();
        LayoutBar();
        return bar;
    }

    private Control BuildMainArea()
    {
        var area = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(24, 16, 24, 8),
            BackColor = Mist
        };
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        area.Controls.Add(BuildChecklistCard(), 0, 0);
        area.Controls.Add(BuildLogCard(), 0, 1);
        return area;
    }

    private Control BuildChecklistCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(1)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Line);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var inner = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.White,
            Padding = new Padding(16, 12, 16, 10)
        };
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var head = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var title = new Label
        {
            Text = "Outils a installer",
            Font = new Font("Segoe UI Semibold", 10.5F),
            ForeColor = Ink,
            AutoSize = true,
            Location = new Point(0, 4)
        };
        _selectionCount.Font = new Font("Segoe UI", 9F);
        _selectionCount.ForeColor = Muted;
        _selectionCount.AutoSize = true;
        _selectionCount.Location = new Point(150, 6);
        head.Controls.Add(title);
        head.Controls.Add(_selectionCount);

        _checkedList.Dock = DockStyle.Fill;
        _checkedList.CheckOnClick = true;
        _checkedList.BorderStyle = BorderStyle.None;
        _checkedList.Font = new Font("Segoe UI", 10F);
        _checkedList.IntegralHeight = false;
        _checkedList.BackColor = Color.White;
        _checkedList.ForeColor = Ink;
        foreach (var pkg in _packages)
        {
            _checkedList.Items.Add(pkg.DisplayLabel);
        }

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0),
            BackColor = Color.White
        };

        StyleSecondaryButton(_btnAll, "Tout cocher", 118);
        StyleSecondaryButton(_btnNone, "Tout decocher", 124);
        StyleSecondaryButton(_btnDefaults, "Profil Base", 110);
        _btnAll.Click += (_, _) => { SetAll(true); UpdateSelectionCount(); };
        _btnNone.Click += (_, _) => { SetAll(false); UpdateSelectionCount(); };
        _btnDefaults.Click += (_, _) => ApplyBaseProfile();
        actions.Controls.Add(_btnAll);
        actions.Controls.Add(_btnNone);
        actions.Controls.Add(_btnDefaults);

        inner.Controls.Add(head, 0, 0);
        inner.Controls.Add(_checkedList, 0, 1);
        inner.Controls.Add(actions, 0, 2);
        card.Controls.Add(inner);
        return card;
    }

    private Control BuildLogCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Pine,
            Margin = new Padding(0)
        };

        var inner = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Pine,
            Padding = new Padding(14, 10, 14, 12)
        };
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var head = new Label
        {
            Text = "JOURNAL",
            Font = new Font("Segoe UI Semibold", 8F),
            ForeColor = Mint,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _logBox.Dock = DockStyle.Fill;
        _logBox.Multiline = true;
        _logBox.ScrollBars = ScrollBars.Vertical;
        _logBox.ReadOnly = true;
        _logBox.BorderStyle = BorderStyle.None;
        _logBox.BackColor = PineMid;
        _logBox.ForeColor = Foam;
        _logBox.Font = new Font("Consolas", 9.25F);

        inner.Controls.Add(head, 0, 0);
        inner.Controls.Add(_logBox, 0, 1);
        card.Controls.Add(inner);
        return card;
    }

    private Control BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        var topLine = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Line
        };

        StyleSecondaryButton(_btnClose, "Fermer", 110);
        _btnClose.Height = 38;
        _btnClose.Click += (_, _) =>
        {
            _cts?.Cancel();
            Close();
        };

        StylePrimaryButton(_btnInstall, "Installer");
        _btnInstall.Size = new Size(150, 38);
        _btnInstall.Click += async (_, _) => await InstallSelectedAsync().ConfigureAwait(true);

        footer.Controls.Add(topLine);
        footer.Controls.Add(_btnClose);
        footer.Controls.Add(_btnInstall);

        void LayoutFooter()
        {
            _btnInstall.Location = new Point(footer.ClientSize.Width - _btnInstall.Width - 24, 18);
            _btnClose.Location = new Point(_btnInstall.Left - _btnClose.Width - 10, 18);
        }

        footer.Resize += (_, _) => LayoutFooter();
        LayoutFooter();
        return footer;
    }

    private static void StylePrimaryButton(Button button, string text)
    {
        button.Text = text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Mint;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI Semibold", 10F);
        button.Cursor = Cursors.Hand;
        button.FlatAppearance.MouseOverBackColor = Moss;
        button.FlatAppearance.MouseDownBackColor = PineMid;
    }

    private static void StyleSecondaryButton(Button button, string text, int width)
    {
        button.Text = text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Line;
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = Color.White;
        button.ForeColor = Ink;
        button.Font = new Font("Segoe UI Semibold", 9F);
        button.Cursor = Cursors.Hand;
        button.Size = new Size(width, 32);
        button.Margin = new Padding(0, 0, 8, 0);
        button.FlatAppearance.MouseOverBackColor = Foam;
        button.FlatAppearance.MouseDownBackColor = Line;
    }

    private void UpdateSelectionCount()
    {
        var count = 0;
        for (var i = 0; i < _checkedList.Items.Count; i++)
        {
            if (_checkedList.GetItemChecked(i))
            {
                count++;
            }
        }

        _selectionCount.Text = count == 0
            ? "aucune selection"
            : $"{count} selectionne(s) / {_packages.Count}";
    }

    private void SelectInitialProfile()
    {
        var baseIndex = _profiles.FindIndex(p =>
            string.Equals(p.Key, "base", StringComparison.OrdinalIgnoreCase));
        _profileCombo.SelectedIndex = baseIndex >= 0 ? baseIndex : 0;
    }

    private void ApplyBaseProfile()
    {
        var baseProfile = _profiles.FirstOrDefault(p =>
            string.Equals(p.Key, "base", StringComparison.OrdinalIgnoreCase));
        if (baseProfile is null)
        {
            for (var i = 0; i < _packages.Count; i++)
            {
                _checkedList.SetItemChecked(i, _packages[i].Default);
            }

            UpdateSelectionCount();
            return;
        }

        var index = _profiles.IndexOf(baseProfile);
        if (_profileCombo.SelectedIndex != index)
        {
            _profileCombo.SelectedIndex = index;
        }
        else
        {
            ApplyProfile(baseProfile);
        }
    }

    private void ApplyProfile(ProfileEntry profile)
    {
        var wanted = new HashSet<string>(
            profile.Packages.Where(k => !string.IsNullOrWhiteSpace(k)),
            StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < _packages.Count; i++)
        {
            _checkedList.SetItemChecked(i, wanted.Contains(_packages[i].Key));
        }

        _profileHint.Text = string.IsNullOrWhiteSpace(profile.Description)
            ? $"{wanted.Count} outil(s) pour ce profil"
            : profile.Description!;
        UpdateSelectionCount();
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
        _profileCombo.Enabled = !busy;
        _checkedList.Enabled = !busy;
        _btnInstall.Text = busy ? "Installation..." : "Installer";
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
