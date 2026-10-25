# Original data in component content slots

Components using `BleeckerContentText` keep the caller's original native element
or model. Rich models require an explicit DataTemplate or DataTemplateSelector.
Plain scalars follow the existing source formatting rules. A template receives
the original data rather than a display string or copied view model.

The presenter installs current content before attaching its new template or
selector. Selecting while Content is still null can cache the wrong template:
the Header regression reproduced a selector receiving only null even though
the resulting editor later bound to the original model. Current setup detaches
the old renderer, installs the original data, then selects its presentation.
An unchanged data/template/selector tuple keeps its existing presenter and
caller controls, bindings, input and selection.

Header's public model/selector regression verifies both selector input identity
and the rendered editor's original model, two-way updates, live model changes
and retained editor identity after an unrelated navigation update. The same
shared path is used by Footer and other templated component slots; their
existing ownership/layout regressions remain required. This setup change does
not add alternate data or presentation fallbacks.

AppShell's header and footer wrappers also follow the original rendered root's
`Canvas.ZIndex`. This keeps a Header above the shell's opaque main region and
allows caller stacking changes to take effect. The observer follows native
controls and materialized template roots, and disconnects from replaced or
unloaded roots. Empty slots reset their inherited stacking order to zero.
Other components keep their existing slot ordering.

Popover uses the slot's measured natural width and height to detect live caller
content changes. Its presentation update runs through the dispatcher outside
`LayoutUpdated`; intrinsic surface measurement is cached until content or
presentation inputs change. This avoids repeatedly invalidating the native
layout while replacing a popup template. Rich content retains its original
native measuring and presentation path; the additional natural-height value
does not change the slot's returned desired size.

The Header/AppShell regression checks actual topmost hits above an opaque main
region, live stacking changes, templated roots, replacement, clearing and retired
producers. Native captures preserve their complete raw frame. When dark logo
filtering adds an off-screen decoder to the native render bounds, the client
viewport is extracted from measured coordinates without resampling. The audit
compares every extracted pixel with the raw frame.
