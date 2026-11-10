# Known minimap revision compatibility

Implements existing original-backlog P06/F04, not a new map or gameplay mechanic. KNIGHT remains Godot at G:/knight/Knight. User requests autonomous reasonable implementation unless a consequential missing choice blocks it; no new approval round, Git writes or worktree copy. Existing codex branch and dirty user work are preserved.

## Design and alternatives

Keep optional map separate from normalized core. Continue accepting exact-current maps as before. For the single physically verified v133→current revision pair, preserve only old explored bits whose geometry mapping is known. Unknown pairs still discard optional map, never core. Blind index copy is wrong because old12440/current12436 boundaries both encode1555bytes. Shipping/instantiating the entire old level is unnecessary and risks binding current external leaves. Use a compact, content-attested delta instead.

- Old revision B264EB610D985D63288925D90C9F239950A1D406AF7063E0BF5C470F8E62089E.
- Current revision 48020BD483F79CC058185DB230C17608740B5FC43309E2F783F0C7C02B45CA15 and serialized catalog SHA256 1D2D41D9191238A87CC679F04F522D09ED99D18C3374C710778364B986E18C4F.
- Both world bounds0,0,7680,4320/grid8/cells518400/solids143. Only solid48 changes: old world rectangle2856,3720,152,24; new2856,3720,136,24.
- Copy previously explored identical-world cells, clearing the entire old/new changed footprint and touching border cells (columns356–376, rows464–468). No Advance, interpolation, nearest-neighbor or new discovery.
- Drop every boundary belonging to changed solid48: old6526–6573, current6526–6569. Earlier boundary IDs unchanged; old6574–12439 map to current6570–12435. Attest this complete mapping against reconstructed old physical catalog in tests, with exact world segment and owning-solid equality, not only lengths or counts.
- Validate canonical Base64, exact old byte counts and format before conversion. New tail padding must remain0. Current target label alone is insufficient: verify full serialized target bytes, bounds/counts and revision. No migration for unknown geometry even with a copied revision label.

## Evidence and risks

Source recon: docs/production/2026-10-27-minimap-migration-recon-v001.md. Old physical scene SHA256 1F36A94604C8DD00EA486F9FB40ECA09EEF1E51539DF903D1B2E6570E84BF50B; serialized old catalog8A826994F3BC25BC03BB51C8E4B3722AABFF401C5AADBA763851CD9CF1D24FF6. Test reconstructs exact old bytes using current immutable unchanged portions plus literal changed solid/boundaries, and checks this original digest before testing mapping. Runtime carries no historical file paths/dependency.

Versioned compatibility cannot reconstruct old viewing positions/dynamic occlusion; conservatively dropping changed footprint may hide a few previously seen pixels, preferable to revealing unknown paths. Reading must leave every core field and source file byte unchanged. A later geometry version intentionally requires a new verified migration descriptor. No real-user-save reads/writes, no automatic disk rewrite, no terrain/AI/angle/energy/UI/art change, no foreground input or paid generation.
