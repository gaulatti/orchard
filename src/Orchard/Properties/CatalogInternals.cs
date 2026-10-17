using System.Runtime.CompilerServices;

// The catalog freezes each control's clock without changing the Windows clock
// or adding a public clock configuration property.
[assembly: InternalsVisibleTo("Orchard.Catalog")]
