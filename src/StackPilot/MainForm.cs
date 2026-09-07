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
    private readonly ComboBox _profileCombo = new();
    private readonly TextBox _logBox = new();
    private readonly SoftButton _btnInstall = new();
    private readonly SoftButton _btnAll = new();
    private readonly SoftButton _btnNone = new();
    private readonly SoftButton _btnDefaults = new();
    private readonly SoftButton _btnClose = new();
    private readonly Label _adminLabel = new();
    private readonly Label _profileHint = new();
    private readonly Label _selectionCount = new();
    private readonly Panel _toolsScroll = new();
    private readonly ToolTip _toolTip = new();
    private readonly List<(PackageEntry Package, CheckBox Box)> _toolRows = new();
    private SoftComboHost? _profileComboHost;
    private CancellationTokenSource? _cts;
    private bool _suppressCheckEvents;

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
        MinimumSize = new Size(1080, 720);
        Size = new Size(1180, 800);
        Font = new Font("Segoe UI", 9.5F);
        BackColor = Mist;
        ForeColor = Ink;
        DoubleBuffered = true;

        _toolTip.ShowAlways = true;
        _toolTip.AutoPopDelay = 8000;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Mist
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildProfileBar(), 0, 1);
        root.Controls.Add(BuildMainArea(), 0, 2);
        root.Controls.Add(BuildFooter(), 0, 3);
        Controls.Add(root);

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
            Text = "Choisis un profil metier, ajuste la checklist, puis clique Installer.",
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
            Text = "Choisir un profil",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Moss,
            AutoSize = true,
            Location = new Point(28, 12)
        };

        _profileCombo.DropDownStyle = ComboBoxStyle.DropDownList;
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

        _profileComboHost = new SoftComboHost(_profileCombo)
        {
            Location = new Point(28, 34),
            Size = new Size(340, 40)
        };

        _profileHint.Font = new Font("Segoe UI", 9F);
        _profileHint.ForeColor = Muted;
        _profileHint.Location = new Point(388, 42);
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
        bar.Controls.Add(_profileComboHost);
        bar.Controls.Add(_profileHint);
        bar.Controls.Add(_adminLabel);
        bar.Controls.Add(bottomLine);

        void LayoutBar()
        {
            _profileHint.Width = Math.Max(160, bar.ClientSize.Width - 620);
            _adminLabel.Location = new Point(
                Math.Max(400, bar.ClientSize.Width - _adminLabel.PreferredWidth - 28),
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
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
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
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var head = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var title = new Label
        {
            Text = "Outils a installer",
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = Ink,
            AutoSize = true,
            Location = new Point(0, 6)
        };
        _selectionCount.Font = new Font("Segoe UI Semibold", 8.5F);
        _selectionCount.ForeColor = Moss;
        _selectionCount.AutoSize = true;
        _selectionCount.BackColor = Foam;
        _selectionCount.Padding = new Padding(8, 3, 8, 3);
        _selectionCount.Location = new Point(168, 6);
        head.Controls.Add(title);
        head.Controls.Add(_selectionCount);

        _toolsScroll.Dock = DockStyle.Fill;
        _toolsScroll.AutoScroll = true;
        _toolsScroll.BackColor = Color.FromArgb(0xFA, 0xFC, 0xFB);
        _toolsScroll.Padding = new Padding(4, 2, 4, 8);
        _toolsScroll.BorderStyle = BorderStyle.None;
        _toolsScroll.Resize += (_, _) => SyncToolsContentWidth();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0),
            BackColor = Color.White
        };

        StyleSoftButton(_btnAll, "Tout cocher", SoftButtonKind.Secondary, 128);
        StyleSoftButton(_btnNone, "Tout decocher", SoftButtonKind.Ghost, 134);
        StyleSoftButton(_btnDefaults, "Profil Base", SoftButtonKind.Secondary, 120);
        _btnAll.Click += (_, _) => { SetAll(true); UpdateSelectionCount(); };
        _btnNone.Click += (_, _) => { SetAll(false); UpdateSelectionCount(); };
        _btnDefaults.Click += (_, _) => ApplyBaseProfile();
        actions.Controls.Add(_btnAll);
        actions.Controls.Add(_btnNone);
        actions.Controls.Add(_btnDefaults);

        inner.Controls.Add(head, 0, 0);
        inner.Controls.Add(_toolsScroll, 0, 1);
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

        StyleSoftButton(_btnClose, "Fermer", SoftButtonKind.Ghost, 110);
        _btnClose.Height = 40;
        _btnClose.Click += (_, _) =>
        {
            _cts?.Cancel();
            Close();
        };

        StyleSoftButton(_btnInstall, "Installer", SoftButtonKind.Primary, 150);
        _btnInstall.Height = 40;
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

    private static void StyleSoftButton(SoftButton button, string text, SoftButtonKind kind, int width)
    {
        button.Text = text;
        button.Kind = kind;
        button.Size = new Size(width, kind == SoftButtonKind.Primary ? 40 : 34);
        button.Font = new Font("Segoe UI Semibold", kind == SoftButtonKind.Primary ? 10F : 9.25F);
        button.Margin = new Padding(0, 0, 10, 0);
        button.CornerRadius = 10;
    }

    private void RebuildToolsGrid(
        IReadOnlyList<PackageEntry> selectedFirst,
        IReadOnlyList<PackageEntry> others,
        ISet<string> checkedKeys)
    {
        _toolsScroll.SuspendLayout();
        _toolsScroll.Controls.Clear();
        _toolRows.Clear();

        var content = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location = Point.Empty,
            BackColor = Color.FromArgb(0xFA, 0xFC, 0xFB),
            Padding = new Padding(8, 6, 12, 6)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var row = 0;
        if (selectedFirst.Count > 0)
        {
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(
                BuildSectionLabel($"Dans ce profil · {selectedFirst.Count}", Moss, Foam),
                0,
                row++);
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(BuildPackageColumns(selectedFirst, checkedKeys, highlight: true), 0, row++);
        }

        if (others.Count > 0)
        {
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(
                BuildSectionLabel($"Autres outils · {others.Count}", Muted, Color.Transparent),
                0,
                row++);
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(BuildPackageColumns(others, checkedKeys, highlight: false), 0, row++);
        }

        _toolsScroll.Controls.Add(content);
        SyncToolsContentWidth();
        _toolsScroll.ResumeLayout(true);
    }

    private void SyncToolsContentWidth()
    {
        if (_toolsScroll.Controls.Count == 0)
        {
            return;
        }

        var content = _toolsScroll.Controls[0];
        // Keep enough width so each of the two columns can show full tool names.
        var width = Math.Max(720, _toolsScroll.ClientSize.Width - 8);
        content.Width = width;

        foreach (Control section in content.Controls)
        {
            if (section is not FlowLayoutPanel flow)
            {
                continue;
            }

            flow.Width = Math.Max(680, width - 24);
            var colWidth = Math.Max(320, (flow.ClientSize.Width - flow.Padding.Horizontal - 12) / 2);
            var count = 0;
            foreach (Control child in flow.Controls)
            {
                if (child is CheckBox box)
                {
                    box.Width = colWidth;
                    box.Height = 38;
                    count++;
                }
            }

            var rows = Math.Max(1, (count + 1) / 2);
            flow.Height = rows * 42 + flow.Padding.Vertical + 4;
        }
    }

    private static Label BuildSectionLabel(string text, Color fore, Color back)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI Semibold", 8.5F),
            ForeColor = fore,
            BackColor = back == Color.Transparent ? Color.FromArgb(0xFA, 0xFC, 0xFB) : back,
            AutoSize = true,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0, 8, 0, 6)
        };
    }

    private Control BuildPackageColumns(
        IReadOnlyList<PackageEntry> packages,
        ISet<string> checkedKeys,
        bool highlight)
    {
        var flow = new FlowLayoutPanel
        {
            AutoSize = false,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = highlight ? Foam : Color.FromArgb(0xFA, 0xFC, 0xFB),
            Padding = new Padding(highlight ? 10 : 4, highlight ? 10 : 4, highlight ? 10 : 4, highlight ? 10 : 4),
            Margin = new Padding(0, 0, 0, 6)
        };

        foreach (var pkg in packages)
        {
            var box = new CheckBox
            {
                Text = pkg.DisplayLabel,
                Checked = checkedKeys.Contains(pkg.Key),
                AutoSize = false,
                Size = new Size(340, 38),
                Font = new Font("Segoe UI", 9.75F),
                ForeColor = Ink,
                BackColor = Color.Transparent,
                Margin = new Padding(4, 2, 4, 2),
                Padding = new Padding(4, 0, 4, 0),
                Cursor = Cursors.Hand,
                AutoEllipsis = true,
                UseMnemonic = false,
                Tag = pkg
            };
            _toolTip.SetToolTip(box, pkg.DisplayLabel);
            box.CheckedChanged += ToolCheckChanged;
            _toolRows.Add((pkg, box));
            flow.Controls.Add(box);
        }

        // Approximate height: 2 columns → ceil(n/2) rows.
        var rows = Math.Max(1, (packages.Count + 1) / 2);
        flow.Height = rows * 42 + flow.Padding.Vertical + 4;
        return flow;
    }

    private void ToolCheckChanged(object? sender, EventArgs e)
    {
        if (_suppressCheckEvents)
        {
            return;
        }

        UpdateSelectionCount();
    }

    private void UpdateSelectionCount()
    {
        var count = _toolRows.Count(r => r.Box.Checked);
        var total = _packages.Count;
        _selectionCount.Text = count == 0
            ? "aucune selection"
            : $"{count} selectionne(s) / {total}";

        // Keep badge tucked after the title once preferred width is known.
        if (_selectionCount.Parent is Control head)
        {
            var title = head.Controls.OfType<Label>().FirstOrDefault(l => l != _selectionCount);
            if (title is not null)
            {
                _selectionCount.Location = new Point(title.Right + 12, 6);
            }
        }
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
            var defaults = _packages.Where(p => p.Default).ToList();
            var rest = _packages.Where(p => !p.Default).ToList();
            var keys = new HashSet<string>(defaults.Select(p => p.Key), StringComparer.OrdinalIgnoreCase);
            RebuildToolsGrid(defaults, rest, keys);
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

        // Preserve catalog relative order within each group.
        var selected = _packages.Where(p => wanted.Contains(p.Key)).ToList();
        var others = _packages.Where(p => !wanted.Contains(p.Key)).ToList();

        RebuildToolsGrid(selected, others, wanted);

        _profileHint.Text = string.IsNullOrWhiteSpace(profile.Description)
            ? $"{wanted.Count} outil(s) pour ce profil"
            : profile.Description!;
        UpdateSelectionCount();
    }

    private void SetAll(bool value)
    {
        _suppressCheckEvents = true;
        try
        {
            foreach (var row in _toolRows)
            {
                row.Box.Checked = value;
            }
        }
        finally
        {
            _suppressCheckEvents = false;
        }
    }

    private List<PackageEntry> GetSelectedPackages()
    {
        return _toolRows.Where(r => r.Box.Checked).Select(r => r.Package).ToList();
    }

    private async Task InstallSelectedAsync()
    {
        var selected = GetSelectedPackages();

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

            if (InvokeRequired && IsHandleCreated)
            {
                BeginInvoke(() => AppendLog(line));
            }
            else if (!IsDisposed)
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
        _toolsScroll.Enabled = !busy;
        foreach (var row in _toolRows)
        {
            row.Box.Enabled = !busy;
        }

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
