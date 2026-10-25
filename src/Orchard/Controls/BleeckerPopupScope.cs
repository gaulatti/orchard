using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

// Logical containment survives both native and shared-plane portals. The
// source FocusScope stack pauses older traps while the newest active trap owns
// input; it does not rewrite caller enabled/focus properties.
internal static class BleeckerPopupScope
{
    private sealed class RootScopes
    {
        internal readonly List<Registration> Items = [];
        internal readonly List<Action> Observers = [];
    }
    private static readonly ConditionalWeakTable<XamlRoot, RootScopes> roots = new();
    internal sealed class Registration(XamlRoot root, FrameworkElement declaration, FrameworkElement surface, bool trapsFocus, Func<bool> enabled, Action? changed) : IDisposable
    {
        internal XamlRoot Root { get; } = root;
        internal FrameworkElement Declaration { get; } = declaration;
        internal FrameworkElement Surface { get; } = surface;
        internal bool TrapsFocus { get; } = trapsFocus;
        internal Func<bool> Enabled { get; } = enabled;
        internal Action? Changed { get; } = changed;
        public void Dispose() { if (roots.TryGetValue(Root, out var scopes) && scopes.Items.Remove(this)) Notify(scopes); }
    }
    private sealed class Observation(RootScopes scopes, Action changed) : IDisposable
    {
        public void Dispose() => scopes.Observers.Remove(changed);
    }
    internal static IDisposable Observe(XamlRoot root, Action changed)
    {
        var scopes = roots.GetOrCreateValue(root); scopes.Observers.Add(changed); return new Observation(scopes, changed);
    }
    private static void Notify(RootScopes scopes)
    {
        foreach (var item in scopes.Items.ToArray()) if (scopes.Items.Contains(item)) item.Changed?.Invoke();
        foreach (var observer in scopes.Observers.ToArray()) if (scopes.Observers.Contains(observer)) observer();
    }
    internal static Registration Attach(FrameworkElement declaration, FrameworkElement surface, bool trapsFocus, Func<bool> enabled, Action? changed = null)
    {
        var root = declaration.XamlRoot ?? throw new InvalidOperationException("A popup scope requires the declaration's actual XamlRoot.");
        if (surface.XamlRoot is { } other && !ReferenceEquals(root, other)) throw new InvalidOperationException("Popup declaration and surface belong to different roots.");
        var scopes = roots.GetOrCreateValue(root);
        if (scopes.Items.Any(r => ReferenceEquals(r.Surface, surface))) throw new InvalidOperationException("The popup scope is already attached.");
        var result = new Registration(root, declaration, surface, trapsFocus, enabled, changed); scopes.Items.Add(result); Notify(scopes); return result;
    }
    internal static bool Contains(XamlRoot root, DependencyObject declarationRoot, DependencyObject? target)
    {
        return target is not null && ContentRoots(root, declarationRoot).Any(parent => Within(target, parent));
    }
    private static IReadOnlyList<DependencyObject> ContentRoots(XamlRoot root, DependencyObject declarationRoot)
    {
        var allowed = new List<DependencyObject> { declarationRoot };
        if (roots.TryGetValue(root, out var scopes))
        {
            bool changed;
            do { changed = false; foreach (var scope in scopes.Items) if (!allowed.Contains(scope.Surface) && allowed.Any(parent => Within(scope.Declaration, parent))) { allowed.Add(scope.Surface); changed = true; } } while (changed);
        }
        return allowed;
    }
    private static Registration? ActiveTrap(XamlRoot root) => roots.TryGetValue(root, out var scopes) ? scopes.Items.LastOrDefault(r => r.TrapsFocus && r.Enabled()) : null;
    internal static IReadOnlyList<DependencyObject>? ActiveContentRoots(XamlRoot root) => ActiveTrap(root) is { } trap ? ContentRoots(root, trap.Surface) : null;
    internal static bool IsActiveTrap(Registration scope) => ReferenceEquals(ActiveTrap(scope.Root), scope);
    internal static bool AllowsInput(XamlRoot? root, DependencyObject declaration) => root is null || ActiveTrap(root) is not { } trap || Contains(root, trap.Surface, declaration);
    private static bool Within(DependencyObject? node, DependencyObject ancestor)
    {
        for (; node is not null; node = node is TextElement text ? text.ElementStart.Parent : VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, ancestor)) return true;
        return false;
    }
}
