using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog.Demos;

public sealed partial class EditorialWorkspaceFixture : UserControl
{
    internal EditorialDemoState Model { get; } = new();
    private readonly Dictionary<string, PageRow> rows = [];
    private readonly Dictionary<string, ChoiceRow> choices = [];
    private bool updating;
    public event Action<string>? SuiteRequested;

    public EditorialWorkspaceFixture()
    {
        InitializeComponent();
        Suite.Options = [new("monitoring", "Monitoring demo"), new("editorial", "Editorial demo")];
        Suite.ValueRequested += (_, args) => { if (args.Value == "monitoring") Model.SelectSuite(args.Value); };
        Model.SuiteRequested += suite => SuiteRequested?.Invoke(suite);
        foreach (var (id, label) in new[] { ("pages", "Pages"), ("media", "Media"), ("categories", "Categories"), ("authors", "Authors") })
            Sidebar.Items.Add(new() { Id = id, Label = label, Href = "#demo-" + id, Command = new DemoCommand(() => Model.SelectSection(id), () => Model.Pending is null) });
        StatusFilter.Options = [new("", "All statuses"), new("draft", "Draft"), new("review", "Review")];
        LanguageFilter.Options = [new("", "All languages"), new("en", "English"), new("pt", "Português")];
        PageStatus.Options = [new("draft", "Draft"), new("review", "Review")];
        Media.Options = EditorialDemoState.Media.Select(item => new BleeckerSelectOption(item.Id, item.Label)).ToArray();
        StatusFilter.ValueRequested += (_, args) => Model.Filter(Search.Text, args.Value, Model.LanguageFilter);
        LanguageFilter.ValueRequested += (_, args) => Model.Filter(Search.Text, Model.StatusFilter, args.Value);
        Search.RegisterPropertyChangedCallback(BleeckerSearchInput.TextProperty, (_, _) => { if (!updating) Model.Filter(Search.Text, Model.StatusFilter, Model.LanguageFilter); });
        PageTitle.TextChanged += (_, _) => Edit();
        Body.RegisterPropertyChangedCallback(BleeckerTextarea.TextProperty, (_, _) => Edit());
        PageStatus.ValueRequested += (_, args) => { if (Model.Draft is { } draft) Model.Edit(draft.Title, draft.Body, args.Value); };
        Media.ValueRequested += (_, args) => Model.SelectMedia(args.Value);
        Sort.Command = new DemoCommand(Model.ToggleSort);
        Save.Command = new DemoCommand(Model.Save, () => Model.CanSave);
        KeepEditing.Command = new DemoCommand(Model.CancelPending, () => Model.Pending is not null);
        Discard.Command = new DemoCommand(Model.ConfirmDiscard, () => Model.Pending is not null);
        foreach (var page in Model.Pages) rows.Add(page.Id, new(this, page));
        AddChoices("category", EditorialDemoState.Categories, Categories);
        AddChoices("author", EditorialDemoState.Authors, Authors);
        Model.Changed += Render;
        Loaded += (_, _) => Render();
        Render();
    }

    private void AddChoices(string kind, IEnumerable<DemoChoice> values, StackPanel panel)
    {
        foreach (var choice in values)
        {
            var row = new ChoiceRow(this, kind, choice);
            choices.Add(kind + ":" + choice.Id, row);
            panel.Children.Add(row.View);
        }
    }

    private void Edit()
    {
        if (!updating && Model.Draft is { } draft) Model.Edit(PageTitle.Text, Body.Text, draft.Status);
    }

    private void Render()
    {
        if (updating) return;
        updating = true;
        try
        {
            var visible = Model.Visible.ToArray();
            WorkspaceDemoUi.Reconcile(Rows, visible.Select(page => (UIElement)rows[page.Id].View).ToArray());
            foreach (var page in Model.Pages) rows[page.Id].Update(Model.SelectedIds.Contains(page.Id));
            StatusFilter.Value = Model.StatusFilter;
            LanguageFilter.Value = Model.LanguageFilter;
            Sort.Content = Model.Descending ? "Title Z–A" : "Title A–Z";
            Summary.Text = $"{visible.Length} matching pages · {Model.SelectedIds.Count} selected · " + (Model.Dirty ? "Unsaved local changes" : "Local sample");
            Feedback.Text = Model.Feedback;
            var pending = Model.Pending is not null;
            Decision.Visibility = pending ? Visibility.Visible : Visibility.Collapsed;
            Suite.IsEnabled = !pending;
            Search.IsEnabled = !pending; StatusFilter.IsEnabled = !pending; LanguageFilter.IsEnabled = !pending; Sort.IsEnabled = !pending;
            foreach (var row in rows.Values) { row.Selection.IsEnabled = !pending; row.Open.IsEnabled = !pending; }
            Editor.IsEnabled = !pending;
            PagesView.Visibility = Model.Section == "pages" ? Visibility.Visible : Visibility.Collapsed;
            Filters.Visibility = Model.Section == "pages" ? Visibility.Visible : Visibility.Collapsed;
            ChoicesView.Visibility = Model.Section == "pages" ? Visibility.Collapsed : Visibility.Visible;
            foreach (var item in Sidebar.Items) { item.Active = item.Id == Model.Section; ((DemoCommand)item.Command!).Refresh(); }
            Editor.Visibility = Model.Draft is null ? Visibility.Collapsed : Visibility.Visible;
            if (Model.Draft is { } draft)
            {
                if (PageTitle.Text != draft.Title) PageTitle.Text = draft.Title;
                if (Body.Text != draft.Body) Body.Text = draft.Body;
                PageStatus.Value = draft.Status;
                Media.Value = draft.MediaId;
                TitleField.Error = string.IsNullOrWhiteSpace(draft.Title) ? "Enter a title before saving the local sample." : "";
                MediaPreview.Text = EditorialDemoState.Media.Single(item => item.Id == draft.MediaId).Label;
                foreach (var (key, row) in choices)
                {
                    var selected = key.StartsWith("category:", StringComparison.Ordinal) ? draft.Categories : draft.Authors;
                    row.Update(selected, pending);
                }
                OrderChoices("category", draft.Categories, EditorialDemoState.Categories, Categories);
                OrderChoices("author", draft.Authors, EditorialDemoState.Authors, Authors);
            }
            if (Model.Section != "pages")
            {
                ChoicesView.Children.Clear();
                var values = Model.Section switch { "media" => EditorialDemoState.Media, "categories" => EditorialDemoState.Categories, "authors" => EditorialDemoState.Authors, _ => throw new InvalidOperationException("Unknown demo section.") };
                foreach (var value in values) ChoicesView.Children.Add(WorkspaceDemoUi.Text(value.Label));
            }
            foreach (var button in new[] { Sort, Save, KeepEditing, Discard }) ((DemoCommand)button.Command!).Refresh();
        }
        finally { updating = false; }
    }

    internal BleeckerCheckbox Selection(string id) => rows[id].Selection;
    private void OrderChoices(string kind, List<string> selected, IEnumerable<DemoChoice> all, StackPanel panel)
        => WorkspaceDemoUi.Reconcile(panel, selected.Concat(all.Select(item => item.Id).Where(id => !selected.Contains(id))).Select(id => (UIElement)choices[kind + ":" + id].View).ToArray());
    internal BleeckerButton OpenPage(string id) => rows[id].Open;
    internal BleeckerCheckbox Choice(string kind, string id) => choices[kind + ":" + id].Selection;
    internal BleeckerButton Move(string kind, string id, int delta) => delta == -1 ? choices[kind + ":" + id].Up : choices[kind + ":" + id].Down;

    private sealed class PageRow
    {
        private readonly DemoPage page;
        internal StackPanel View { get; } = new() { Spacing = 4 };
        internal BleeckerCheckbox Selection { get; } = new() { Content = "Select row" };
        internal BleeckerButton Open { get; }
        private readonly TextBlock metadata = WorkspaceDemoUi.Text("");
        internal PageRow(EditorialWorkspaceFixture owner, DemoPage page)
        {
            this.page = page;
            Open = WorkspaceDemoUi.Button(page.Title, () => owner.Model.Open(page.Id), BleeckerButtonVariant.Link);
            Open.HorizontalAlignment = HorizontalAlignment.Left;
            Selection.Checked += (_, _) => { if (!owner.updating) owner.Model.SetSelected(page.Id, true); };
            Selection.Unchecked += (_, _) => { if (!owner.updating) owner.Model.SetSelected(page.Id, false); };
            View.Children.Add(Selection); View.Children.Add(Open); View.Children.Add(metadata);
        }
        internal void Update(bool selected)
        {
            Selection.IsChecked = selected;
            WorkspaceDemoUi.Name(Selection, "Select fictional page: " + page.Title);
            WorkspaceDemoUi.Name(Open, "Edit fictional page: " + page.Title);
            Open.Content = page.Title;
            metadata.Text = page.Status + " · " + page.Language;
        }
    }

    private sealed class ChoiceRow
    {
        private readonly string id;
        internal Grid View { get; } = new() { ColumnSpacing = 8 };
        internal BleeckerCheckbox Selection { get; }
        internal BleeckerButton Up { get; }
        internal BleeckerButton Down { get; }
        private readonly TextBlock order = WorkspaceDemoUi.Text("");
        internal ChoiceRow(EditorialWorkspaceFixture owner, string kind, DemoChoice choice)
        {
            id = choice.Id;
            foreach (var width in new[] { new GridLength(1, GridUnitType.Star), GridLength.Auto, GridLength.Auto, GridLength.Auto }) View.ColumnDefinitions.Add(new() { Width = width });
            Selection = new() { Content = choice.Label };
            WorkspaceDemoUi.Name(Selection, "Select " + kind + ": " + choice.Label);
            Selection.Checked += (_, _) => { if (!owner.updating) owner.Model.SetChoice(kind, id, true); };
            Selection.Unchecked += (_, _) => { if (!owner.updating) owner.Model.SetChoice(kind, id, false); };
            Up = WorkspaceDemoUi.Button("↑", () => owner.Model.MoveChoice(kind, id, -1));
            Down = WorkspaceDemoUi.Button("↓", () => owner.Model.MoveChoice(kind, id, 1));
            WorkspaceDemoUi.Name(Up, "Move " + choice.Label + " earlier"); WorkspaceDemoUi.Name(Down, "Move " + choice.Label + " later");
            Grid.SetColumn(order, 1); Grid.SetColumn(Up, 2); Grid.SetColumn(Down, 3);
            View.Children.Add(Selection); View.Children.Add(order); View.Children.Add(Up); View.Children.Add(Down);
        }
        internal void Update(List<string> selected, bool pending)
        {
            var index = selected.IndexOf(id);
            Selection.IsChecked = index >= 0;
            order.Text = index < 0 ? "" : (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            Up.IsEnabled = !pending && index > 0;
            Down.IsEnabled = !pending && index >= 0 && index < selected.Count - 1;
        }
    }
}
