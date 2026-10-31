# AlertDialog

Implementation acceptance follows the pinned Bleecker source and its 56 frozen
Light/Dark layouts. `IsOpen` belongs to the caller. Cancel/Confirm request events
and optional commands must execute once, preserve its binding, and never close
the dialog themselves. Loading disables both actions, rejects Escape/backdrop
cancellation, and adds the source's manual 16px loader without marking the
confirm button as a separate busy control.

The renderer must preserve title/optional description relationships, 448px
maximum width, 16px viewport clearance, responsive 24/32px inner padding,
bounded scrolling, RTL, source labels and both variants. `SurfaceStyle` carries
the native surface customization; the frozen custom case uses 512px maximum
width and zero corner radius. Title must be supplied before opening; an empty
title remains a valid source state.

```xml
<orchard:BleeckerModalHost>
    <Grid>
        <!-- The caller sets IsOpen, and handles either requested action. -->
        <orchard:BleeckerAlertDialog Title="Fictional confirmation"
            Description="Review this local example before continuing."
            IsOpen="{x:Bind ConfirmationOpen, Mode=OneWay}"
            CancelRequested="OnCancelRequested"
            ConfirmRequested="OnConfirmRequested" />
    </Grid>
</orchard:BleeckerModalHost>
```

`CancelCommand`/`ConfirmCommand` and their parameter properties are optional.
When a command rejects `CanExecute`, its action is disabled and requests are
ignored. A valid action raises its request event, then executes the captured
command with the original parameter. Neither changes `IsOpen`; the caller may
accept the request by changing its own bound value. The public `Cancel()` and
`Confirm()` methods follow the same checks. Window-pattern Close requests Cancel.

The dialog uses the existing medium Encode Sans asset for its title and small
button labels. Declaring Medium weight against only the regular font file does
not select that asset. The retained first native run demonstrates the resulting
button-width failure; the source gate checks real label producers rather than
supplying their expected widths to the model.

The source keeps a 15px scrollbar visible. The private native chrome uses its
measured Light/Dark track and thumb colors, arrow controls and pixel-snapped
thumb. It retains the native ScrollView and scroll provider. Full scrollbar
hover/press, edge raster and physical interaction acceptance remain open.

The current background gate passes 56 native layouts, 2,664 geometry values,
the source text lines and native dialog relationships. Its six scrolling cases
also pass all 15,424 stable track/thumb pixel samples at the existing one-channel
tolerance. This does not include edge antialiasing or focused-control raster.
The action gate passes 22 Light/Dark cases and eight software native Button
invocations, with zero activation, focus or hardware input. Shared-host
regressions are retained separately. Full source raster and platform acceptance
remain open.

Loading actions use the native button presenter's disabled foreground. The owned
Cancel/Confirm captions inherit that foreground, and the custom spinner uses the
existing button-glyph foreground helper. Binding these elements to the base
button foreground left them opaque when the presenter applied disabled opacity.
The focused Light/Dark default/destructive loading check reproduced all 24
caption/spinner brush and paint failures before the repair and passes all 24
afterward, with unchanged geometry. Eleven corrupted reports and the 24 old
opaque paint regions are rejected. The full 56-case hidden dialog regression
also passes its existing geometry, scrollbar paint and no-focus guards. These
checks do not establish physical input or full platform acceptance.

Presence mounts immediately, changes to visible after 20ms, and retains inert
closing paint for 150ms. Surface and backdrop transitions use their independently
measured curves and reversal clocks. Reduced motion changes animation duration,
not the presence timers. Unload/retemplate/root changes must release timers,
composition resources and modal registrations without rewriting caller values.

The catalog fixture must expose default/destructive and loading states and keep
fictional callback counts in memory. Background native checks must compare real
rendered geometry/text/metadata with the frozen source and exercise controlled
actions, commands, property changes and teardown. They must not focus, activate
or show a window or send hardware input. Physical input, foreground paint,
scaling, motion and ARM64 runtime acceptance remain separate requirements.

## 2026-10-30 x64 milestone

The five-check x64 milestone supersedes the older full-raster and ARM64 runtime
requirements above. The exact .70 package consumer renders all 56 Light/Dark
source states, and the extracted x64 catalog verifies pointer confirmation,
Space reopening, Escape cancellation, and focus return in both themes. Native
150% layouts at 420 and 600 DIP pass in Light and Dark without clipping. With
Windows animations already disabled, the open surface reaches its reduced-motion
endpoint. The independent package consumer compiles and runs. The fictional
Monitoring and Editorial demos remain catalog fixtures. ARM64 is build-only and
its runtime remains unverified.
