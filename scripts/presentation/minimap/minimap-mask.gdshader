shader_type canvas_item;
render_mode unshaded;
uniform sampler2D known_mask : filter_nearest, repeat_disable;
uniform vec2 view_offset;
uniform vec2 world_size = vec2(1280.0, 836.923);
uniform vec2 logical_size = vec2(260.0, 170.0);
uniform vec4 space_color : source_color = vec4(.12,.18,.19,1.0);
uniform vec4 boundary_color : source_color = vec4(.65,.75,.73,1.0);
uniform bool boundary_pass = false;
uniform float outer_feather = 12.0;
varying vec2 cache_point;
void vertex() { cache_point = VERTEX; }
void fragment() {
    vec2 normalized = boundary_pass ? (cache_point - view_offset) / world_size : UV;
    // Presentation-only ellipse: configured feather is measured along the
    // short semi-axis (24px vertically, about36.7px horizontally at260x170).
    // This changes no exploration authority.
    float ellipse_depth = (1.0 - length(normalized * 2.0 - vec2(1.0))) * min(logical_size.x, logical_size.y) * .5;
    float alpha = smoothstep(0.0, outer_feather, ellipse_depth);
    if (boundary_pass) {
        // fwidth is the complete parallelogram projection of one final output pixel.
        // Shrink both ends AND the free-side stroke; centre-only coverage is forbidden.
        vec2 half_footprint = .501 * (abs(dFdx(UV)) + abs(dFdy(UV)));
        if (any(lessThan(UV - half_footprint, vec2(0.0))) ||
            any(greaterThan(UV + half_footprint, vec2(1.0))) ||
            any(lessThan(normalized, vec2(0.0))) || any(greaterThan(normalized, vec2(1.0)))) discard;
        COLOR = vec4(boundary_color.rgb, alpha);
    } else {
        vec2 point = (view_offset + UV * world_size) / 8.0;
        vec2 half_footprint = .501 * (abs(dFdx(point)) + abs(dFdy(point)));
        ivec2 first = ivec2(floor(point - half_footprint));
        ivec2 last = ivec2(floor(point + half_footprint));
        bool allowed = all(greaterThanEqual(first, ivec2(0))) && all(lessThan(last, textureSize(known_mask,0))) &&
            all(lessThanEqual(last-first, ivec2(11)));
        // Fixed work ceiling144 nearest reads even under severe layout minification.
        // Over-ceiling footprints stay black, never use the centre as a fallback.
        for (int y=0; y<12; y++) { for (int x=0; x<12; x++) {
            ivec2 cell = first + ivec2(x,y);
            if (cell.x <= last.x && cell.y <= last.y && allowed &&
                texelFetch(known_mask, cell, 0).r < .5) allowed = false;
        }}
        COLOR = vec4(allowed ? space_color.rgb : vec3(0.0), alpha);
    }
}
