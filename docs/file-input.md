# File Input

Work in progress against Bleecker commit `a86059013cd59684ce452849278ea81e734f2066`,
`src/components/file-input.tsx` (SHA-256
`987ba3a36f5419069e7c3471e309d26501b15f45e8706bb529ee9b63020892ad`).
This entry is a reusable file-selection component. It reads no remote service.

## Acceptance contract

The pinned component wraps the native file input. Preserve its three sizes,
normal/error borders, disabled opacity and cursor, light/dark tokens, focus ring,
hovered file button, forwarded input attributes, label association and original
selected file metadata. Native selection results must reach the caller's change
handler. Programmatic `value = ''` and form reset clear the selection without
inventing input/change notifications. Property changes retain the original input and
selection; unmount/remount must release that selection. Required validity and form
reset are native input behavior. `accept` is a chooser hint, not an upload
validation policy. `multiple` controls the chooser; changing it must not silently
discard an existing native selection.

Freeze unmodified pinned source bytes, installed runtime and bundle inputs,
browser geometry, computed host/file-button styles, screenshots and retained
selection/event histories. Cover the size/error/disabled matrix, focus/hover,
selected and empty states, long and Unicode filenames, multiple and zero-byte
files, constrained widths, RTL, text scaling, reduced motion and caller styles.
The independent auditor must reject missing/corrupted source, images, state
coverage, event/selection history and theme/style data.

The Windows control must connect its public selection API to the actual native
picker with the owning window, preserve original selected objects, publish
selection/cancellation/error outcomes, support clear/reset and live property
changes, expose normal automation metadata and release subscriptions on unload.
Do not invent extension/MIME fallback mappings or upload validation. Investigate
the pinned Windows App SDK contract before choosing its filter adaptation.
The native system picker, physical keyboard/pointer operation and native ARM64
runtime require separately authorized desktop evidence. Hidden probes may verify
layout and supplied selection results; they must not be reported as proof of
those interactions.

Completion also requires a registered catalog fixture, both themes and edge
cases, source-backed boundary regressions, x64/ARM64 builds, a fresh independent
NuGet consumer and runnable catalog verification. Existing Monitoring and
Editorial demonstrations remain required.

## Current evidence

`tests/Reference/FileInput.json` freezes 68 settled screenshots and 22 immediate
selection/event histories. SHA-256:
`bc824df8aef0b200cc74bc96a1aac183fe8e75e028f171f8c6abee633b2c7b8b`.
Six pinned dependencies, all 122 original TS/TSX inputs used by Tailwind scanning,
15 actual JS bundle inputs, installed React 19.2.4, Tailwind 4.2.2, esbuild
0.25.12 and original font bytes are bound to the receipt. Source bytes match the
pinned bare Git repository. No network requests occurred.

The independent auditor checks 272 geometry values, 2,992 style values, 194
selection values and 274 exact RGBA probes across 24,480,000 captured pixels.
It rejects 83 counterexamples, including original source corruption, lost
callbacks/identity, selection truncation, changed required validity, invalid
geometry/styles, corrupted PNGs and a substituted theme PNG with a recomputed
hash. Five C# reference regressions pass; the complete portable suite passes
431 tests with none failed or skipped. An initial test compilation error was
corrected before that successful run.

A second fresh browser process reproduced every settled style and all 68 PNGs
byte for byte. The 22 selection/event histories match after explicitly excluding
new injected `File.lastModified` timestamps and transient computed paint at
immediate event boundaries. The original timestamps and transient paint remain
in both receipts; neither screenshot nor source data is rewritten. Timestamps
must be positive integers and match the exact file passed to each callback.
This is semantic history reproducibility, not equality of in-flight animation
samples.

The chooser is intercepted inside headless Chromium and supplied fictional file
payloads. Its resulting input/change events are untrusted test injections.
The evidence proves the source wrapper's selection/callback boundary, not a
Windows system dialog, physical input, native picker filtering or cancellation.

The source small/medium/large heights are 54/58/66 CSS pixels. At 200% root text
size, medium height is 114 pixels; the outer literal 7px radius stays fixed while
the button's rem radius doubles. Caller typography/padding remain forwarded.
The error flag changes the border without implicitly setting `aria-invalid`.
Reduced motion shortens the host transition to 0.01ms; the file-button pseudo
element retains its source 190ms transition. These are frozen source facts;
Windows adaptations still require explicit component/runtime verification.

Windows App SDK is pinned to package 2.5.1, including WinUI 2.3.9 and Foundation
2.3.12. Microsoft's [picker guide](https://learn.microsoft.com/en-us/windows/apps/develop/files/using-file-folder-pickers)
documents owner-window association and extension filters. The newer
[FileOpenPicker](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers.fileopenpicker?view=windows-app-sdk-2.0)
returns lightweight path results. The native implementation must inspect the
installed metadata and choose its original-file and extension/MIME contract
before exposing an API. No implicit MIME-to-extension list has been introduced.

## Native API draft

`BleeckerFileInput` is a Windows button-based file-input control. It provides
`InputSize`, `IsError`, `IsMultiple`, `IsRequired`, `IsSelectionMissing`, original
`SelectedFiles`, and a native `FileTypeFilter` collection. Standard control fonts,
padding, brushes, corners, language, RTL, enabled state and templates remain
consumer customization points. `FontScale` scales the source rem recipe without
changing Windows settings; an explicit caller `FontSize` overrides the default
size binding. Text/spacing scale independently of the source's literal outer
7px radius. Text labels and the multiple-selection format are localizable.

The control deliberately uses the documented WinRT
`Windows.Storage.Pickers.FileOpenPicker` to return original `StorageFile` objects
with their native metadata. It associates the picker with the owning WinUI
window through `InitializeWithWindow` before selection. It reads no file content.
Windows extension filters replace the browser's MIME/extension `accept` syntax;
consumers supply their intended extensions explicitly. An empty collection
selects the native all-files filter. No MIME mapping or upload validation is
invented. WinRT picker restrictions, including elevated-process restrictions,
still apply; runtime/system-dialog acceptance remains open.

An optional `Picker` delegate supports a host-owned native picker integration.
It receives the same already owner-associated picker and the requested multiple
mode after native extension configuration. Returning original files, cancellation
or an exception follows the same selection/lifecycle path as the default picker.
It does not bypass disabled, detached, overlap or filter checks. The sealed public
control is used directly in background fixtures with this integration; no test
subclass or replacement rendering control supplies the visual evidence.

`PickFilesAsync()` returns the original picker result. Successful results update
selection and raise `SelectionChanged` with the same retained list used by the
control. Cancellation retains previous files and raises `SelectionCanceled`.
`SetFiles()` and `ClearSelection()` update bindings/automation values silently,
without a `SelectionChanged` event. Changing size, filters, error, multiple or
disabled state never truncates existing files. The selection list is read-only;
its file objects retain identity.

Disabled, detached and overlapping picker calls are rejected. Picker errors are
available as `LastPickerError` and `PickerFailed`; an awaited call also throws.
For the built-in button action, a `PickerFailed` handler owns error presentation;
otherwise the exception remains visible. Detaching an input silently clears its
selection and retires late callbacks. A caller awaiting an already-running
native operation still receives its result; the control does not pretend that
unloading cancels an OS dialog.

The automation peer retains native button invocation and adds a read-only value
of original filenames, required metadata and selection validity. Caller supplied
names and label associations remain native automation properties. Narrator and
OS setting changes are excluded from this goal.

The evidence in the following paragraphs describes the original File Input
preview with library identity `4575c2f0e0efb146ddb3365655d8ba771583a5bad71a6348bc1bda1647234657`.
Its test counts and older shared-demo runs retain that source/binary scope.

`FileInputsFixture` is registered in the catalog with all three sizes,
error/disabled/required, constrained width, 200% source scaling, caller typography,
live filter/multiple changes, silent clearing and optional fictional local files.
No picker opens until its button is invoked. Both full solution architectures
compile without warnings; 434 portable tests pass. The current hidden native
matrix passes 68 states and 30 behavior checks, including empty multiple-result
cancellation, native invocation/error delivery, overlapping calls, original-file
identity, caller style/local-value precedence, defaults restored on measure,
custom templates and retirement of late unloaded results. Both actual catalog
themes have eight rendered variants with the source heights/font sizes. Native
auditor counterexamples rejected: 64. Both fictional demos pass the current
library's shared-host regression.

These checks cover 748 geometry values, 340 exact source brush values and 70
native PNGs. Strict source component-crop image equality is still **failed**:
411,684 differing pixels, maximum channel difference 227, zero identical frames;
the zero threshold is unchanged. Native system-picker interaction, physical
input/focus, complete visual/motion and native ARM64 runtime remain unverified.
No File Input acceptance is claimed. A new local preview package's 153 themes,
154 loose XAML files, PRI, six fonts, two images, AnyCPU IL and current README are
verified. Both isolated consumer architectures build; the fresh x64 NuGet host
and extracted self-contained catalog each pass the same 68 states/30 checks.
The source, NuGet and archived hosts use library SHA-256
`4575c2f0e0efb146ddb3365655d8ba771583a5bad71a6348bc1bda1647234657`.
The extracted catalog has 1,348 byte-verified files and passes both fictional
demos. Its 68 frames match the source byte for byte. A same-package replay also
matches all 68 package frames byte for byte. Source/package state and contract
data match, while their image comparison remains **failed** at seven pixels
(maximum channel difference 14, 66/68 PNG pairs identical), in dark scaled and
caller-styled frames. The threshold remains zero; these results do not establish
browser/native image equality. Source and package auditors reject 64 corrupted
reports/files each. All 10 Windows text tests pass as well.
The previous Hero Carousel v5 package is historical for these newer sources.
The repository wiki is unavailable, so the contract is maintained here.

## Implicit caller styles

Ancestor implicit `BleeckerFileInput` styles can supply `FontSize` and `Padding`,
including `BasedOn` styles. These override the size/font-scale defaults just as
explicit caller styles do. The standard library style continues to use its
size-dependent defaults. Changing `InputSize` or `FontScale` preserves the caller
style and the original selected-file objects/list. Explicit styles take
precedence over ancestor implicit styles through normal WinUI resource lookup.

The catalog adds an implicitly styled attachment example. The implicit-style
preview (library `758d2398…`) source, exact NuGet consumer and catalog archive each pass 68 existing visual
states and 34 selection/lifecycle/style contracts. New cases cover implicit font
and padding across all three sizes, both font scales and both text directions.
Auditors reject 69 damaged reports/files each. All 34 native states/images reached
before the preceding package's implicit-style failure remain unchanged.
Browser/native strict raster diagnostics retain their actual failures and do not
establish component acceptance. At that earlier checkpoint, system-picker
interaction, physical input, native ARM64 execution, scaling and complete visual
acceptance were still open.

## Foreground picker checkpoint

With renewed desktop-test authorization, the x64 catalog containing the exact
`0.1.0-text-media-v4-local` Orchard DLL (`1ac7e3d5…`) passed the focused
[foreground picker check](evidence/file-input-desktop-v1/checkpoint.json) twice.
A physical pointer click opened a separate Windows `Open` dialog whose owner
handle matched the catalog window. Escape canceled an empty selection. The same
dialog returned the tracked fictional `fictional-playlist.m3u` file; the catalog
observed one original native file, retained the callback list and exposed the
filename through the File Input automation value. The test sets the native
dialog's filename field to that tracked path and confirms it with Enter; it does
not claim manual file browsing. Tab moved focus from the small
input to the primary input; Space opened another owner-associated dialog, and
Escape retained the prior selected file. The catalog's Clear action emptied the
automation value. The test closed its catalog process and changed no OS settings.
It uses the same Orchard DLL SHA-256 as the v4 independent NuGet consumer, but
the foreground dialog was tested in the catalog host, not the consumer host.

Run the check on an available, unlocked desktop with the exact catalog app:

```powershell
./scripts/verify-file-input-desktop.ps1 -AppDirectory artifacts/current/text-media-v4-catalog-x64-app -Output artifacts/current/file-input-desktop-new -ExpectedLibrarySha256 1ac7e3d5ac2095e7a26db55ceb724dcc054ef2580dbda70173ebd88c90859f6b
```

The first three v1 script-development runs failed on premature dialog/UI
Automation sampling; their reports remain with the two passing runs. The
expanded check below adds the multiple-selection and filter path. The existing
hidden selection tests retain their own scope.

## Foreground multiple-selection and filter checkpoint

The same unchanged v4 catalog DLL passed two runs of the expanded
[desktop check](evidence/file-input-multiple-desktop-v1/checkpoint.json).
With `IsMultiple` enabled and the catalog's `.m3u`/`.m3u8` extensions, the
real Windows dialog showed both tracked fictional playlists and hid the tracked
`.txt` sentinel. Native Ctrl-click selected the two visible files, and the
catalog received two original native objects with the expected automation value.
Changing the filter to all files kept the current two-file selection; the next
dialog exposed the `.txt` sentinel alongside both playlists. Choosing it
returned the original note. Turning multiple selection off did not discard that
selection. All five dialogs in the run belonged to the catalog window, and the
catalog process exited. One earlier test-harness run failed because it clicked
the blank center of a system-dialog row; the corrected name-area click passes
twice. The exact package and failed/passing receipts remain pinned.

This establishes those real x64 catalog-host paths, not every filter or picker
variant, the full pointer/keyboard matrix, display/text scaling, native ARM64
runtime or complete visual acceptance. The Orchard control and package bytes
are unchanged.
