using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class MenubarsFixture : UserControl
{
    private bool guides;
    private string mode = "a";
    public MenubarsFixture() { InitializeComponent(); Update(); }
    private void Update() { Default.Menus = Build(); Disabled.Menus = Build(); Rtl.Menus = Build(); Result.Text = $"Fictional guides: {guides}; mode: {mode}"; }
    private BleeckerMenubarMenu[] Build() => [
        new("File", [
            new BleeckerMenubarAction("New fictional draft", () => Result.Text = "New fictional draft selected", shortcut: "Ctrl+N"),
            new BleeckerMenubarAction("Unavailable action", () => Result.Text = "Unexpected disabled action", isDisabled: true),
            new BleeckerMenubarSeparator(),
            new BleeckerMenubarCheckbox("Show fictional guides", guides, value => { guides = value; Update(); }, shortcut: "Ctrl+G"),
            new BleeckerMenubarRadioGroup(mode, value => { mode = value; Update(); }, [new("Fictional Alpha mode", "a"), new("Fictional Beta mode", "b"), new("Unavailable mode", "c", true)]),
            new BleeckerMenubarSeparator(),
            new BleeckerMenubarSubmenu("More fictional choices", [new BleeckerMenubarAction("Nested original action", () => Result.Text = "Nested fictional action selected"), new BleeckerMenubarSubmenu("Deeper fictional choices", [new BleeckerMenubarAction("Deep original action", () => Result.Text = "Deep fictional action selected")])])
        ]),
        new("Edit", [new BleeckerMenubarAction("Edit fictional item", () => Result.Text = "Fictional edit selected")])
    ];
}
