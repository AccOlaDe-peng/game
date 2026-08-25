// Element VFX shader — Project Catalyst shared element material (ART_STYLE_GUIDE § ElementVFXShader).
// One shader, parameter differences express every element: Fire, Water, Wind, Earth, Lightning.
shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;

uniform vec4 element_color : source_color = vec4(1.0, 0.24, 0.10, 1.0);
uniform vec4 secondary_color : source_color = vec4(1.0, 0.70, 0.25, 1.0);
uniform float glow_strength : hint_range(0.0, 4.0) = 1.5;
uniform float noise_scale : hint_range(0.0, 8.0) = 2.0;
uniform float noise_speed : hint_range(0.0, 4.0) = 0.6;
uniform float pulse_speed : hint_range(0.0, 8.0) = 2.0;
uniform float distortion_strength : hint_range(0.0, 1.0) = 0.0;
uniform float dissolve_amount : hint_range(0.0, 1.0) = 0.0;
uniform float edge_strength : hint_range(0.0, 4.0) = 1.0;
uniform float opacity : hint_range(0.0, 1.0) = 1.0;
uniform sampler2D noise_texture : repeat_disable;

void fragment() {
    float pulse = 0.5 + 0.5 * sin(TIME * pulse_speed);

    // Sample the optional noise field; an unset texture reads as 0, which keeps a stable 0.5.
    vec2 noise_uv = UV * noise_scale + vec2(TIME * noise_speed, -TIME * noise_speed * 0.6);
    vec3 noise = texture(noise_texture, noise_uv).rgb;
    float n = noise.r * 0.5 + 0.5;

    // Dissolve: cut the sprite away from low-noise regions when dissolve_amount rises.
    float dissolve = 1.0;
    if (dissolve_amount > 0.001) {
        dissolve = smoothstep(dissolve_amount, min(dissolve_amount + 0.15, 1.0), n);
    }

    // Edge glow: bright rim on the silhouette that falls off toward the centre.
    vec2 to_edge = min(UV, 1.0 - UV);
    float edge = 1.0 - clamp(min(to_edge.x, to_edge.y) * (2.0 * edge_strength), 0.0, 1.0);

    vec3 base = mix(element_color.rgb, secondary_color.rgb, n);
    vec3 color = base + edge * base * edge_strength * 0.5;

    ALBEDO = color;
    EMISSION = color * glow_strength * (0.75 + 0.25 * pulse);
    ALPHA = opacity * dissolve;
}
