# Private sample: bundled third-party license sources

These notices describe bundled dependencies, not a license grant for this game's
own code or assets. This candidate is for the project owner's local quality review;
public release and any project distribution terms remain separate decisions.

## Godot 4.7.1-stable Mono Windows x64

The exact official Mono template archive was verified before extraction:

- `Godot_v4.7.1-stable_mono_export_templates.tpz`
- 1,201,759,011 bytes
- SHA-256 `ef9a708be51ecd974cd7dccdcafd7a1870da3d3e1c24c072bdbb9818c7a7db63`
- Release: <https://github.com/godotengine/godot-builds/releases/tag/4.7.1-stable>

Complete, unmodified notices downloaded from the same source tag:

- `GODOT-LICENSE.txt`: <https://raw.githubusercontent.com/godotengine/godot/4.7.1-stable/LICENSE.txt>
  SHA-256 `b0435e3b3e4e55238f05f4b306f30524a1b2e20147810d436eaa554fa6855c80`
- `GODOT-COPYRIGHT.txt`: <https://raw.githubusercontent.com/godotengine/godot/4.7.1-stable/COPYRIGHT.txt>
  SHA-256 `cb1980c88089573bcacd7221d777c689bb8bbd778799f24c27fca0fe5f774d6d`

## Microsoft.NETCore.App.Runtime.win-x64 9.0.19

`Knight.runtimeconfig.json` and `Knight.deps.json` identify the exported self-contained
runtime as 9.0.19. The local official NuGet package has repository commit
`8381bdb01fe4a26e1f61370e779c78bb73ecc95b` and declares MIT. The exported coreclr,
clrjit, hostfxr, hostpolicy, System.Private.CoreLib, GC, DiaSymReader and msquic files
were matched by SHA-256 against that package. Do not substitute the broader SDK license.

- Package: <https://www.nuget.org/packages/Microsoft.NETCore.App.Runtime.win-x64/9.0.19>
- Original nupkg SHA-256: `86bfb5edd779f99712bdc8d7160fbf2e669783312883eea42a19814968d445ce`
- `DOTNET-LICENSE.txt`: original package `LICENSE.TXT`, SHA-256
  `d7a68596ab69b06f51ca278a6545148e4269a9381c26d597c13df5d88e08cf5b`
- `DOTNET-THIRD-PARTY-NOTICES.txt`: original package `THIRD-PARTY-NOTICES.TXT`, SHA-256
  `40686c6447a7d5b5d3693068e4571b5f483d7ed335aeee773ef662440de4c5d5`

Re-check versions and notices whenever either engine/template or runtime pack changes.
No editor addons are shipped in the sample.
