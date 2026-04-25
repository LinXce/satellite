shader_type canvas_item;
render_mode unshaded;

uniform float side = 1.0;
uniform float fade_band = 20.0;

void fragment() {
	float mask = smoothstep(-fade_band, fade_band, VERTEX.y * side);
	if (mask <= 0.001) {
		discard;
	}
	COLOR.a *= mask;
}
