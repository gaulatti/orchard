# Bundled fonts

- Encode Sans: `thundernixon/Encode-Sans` commit
  `6407de854a4dc3bfbe2160a11c5b57f5a1baf3bc`, `fonts/EncodeSans/static`.
- Libre Franklin: `googlefonts/Libre-Franklin` commit
  `310b7eef1f7b7ba82322295e0034675ea68040b0`, `fonts/ttf`.

The catalog bundles Regular and SemiBold faces from each family, plus Encode Sans
Medium for Bleecker's weight-500 controls. The Medium face comes from the same
pinned Encode Sans commit; its OS/2 weight class is 500 and its SHA-256 is
`40a222af6b6dcd6940866415df454f2e27e798f5dc16539d72c67449c0eeca98`.
Encode Sans Bold supplies App Tab Bar's weight-700 notification text. Its source
is the pinned Sabella resource (same Encode Sans family), OS/2 weight class 700,
SHA-256 `b6f0d04888140d10006fbeeebc194d33f28fdb50ece52ff16f6e4f952d2ccc85`.
The package contains six font files. Regular and SemiBold are the same assets used
by Sabella. The adjacent `OFL-*.txt` files
contain each family’s SIL Open Font License. Retain them when redistributing
the font assets.
