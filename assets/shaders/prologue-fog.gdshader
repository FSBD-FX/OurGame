shader_type canvas_item;
render_mode unshaded;

uniform sampler2D occluders : filter_nearest, repeat_disable;
uniform sampler2D fields : filter_nearest, repeat_disable;
uniform int occluder_count = 0;
uniform int field_count = 0;
uniform bool frame_valid = false;
uniform float opacity_scale = 1.0;
uniform vec2 actor_position;
uniform vec2 lamp_position;
uniform vec2 lamp_direction;
uniform vec3 screen_to_world_x = vec3(1.0, 0.0, 0.0);
uniform vec3 screen_to_world_y = vec3(0.0, 1.0, 0.0);
uniform vec4 base_profile = vec4(180.0, 620.0, 0.55, 620.0);
uniform float base_half_angle = 22.5;

float smooth01(float value) {
    float t = clamp(value, 0.0, 1.0);
    return t * t * (3.0 - 2.0 * t);
}

float inward_weight(vec4 bounds, vec2 point, float width) {
    if (any(lessThan(point, bounds.xy)) || any(greaterThanEqual(point, bounds.zw))) return 0.0;
    vec2 edge = min(point - bounds.xy, bounds.zw - point);
    return smooth01(min(edge.x, edge.y) / width);
}

bool clip_axis(float origin, float direction, float minimum, float maximum,
        inout float entering, inout float leaving) {
    if (direction == 0.0) return origin >= minimum && origin <= maximum;
    float a = (minimum - origin) / direction;
    float b = (maximum - origin) / direction;
    entering = max(entering, min(a, b));
    leaving = min(leaving, max(a, b));
    return entering <= leaving;
}

// Only darken the visible side beside a silhouette. Exact blocked rays below
// remain fully dark: averaging offset rays would reveal rooms behind thin walls.
float corner_visibility(vec2 ray, float ray_length, vec2 corner) {
    float along = dot(ray, corner) / ray_length;
    if (along <= 0.0 || along >= ray_length) return 1.0;
    float width = min(20.0, (ray_length - along) * 0.12);
    float miss_at_point = abs(ray.x * corner.y - ray.y * corner.x) / along;
    return smooth01(miss_at_point / max(width, 0.0001));
}

float visibility(vec2 origin, vec2 point) {
    if (!frame_valid) return 0.0;
    vec2 ray = point - origin;
    float ray_length = length(ray);
    float visible = 1.0;
    for (int i = 0; i < 256; i++) {
        if (i >= occluder_count) break;
        vec4 row_x = texelFetch(occluders, ivec2(0, i), 0);
        vec4 row_y = texelFetch(occluders, ivec2(1, i), 0);
        vec2 end = texelFetch(occluders, ivec2(2, i), 0).xy;
        vec2 start = vec2(dot(row_x.xyz, vec3(origin, 1.0)), dot(row_y.xyz, vec3(origin, 1.0)));
        vec2 finish = vec2(dot(row_x.xyz, vec3(point, 1.0)), dot(row_y.xyz, vec3(point, 1.0)));
        vec2 delta = finish - start;
        float entering = 0.0;
        float leaving = 1.0;
        if (clip_axis(start.x, delta.x, row_x.w, end.x, entering, leaving) &&
                clip_axis(start.y, delta.y, row_y.w, end.y, entering, leaving)) return 0.0;
        if (ray_length <= 0.0) continue;
        // A corner can affect this ray only within the 20-world-unit feather.
        // Row L1 norms conservatively bound that disk in native local space,
        // including shear/scale. Reject distant edges before four projections.
        vec2 margin = 20.0 * vec2(abs(row_x.x) + abs(row_x.y), abs(row_y.x) + abs(row_y.y));
        entering = 0.0;
        leaving = 1.0;
        if (!(clip_axis(start.x, delta.x, row_x.w - margin.x, end.x + margin.x, entering, leaving) &&
                clip_axis(start.y, delta.y, row_y.w - margin.y, end.y + margin.y, entering, leaving))) continue;
        // Invert the uploaded native rectangle basis, not a world AABB. The
        // 20px maximum feather is in world units, including rotated/scaled walls.
        float determinant = row_x.x * row_y.y - row_x.y * row_y.x;
        vec2 world_x = vec2(row_y.y, -row_y.x) / determinant;
        vec2 world_y = vec2(-row_x.y, row_x.x) / determinant;
        vec2 near = vec2(row_x.w, row_y.w) - start;
        vec2 far = end - start;
        visible = min(visible, corner_visibility(ray, ray_length, world_x * near.x + world_y * near.y));
        visible = min(visible, corner_visibility(ray, ray_length, world_x * far.x + world_y * near.y));
        visible = min(visible, corner_visibility(ray, ray_length, world_x * far.x + world_y * far.y));
        visible = min(visible, corner_visibility(ray, ray_length, world_x * near.x + world_y * far.y));
        if (visible <= 0.0) return 0.0;
    }
    return visible;
}

float fog_opacity(vec2 world_point) {
    vec4 profile = base_profile;
    float half_angle = base_half_angle;
    for (int i = 0; i < 64; i++) {
        if (i >= field_count) break;
        vec4 bounds = texelFetch(fields, ivec2(0, i), 0);
        vec4 value = texelFetch(fields, ivec2(1, i), 0);
        vec2 cone_and_width = texelFetch(fields, ivec2(2, i), 0).xy;
        float weight = inward_weight(bounds, world_point, cone_and_width.y);
        profile = mix(profile, value, weight);
        half_angle = mix(half_angle, cone_and_width.x, weight);
    }
    if (!frame_valid) return profile.z;
    float actor_distance = length(world_point - actor_position);
    float distance_weight = smooth01((actor_distance - profile.x) / (profile.y - profile.x));
    // Both sources obey the same native-rectangle segment test. In particular,
    // the actor's near-clear disk never grants light into a hidden room.
    float opacity = profile.z;
    if (distance_weight < 1.0)
        opacity *= 1.0 - (1.0 - distance_weight) * visibility(actor_position, world_point);
    vec2 light = world_point - lamp_position;
    float light_distance = length(light);
    if (opacity <= 0.0 || profile.w <= 0.0 || light_distance >= profile.w ||
            length(lamp_direction) == 0.0) return opacity;
    float angular_weight = 1.0;
    if (light_distance > 0.0) {
        float cross_product = abs(light.x * lamp_direction.y - light.y * lamp_direction.x);
        float dot_product = dot(light, lamp_direction);
        float angle = dot_product == 0.0 ? 1.57079632679 : atan(cross_product, dot_product);
        angular_weight = 1.0 - smooth01(angle / radians(half_angle));
    }
    if (angular_weight > 0.0)
        opacity *= 1.0 - angular_weight * (1.0 - smooth01(light_distance / profile.w)) * visibility(lamp_position, world_point);
    return clamp(opacity, 0.0, profile.z);
}

void fragment() {
    vec3 screen_point = vec3(FRAGCOORD.xy, 1.0);
    vec2 world_point = vec2(dot(screen_to_world_x, screen_point), dot(screen_to_world_y, screen_point));
    COLOR = vec4(0.0, 0.0, 0.0, fog_opacity(world_point) * clamp(opacity_scale, 0.0, 1.0));
}
