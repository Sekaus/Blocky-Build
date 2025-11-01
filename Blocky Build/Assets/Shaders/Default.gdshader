shader_type spatial;

render_mode blend_mix, depth_draw_opaque, depth_prepass_alpha, cull_back, diffuse_burley, specular_schlick_ggx;

uniform vec4 albedo : source_color;
uniform bool force_srgb;
uniform bool alpha;
uniform sampler2D texture_albedo : source_color, filter_nearest_mipmap, repeat_enable;
uniform ivec2 albedo_texture_size;

uniform float roughness : hint_range(0.0, 1.0);
uniform sampler2D texture_roughness : hint_roughness_r, filter_nearest_mipmap, repeat_enable;

uniform float specular : hint_range(0.0, 1.0, 0.01);
uniform float metallic : hint_range(0.0, 1.0, 0.01);
uniform sampler2D texture_metallic : hint_default_white, filter_nearest_mipmap, repeat_enable;
uniform vec4 metallic_texture_channel;

uniform sampler2D texture_emission : source_color, hint_default_black, filter_nearest_mipmap, repeat_enable;
uniform vec4 emission : source_color;
uniform float emission_energy : hint_range(0.0, 100.0, 0.01);

uniform vec3 uv1_scale;
uniform vec3 uv1_offset;
uniform vec3 uv2_scale;
uniform vec3 uv2_offset;

void vertex() {
    UV = UV * uv1_scale.xy + uv1_offset.xy;
}

void fragment() {
    vec2 base_uv = UV;

    vec4 albedo_tex = texture(texture_albedo, base_uv);
    if (force_srgb) {
        albedo_tex.rgb = mix(
            pow((albedo_tex.rgb + vec3(0.055)) * (1.0 / (1.0 + 0.055)), vec3(2.4)),
            albedo_tex.rgb.rgb * (1.0 / 12.92),
            lessThan(albedo_tex.rgb, vec3(0.04045))
        );
    }

    // Apply vertex color as lighting (like Minecraft)
    vec3 vertex_light = COLOR.rgb;

    ALBEDO = albedo_tex.rgb * vertex_light;

    float metallic_tex = dot(texture(texture_metallic, base_uv), metallic_texture_channel);
    METALLIC = metallic_tex * metallic;
    SPECULAR = specular;

    vec4 roughness_texture_channel = vec4(1.0, 0.0, 0.0, 0.0);
    float roughness_tex = dot(texture(texture_roughness, base_uv), roughness_texture_channel);
    ROUGHNESS = roughness_tex * roughness;

    if (alpha)
        ALPHA = albedo.a * albedo_tex.a;

    // Emission: add texture + color
    vec3 emission_tex = texture(texture_emission, base_uv).rgb;
    EMISSION = (emission.rgb + emission_tex) * emission_energy;
}