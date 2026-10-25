using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Orchard.Catalog;

public sealed partial class FileInputsFixture : UserControl
{
    public FileInputsFixture()
    {
        InitializeComponent();
        foreach (var input in new[] { Small, Primary, Large })
        {
            input.SelectionChanged += (_, files) => Result.Text = $"Selected {files.Count} original native file(s); callback list retained: {ReferenceEquals(files,input.SelectedFiles)}";
            input.SelectionCanceled += (_, _) => Result.Text = "Selection canceled; previous files retained";
            input.PickerFailed += (_, error) => Result.Text = "File picker failed: " + error.Message;
        }
        Primary.FileTypeFilter.Add(".m3u"); Primary.FileTypeFilter.Add(".m3u8");
    }
    private void MultipleChanged(object sender, RoutedEventArgs args) { if (Primary is not null) Primary.IsMultiple = ((CheckBox)sender).IsChecked == true; }
    private void ErrorChanged(object sender, RoutedEventArgs args) { if (Primary is not null) Primary.IsError = ((CheckBox)sender).IsChecked == true; }
    private void DisabledChanged(object sender, RoutedEventArgs args) { if (Primary is not null) Primary.IsEnabled = ((CheckBox)sender).IsChecked != true; }
    private void RequiredChanged(object sender, RoutedEventArgs args) { if (Primary is not null) Primary.IsRequired = ((CheckBox)sender).IsChecked == true; }
    private void Clear(object sender, RoutedEventArgs args) { Primary.ClearSelection(); Result.Text = "Selection cleared without a SelectionChanged callback"; }
    private void ImagesOnly(object sender, RoutedEventArgs args) { Primary.FileTypeFilter.Clear(); foreach (var extension in new[] { ".png", ".jpg", ".jpeg" }) Primary.FileTypeFilter.Add(extension); Result.Text = "Native extension filter: .png, .jpg, .jpeg"; }
    private void EveryType(object sender, RoutedEventArgs args) { Primary.FileTypeFilter.Clear(); Result.Text = "Native picker allows all file types"; }
    private async void UseFictional(object sender, RoutedEventArgs args)
    {
        var folder = Path.Combine(Path.GetTempPath(), "orchard-catalog-file-input"); Directory.CreateDirectory(folder);
        var original = new List<StorageFile>();
        foreach (var (name,text) in new[] { ("fictional-note.txt", "Original fictional bytes"), ("fictional-playlist.m3u", "#EXTM3U") })
        {
            var path = Path.Combine(folder, name); await File.WriteAllTextAsync(path,text);
            original.Add(await StorageFile.GetFileFromPathAsync(path));
        }
        Primary.SetFiles(original); Result.Text = "Supplied original fictional local files silently; changing size/filter/error preserves them";
    }
}
