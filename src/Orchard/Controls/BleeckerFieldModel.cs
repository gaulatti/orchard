namespace Orchard;

internal static class BleeckerFieldModel
{
    internal static bool Numeric(object? value) => BleeckerReactContent.Numeric(value);
    internal static bool Present(object? value) => BleeckerReactContent.Present(value);
    internal static object? Display(object? value) => BleeckerReactContent.Display(value);
    internal readonly record struct Flow(double ControlY, double DescriptionY, double ErrorY, double Height);
    internal static Flow Place(double header, double control, bool description, double descriptionHeight, bool error, double errorHeight)
    {
        var controlY = header + 10; var cursor = controlY + control;
        var descriptionY = cursor + 10;
        if (description && descriptionHeight > 0) cursor = descriptionY + descriptionHeight;
        var errorY = cursor + 10;
        if (error && errorHeight > 0) cursor = errorY + errorHeight;
        return new(controlY, descriptionY, errorY, cursor);
    }
}
