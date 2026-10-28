using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using BrightIdeasSoftware;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args) {
        if (args.Length != 3)
            throw new ArgumentException("Expected: write|read layout-file expected-framework");
        string framework = ((TargetFrameworkAttribute)Attribute.GetCustomAttribute(
            typeof(ObjectListView).Assembly, typeof(TargetFrameworkAttribute))).FrameworkName;
        if (framework != args[2])
            throw new InvalidOperationException("Wrong runtime asset selected: " + framework);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using (var form = new Form { Opacity = 0, ShowInTaskbar = false }) {
            form.Show();
            var models = new[] { new Row("Ada"), new Row("Grace") };
            using (var list = new ObjectListView { ShowGroups = false, View = View.Details }) {
                list.AllColumns.Add(new OLVColumn("Name", "Name") { Width = 133 });
                list.AllColumns.Add(new OLVColumn("Hidden", "Name") { IsVisible = false, Width = 77 });
                list.RebuildColumns();
                form.Controls.Add(list);
                list.SetObjects(models);
                list.CreateControl();
                byte[] original = list.SaveState();
                if (args[0] == "write")
                    File.WriteAllBytes(args[1], original);
                else if (args[0] == "read") {
                    if (!list.RestoreState(File.ReadAllBytes(args[1])))
                        throw new InvalidOperationException("Cross-target layout restore failed.");
                } else {
                    throw new ArgumentException("Unknown layout operation.");
                }
                list.AllColumns[0].Width = 201;
                if (!list.RestoreState(original) || list.AllColumns[0].Width != 133 ||
                    list.AllColumns[1].IsVisible || list.AllColumns[1].Width != 77)
                    throw new InvalidOperationException("Layout round trip failed.");
                var exporter = new OLVExporter(list);
                if (!exporter.ExportTo(OLVExporter.ExportFormat.TSV).Contains("Grace"))
                    throw new InvalidOperationException("Export failed.");
            }
            using (var fast = new FastObjectListView { ShowGroups = false, View = View.Details })
            using (var tree = new TreeListView { ShowGroups = false, View = View.Details })
            using (var data = new DataListView { ShowGroups = false, View = View.Details })
            using (var typed = new TypedObjectListView<Row> { ShowGroups = false, View = View.Details }) {
                foreach (var list in new ObjectListViewBase[] { fast, tree, data, typed }) {
                    list.Columns.Add(new OLVColumn("Name", "Name"));
                    form.Controls.Add(list);
                    list.CreateControl();
                }
                fast.SetObjects(models);
                tree.CanExpandGetter = model => false;
                tree.Roots = models;
                data.DataSource = new BindingList<Row>(models.ToList());
                typed.Objects = models;
                if (new ObjectListViewBase[] { fast, tree, data, typed }.Any(list => list.GetItemCount() != 2))
                    throw new InvalidOperationException("Packaged control initialization failed.");
            }
            using (var columns = new ColumnSelectionForm())
                columns.CreateControl();
        }
        if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
            assembly.GetName().Name.StartsWith("Microsoft.DotNet.DesignTools", StringComparison.Ordinal)))
            throw new InvalidOperationException("A runtime consumer loaded Designer SDK assemblies.");
        Console.WriteLine("PASS: " + framework + ", " + (IntPtr.Size * 8) + "-bit");
    }

    public sealed class Row
    {
        public Row(string name) { this.Name = name; }
        public string Name { get; }
    }
}
