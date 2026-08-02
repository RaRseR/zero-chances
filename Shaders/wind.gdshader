shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_disabled;

uniform sampler2D climate_map : filter_linear;
uniform float world_size = 100000.0;
uniform float sea_level = 0.0;

// Земля тёмно-серая. Стрелки: слабый синий -> сильный жёлтый.
uniform vec3 color_ground : source_color = vec3(0.16, 0.16, 0.18);
uniform vec3 color_weak   : source_color = vec3(0.30, 0.42, 0.58);
uniform vec3 color_strong : source_color = vec3(0.86, 0.78, 0.42);
uniform float strength_gain : hint_range(0.5, 5.0) = 1.4;

// Стрелки.
uniform float arrow_cell_size : hint_range(1000.0, 15000.0) = 3300.0;
uniform float arrow_thickness : hint_range(0.005, 0.2) = 0.045;
uniform float arrow_length    : hint_range(0.1, 0.5) = 0.34;

// Линия моря + границы.
uniform float chunk_world_size : hint_range(100.0, 100000.0) = 5000.0;
uniform float border_width     : hint_range(1.0, 500.0)      = 8.0;
uniform float dash_length      : hint_range(10.0, 1000.0)    = 300.0;
uniform vec3  border_color     : source_color                 = vec3(1.0, 1.0, 1.0);
uniform float border_opacity   : hint_range(0.0, 1.0)        = 0.2;
uniform vec3  sea_line_color   : source_color                 = vec3(0.0, 0.0, 0.0);
uniform float sea_line_pixels  : hint_range(0.5, 8.0)        = 1.0;

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


// Расстояние от точки p до отрезка a-b.
float segment_distance(vec2 p, vec2 a, vec2 b) {
	vec2 pa = p - a;
	vec2 ba = b - a;
	float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
	return length(pa - ba * h);
}


void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_height = VERTEX.y;
}


void fragment() {
	// Клетка сетки, одна стрелка в каждой.
	vec2 cell_id = floor(v_world_pos.xz / arrow_cell_size);
	vec2 cell_center = (cell_id + 0.5) * arrow_cell_size;

	// Ветер в центре клетки.
	vec2 uv = cell_center / world_size;
	vec2 wind = texture(climate_map, uv).gb * 2.0 - 1.0;
	float magnitude = length(wind);
	float strength = clamp(magnitude * strength_gain, 0.0, 1.0);

	// Локальная координата в клетке: -0.5..0.5.
	vec2 local = fract(v_world_pos.xz / arrow_cell_size) - 0.5;

	vec3 col = color_ground;

	if (magnitude > 0.01) {
		vec2 dir = wind / magnitude;
		vec2 perp = vec2(-dir.y, dir.x);

		// тело стрелки: отрезок вдоль ветра.
		vec2 tail = -dir * arrow_length;
		vec2 head =  dir * arrow_length;
		float d = segment_distance(local, tail, head);

		// наконечник: два коротких отрезка от head.
		vec2 barb_l = head - dir * (arrow_length * 0.45) + perp * (arrow_length * 0.32);
		vec2 barb_r = head - dir * (arrow_length * 0.45) - perp * (arrow_length * 0.32);
		d = min(d, segment_distance(local, head, barb_l));
		d = min(d, segment_distance(local, head, barb_r));

		if (d < arrow_thickness) {
			col = mix(color_weak, color_strong, strength);
		}
	}

	// линия моря (адаптивная).
	col = mix(col, sea_line_color, sea_line_factor(v_height));

	// границы чанков.
	if (is_chunk_border(v_world_pos)) {
		col = mix(col, border_color, border_opacity);
	}

	ALBEDO = col;
	ROUGHNESS = 1.0;
}
