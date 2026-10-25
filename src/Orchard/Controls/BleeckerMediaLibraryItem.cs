using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>Original caller-owned media metadata and native image decoders.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class BleeckerMediaLibraryItem : DependencyObject
{
    public BleeckerMediaLibraryItem() { }
    public BleeckerMediaLibraryItem(string id, string filename) { Id = id; Filename = filename; }
    internal event EventHandler? Changed;
    private static DependencyProperty Property(string name, Type type, object? value = null) => DependencyProperty.Register(name, type, typeof(BleeckerMediaLibraryItem), new PropertyMetadata(value, (owner, _) => ((BleeckerMediaLibraryItem)owner).Changed?.Invoke(owner, EventArgs.Empty)));
    public static readonly DependencyProperty IdProperty = Property(nameof(Id), typeof(string), "");
    public static readonly DependencyProperty FilenameProperty = Property(nameof(Filename), typeof(string), "");
    public static readonly DependencyProperty AltProperty = Property(nameof(Alt), typeof(string));
    public static readonly DependencyProperty UrlProperty = Property(nameof(Url), typeof(string));
    public static readonly DependencyProperty CreatedAtProperty = Property(nameof(CreatedAt), typeof(string));
    public static readonly DependencyProperty ThumbnailSourceProperty = Property(nameof(ThumbnailSource), typeof(ImageSource));
    public static readonly DependencyProperty ImageSourceProperty = Property(nameof(ImageSource), typeof(ImageSource));
    public string Id { get => (string)GetValue(IdProperty); set => SetValue(IdProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string Filename { get => (string)GetValue(FilenameProperty); set => SetValue(FilenameProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string? Alt { get => (string?)GetValue(AltProperty); set => SetValue(AltProperty, value); }
    /// <summary>Original full URL for visible text, external navigation and copying. It is never reconstructed or normalized.</summary>
    public string? Url { get => (string?)GetValue(UrlProperty); set => SetValue(UrlProperty, value); }
    public string? CreatedAt { get => (string?)GetValue(CreatedAtProperty); set => SetValue(CreatedAtProperty, value); }
    /// <summary>Caller-owned thumbnail decoder. A decoding failure never switches to ImageSource.</summary>
    public ImageSource? ThumbnailSource { get => (ImageSource?)GetValue(ThumbnailSourceProperty); set => SetValue(ThumbnailSourceProperty, value); }
    /// <summary>Caller-owned full image decoder used by the preview and by tiles without a thumbnail.</summary>
    public ImageSource? ImageSource { get => (ImageSource?)GetValue(ImageSourceProperty); set => SetValue(ImageSourceProperty, value); }
}

public sealed class BleeckerMediaLibraryItems : ObservableCollection<BleeckerMediaLibraryItem>
{
    public BleeckerMediaLibraryItems() { }
    public BleeckerMediaLibraryItems(IEnumerable<BleeckerMediaLibraryItem> items) : base(items) { }
}
