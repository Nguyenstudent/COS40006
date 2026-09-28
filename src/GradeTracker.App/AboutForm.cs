using System.Reflection;
using System.Runtime.InteropServices;
using GradeTracker.Core;
using GradeTracker.Data;

namespace GradeTracker.App;

/// <summary>
/// Shows which DLLs were loaded, their versions and where they were loaded from.
/// Useful as deployment evidence: after installing the MSI, every path should
/// point into the install folder (e.g. C:\Program Files\...\Grade Tracker).
/// </summary>
public class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About Grade Tracker";
        Size = new Size(760, 360);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var assemblies = new (string Role, Assembly Assembly)[]
        {
            ("Executable", typeof(AboutForm).Assembly),
            ("Our DLL", typeof(GradeCalculator).Assembly),
            ("Our DLL", typeof(JsonUnitRepository).Assembly),
            ("3rd-party DLL", JsonUnitRepository.SerializerAssembly)
        };

        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        list.Columns.Add("Role", 100);
        list.Columns.Add("Assembly", 170);
        list.Columns.Add("Version", 80);
        list.Columns.Add("Loaded from", 390);

        foreach (var (role, assembly) in assemblies)
        {
            AssemblyName name = assembly.GetName();
            var item = new ListViewItem(role);
            item.SubItems.Add(name.Name ?? "?");
            item.SubItems.Add(name.Version?.ToString() ?? "?");
            item.SubItems.Add(assembly.Location);
            list.Items.Add(item);
        }

        var info = new Label
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(8),
            Text = $"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})\n" +
                   $"Install folder: {AppContext.BaseDirectory}"
        };

        var closeButton = new Button { Text = "Close", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 32 };
        AcceptButton = closeButton;

        Controls.Add(list);
        Controls.Add(info);
        Controls.Add(closeButton);
        list.BringToFront();
    }
}
