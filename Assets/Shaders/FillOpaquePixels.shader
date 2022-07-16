shader_type canvas_item;

uniform vec4 u_color;

void fragment()
{
	vec4 c = texture(TEXTURE, UV);
	COLOR = u_color * c.a;
}