using GradeTracker.Core;
using GradeTracker.Core.Models;
using GradeTracker.Data;

namespace GradeTracker.App;

/// <summary>
/// Main window. The UI is built in code (no designer file) so the whole
/// form can be read in one place.
/// </summary>
public class MainForm : Form
{
    private readonly JsonUnitRepository _repository = new();
    private readonly List<UnitResult> _units = new();

    private readonly TextBox _codeBox = new() { Width = 100, PlaceholderText = "e.g. SWE40006" };
    private readonly TextBox _nameBox = new() { Width = 240, PlaceholderText = "Unit name" };
    private readonly NumericUpDown _creditBox = new()
    {
        Minimum = 0.5m, Maximum = 100m, DecimalPlaces = 1, Increment = 12.5m, Value = 12.5m, Width = 70
    };
    private readonly NumericUpDown _markBox = new()
    {
        Minimum = 0m, Maximum = 100m, DecimalPlaces = 1, Increment = 1m, Width = 70
    };
    private readonly DataGridView _grid = new();
    private readonly Label _summaryLabel = new() { AutoSize = true, Margin = new Padding(12, 9, 3, 3) };
    private readonly ToolStripStatusLabel _statusLabel = new();

    public MainForm()
    {
        Text = $"Grade Tracker v{typeof(MainForm).Assembly.GetName().Version?.ToString(3)}";
        MinimumSize = new Size(760, 420);
        Size = new Size(860, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        _summaryLabel.Font = new Font(Font.FontFamily, 10f, FontStyle.Bold);

        BuildLayout();
        Load += (_, _) => LoadUnits();
    }

    private void BuildLayout()
    {
        // Grid (fills the remaining space)
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = SystemColors.Window;
        _grid.Columns.Add("Code", "Unit code");
        _grid.Columns.Add("Name", "Unit name");
        _grid.Columns.Add("Credit", "Credit points");
        _grid.Columns.Add("Mark", "Mark");
        _grid.Columns.Add("Grade", "Grade");
        _grid.Columns["Name"]!.FillWeight = 250;

        // Input row (top)
        var addButton = new Button { Text = "Add unit", AutoSize = true };
        addButton.Click += (_, _) => AddUnit();
        AcceptButton = addButton;

        var inputPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8),
            WrapContents = true
        };
        inputPanel.Controls.AddRange(new Control[]
        {
            MakeLabel("Code"), _codeBox,
            MakeLabel("Name"), _nameBox,
            MakeLabel("Credit"), _creditBox,
            MakeLabel("Mark"), _markBox,
            addButton
        });

        // Action row (bottom)
        var removeButton = new Button { Text = "Remove selected", AutoSize = true };
        removeButton.Click += (_, _) => RemoveSelectedUnit();
        var aboutButton = new Button { Text = "About / DLL info", AutoSize = true };
        aboutButton.Click += (_, _) => ShowAbout();

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = new Padding(8)
        };
        actionPanel.Controls.AddRange(new Control[] { removeButton, aboutButton, _summaryLabel });

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_statusLabel);

        Controls.Add(_grid);
        Controls.Add(inputPanel);
        Controls.Add(actionPanel);
        Controls.Add(statusStrip);
        _grid.BringToFront(); // make the Fill control dock last
    }

    private static Label MakeLabel(string text) =>
        new() { Text = text, AutoSize = true, Margin = new Padding(6, 7, 3, 3) };

    private void LoadUnits()
    {
        try
        {
            _units.Clear();
            _units.AddRange(_repository.Load());
            _statusLabel.Text = $"Data file: {_repository.FilePath}";
        }
        catch (DataStoreException ex)
        {
            _statusLabel.Text = "Could not load saved data.";
            MessageBox.Show(ex.Message, "Load error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        RefreshGrid();
    }

    private bool SaveUnits()
    {
        try
        {
            _repository.Save(_units);
            _statusLabel.Text = $"Saved {DateTime.Now:HH:mm:ss} to {_repository.FilePath}";
            return true;
        }
        catch (DataStoreException ex)
        {
            MessageBox.Show(ex.Message, "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void AddUnit()
    {
        var unit = new UnitResult
        {
            Code = _codeBox.Text.Trim().ToUpperInvariant(),
            Name = _nameBox.Text.Trim(),
            CreditPoints = (double)_creditBox.Value,
            Mark = (double)_markBox.Value
        };

        List<string> errors = UnitValidator.Validate(unit, _units);
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, errors), "Please check the unit",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _units.Add(unit);
        if (!SaveUnits())
        {
            _units.Remove(unit);
            return;
        }

        RefreshGrid();
        _codeBox.Clear();
        _nameBox.Clear();
        _markBox.Value = 0;
        _codeBox.Focus();
    }

    private void RemoveSelectedUnit()
    {
        if (_grid.CurrentRow is null || _grid.CurrentRow.Index < 0 || _grid.CurrentRow.Index >= _units.Count)
        {
            MessageBox.Show("Select a unit first.", "Remove unit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        UnitResult unit = _units[_grid.CurrentRow.Index];
        var answer = MessageBox.Show($"Remove {unit.Code} {unit.Name}?", "Remove unit",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
        {
            return;
        }

        _units.Remove(unit);
        SaveUnits();
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        _grid.Rows.Clear();
        foreach (UnitResult u in _units)
        {
            Grade grade = GradeCalculator.GetGrade(u.Mark);
            _grid.Rows.Add(u.Code, u.Name, u.CreditPoints.ToString("0.0"), u.Mark.ToString("0.0"),
                $"{grade} ({GradeCalculator.GetGradeName(grade)})");
        }

        if (_units.Count == 0)
        {
            _summaryLabel.Text = "No units yet - add your first unit above.";
            return;
        }

        double wam = GradeCalculator.CalculateWam(_units);
        double gpa = GradeCalculator.CalculateGpa(_units);
        _summaryLabel.Text = $"Units: {_units.Count}    WAM: {wam:0.00}    GPA: {gpa:0.00} / 4.00";
    }

    private void ShowAbout()
    {
        using var dialog = new AboutForm();
        dialog.ShowDialog(this);
    }
}
