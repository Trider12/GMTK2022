shader_type canvas_item;

uniform sampler2D noiseTexture;

float noise(vec2 p, float time)
{
	float s = texture(noiseTexture,vec2(1.,2.*cos(time))*time*8. + p*1.).x;
	s *= s;
	return s;
}

float onOff(float a, float b, float c, float time)
{
	return step(c, sin(time + a*cos(time*b)));
}

float ramp(float y, float start, float end)
{
	float inside = step(start,y) - step(end,y);
	float fact = (y-start)/(end-start)*inside;
	return (1.-fact) * inside;
}

float stripes(vec2 uv, float time)
{
	float noi = noise(uv*vec2(0.5,1.) + vec2(1.,3.), time);
	return ramp(mod(uv.y*4. + time/2.+sin(time + sin(time*0.63)),1.),0.5,0.6)*noi;
}

vec3 getVideo(vec2 uv, float time, sampler2D screenTexture)
{
	vec2 look = uv;
	float window = 1./(1.+20.*(look.y-mod(time/4.,1.))*(look.y-mod(time/4.,1.)));
	look.x = look.x + sin(look.y*10. + time)/50.*onOff(4.,4.,.3, time)*(1.+cos(time*80.))*window;
	float vShift = 0.4*onOff(2.,3.,.9, time)*(sin(time)*sin(time*20.) + 
	(0.5 + 0.1*sin(time*200.)*cos(time)));
	look.y = mod(look.y + vShift, 1.);
	vec3 video = texture(screenTexture,look).rgb;
	return video;
}

vec2 screenDistort(vec2 uv)
{
	uv -= vec2(.5,.5);
	uv = uv*1.2*(1./1.2+2.*uv.x*uv.x*uv.y*uv.y);
	uv += vec2(.5,.5);
	return uv;
}

void fragment()
{
	vec2 uv = screenDistort(SCREEN_UV);
	vec3 video = getVideo(uv, TIME, SCREEN_TEXTURE);
	float vigAmt = 3.+.3*sin(TIME + 5.*cos(TIME*5.));
	float vignette = (1.-vigAmt*(uv.y-.5)*(uv.y-.5))*(1.-vigAmt*(uv.x-.5)*(uv.x-.5));
	
	video += stripes(uv, TIME);
	//video += noise(uv*2., TIME)/2.;
	//video *= vignette;
	video *= (12.+mod(uv.y*30.+TIME,1.))/13.;
	
	COLOR = vec4(video,1.0);
}