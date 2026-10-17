# CSS color transitions

Native CSS-style transitions preserve the source color space. Legacy `rgb()`
and hex endpoints interpolate in encoded sRGB (`BleeckerSrgbColorMix`). An
explicit `color-mix(in oklab, ...)` endpoint promotes a mixed transition to
premultiplied Oklab (`BleeckerColorMix`). `BleeckerCssColor` carries this
distinction through interruptions and returns the exact target's space when
the transition finishes. Radio Group uses RGB for selected ring strokes and
Oklab for its translucent off/hover strokes. Its selected Light/Dark theme
transition therefore follows sRGB; selection and off/hover changes follow Oklab.

For source parity, sRGB/Oklab conversion follows Chromium's route through
Skia's ICC 16.16 sRGB-to-XYZ D50 matrix, Bradford adaptation to D65, and the
Oklab LMS transform. The combined coefficients in `BleeckerColorMix` are the
product of those matrices; decoding uses their inverse. The common direct
sRGB approximation differs in intermediate values and can cross an 8-bit
rounding boundary. The source references are
[Skia's named sRGB gamut](https://raw.githubusercontent.com/google/skia/main/include/core/SkColorSpace.h),
[Chromium's color conversions](https://raw.githubusercontent.com/chromium/chromium/main/ui/gfx/color_conversions.cc)
and [Bradford adaptation](https://skia.googlesource.com/skia.git/+/5a0ed89aea287b4b4fdd9ef968ea6e100f067456/modules/skcms/skcms.cc).
No external color-management dependency is added.

The Radio motion reference freezes 52 browser timelines. Regression tests
compare 156 enabled-motion samples with exact RGB bytes, fractional alpha at
0.000001, and scalar values at the browser's serialized precision. Four
source-derived off/hover colors additionally verify premultiplied Oklab
coordinates and exact RGB round trips. Native captures independently read
actual ring brushes, dot transforms, paint timestamps and clocks; whole-client
pixel and animated-raster acceptance remains separate.

These changes affect native rendering and local verification. They add no
backend, configuration, service, database or metrics surface.
