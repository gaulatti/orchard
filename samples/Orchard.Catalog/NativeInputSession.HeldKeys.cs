using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Text.Json;

namespace Orchard.Catalog;

internal sealed partial class NativeInputSession
{
    private readonly List<object> heldKeyActions = [];

    internal async Task HeldKeyAsync(ushort key, Control target, Func<Control> releaseTarget, Func<Task> whileHeld,ushort? modifier=null)
    {
        Require(OwnsForeground && ReferenceEquals(FocusManager.GetFocusedElement(root), target), "Held key did not start on its focused native target.");
        var downDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var upDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var downs = 0; var ups = 0; var modifierDowns=0;var modifierUps=0;var downOwned = true; var upOwned = true; Control? expectedUp = null;
        var down = new KeyEventHandler((_, args) => { if(modifier is not null&&(ushort)args.Key==modifier){modifierDowns++;downOwned&=ReferenceEquals(args.OriginalSource,target);return;}if ((ushort)args.Key != key) return; downs++; downOwned &= ReferenceEquals(args.OriginalSource, target); downDelivered.TrySetResult(); });
        var up = new KeyEventHandler((_, args) => { if(modifier is not null&&(ushort)args.Key==modifier){modifierUps++;upOwned&=ReferenceEquals(args.OriginalSource,expectedUp);upDelivered.TrySetResult();return;}if ((ushort)args.Key != key) return; ups++; upOwned &= ReferenceEquals(args.OriginalSource, expectedUp);if(modifier is null)upDelivered.TrySetResult(); });
        root.Content.AddHandler(UIElement.KeyDownEvent, down, true); root.Content.AddHandler(UIElement.KeyUpEvent, up, true);
        var sent = false;
        try
        {
            var presses=new List<NativeInput>();if(modifier is {} mod)presses.Add(new NativeInput{Type=1,Data=new InputData{Keyboard=new KeyboardInput{Key=mod}}});presses.Add(new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = key } } });Send(presses.ToArray()); sent = true;
            await downDelivered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await whileHeld();
        }
        finally
        {
            try
            {
                if (sent)
                {
                    var focusedBeforeRelease = false;
                    try { expectedUp = releaseTarget(); focusedBeforeRelease = OwnsForeground && ReferenceEquals(FocusManager.GetFocusedElement(root), expectedUp); }
                    finally { var releases=new List<NativeInput>{new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = key, Flags = 2 } } }};if(modifier is {} mod)releases.Add(new NativeInput{Type=1,Data=new InputData{Keyboard=new KeyboardInput{Key=mod,Flags=2}}});Send(releases.ToArray()); }
                    await upDelivered.Task.WaitAsync(TimeSpan.FromSeconds(2));
                    Require(focusedBeforeRelease, "Held key lost its explicit release target.");
                    Require(downs == 1 && ups == 1 && downOwned && upOwned, "Held native key did not retain its exact down/up owners.");
                    Require(modifier is null||modifierDowns==1&&modifierUps==1,"Held native modifier did not retain one exact press/release.");
                }
            }
            finally
            {
                root.Content.RemoveHandler(UIElement.KeyDownEvent, down); root.Content.RemoveHandler(UIElement.KeyUpEvent, up);
                heldKeyActions.Add(new { key, downTarget = target.Name, upTarget = expectedUp?.Name, downs, ups, downOwned, upOwned, modifier,modifierDowns,modifierUps,focusTransferred = !ReferenceEquals(target, expectedUp) });
                await File.WriteAllTextAsync(Path.Combine(folder, $"{prefix}-held-keys.json"), JsonSerializer.Serialize(heldKeyActions, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }
}
