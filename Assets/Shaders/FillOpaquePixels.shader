shader_type canvas_item;

uniform vec4 u_color : hint_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment()
{
	vec4 c = texture(TEXTURE, UV);
	COLOR = u_color * c.a;
}