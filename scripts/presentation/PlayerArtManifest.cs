using System.Collections.Generic;

namespace Knight.Presentation;

public sealed record PlayerArtManifest(
    string CharacterId,
    string TextureResourcePath,
    int SheetWidth,
    int SheetHeight,
    IReadOnlyDictionary<string, PlayerAnimationClip> Clips);
