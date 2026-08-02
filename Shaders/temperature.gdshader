shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_disabled;

uniform sampler2D climate_map : filter_linear;
uniform float world_size = 100000.0;
uniform float sea_level = 0.0;

uniform float chunk_world_size : hint_range(100.0, 100000.0) = 5000.0;
uniform float border_width     : hint_range(1.0, 500.0)      = 8.0;
uniform float dash_length      : hint_range(10.0, 1000.0)    = 300.0;
uniform vec3  border_color     : source_color                 = vec3(1.0, 1.0, 1.0);
uniform float border_opacity   : hint_range(0.0, 1.0)        = 0.2;
uniform vec3  sea_line_color   : source_color                 = vec3(0.1, 0.1, 0.1);
uniform float sea_line_pixels  : hint_range(0.5, 8.0)        = 0.5;

varying vec3  v_world_pos;
varying float v_height;


bool is_chunk_border(vec3 world_pos) {
	vec2 xz = world_pos.xz;
	vec2 in_chunk = mod(xz, chunk_world_size);
	vec2 dist_edge = min(in_chunk, chunk_world_size - in_chunk);
	if (min(dist_edge.x, dist_edge.y) > border_width) return false;
	float along = (dist_edge.x < dist_edge.y) ? xz.y : xz.x;
	return mod(along, dash_length * 2.0) < dash_length;
}


float sea_line_factor(float height) {
	float edge = fwidth(height) * sea_line_pixels;
	return 1.0 - smoothstep(0.0, edge, abs(height - sea_level));
}


void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_height = VERTEX.y;
}


void fragment() {
	vec2 uv = v_world_pos.xz / world_size;
	float temperature = texture(climate_map, uv).r;

	vec3 col = vec3(temperature, 0.3, 1.0 - temperature);

	// вода темнее.
	if (v_height < sea_level) {
		col *= 0.5;
	}

	// линия уровня моря (адаптивная).
	col = mix(col, sea_line_color, sea_line_factor(v_height));

	// границы чанков.
	if (is_chunk_border(v_world_pos)) {
		col = mix(col, border_color, border_opacity);
	}

	ALBEDO = col;
	ROUGHNESS = 1.0;
}
