#version 330 core

in vec4 color;
in vec2 texCoord;

out vec4 fragColor;

uniform sampler2D uTexture;

vec3 lerp(vec3 a, vec3 b, float t) {
	return (b - a) * t + a;
}

vec3 getDecalColor(int blueValue) {
	int index = 254 - blueValue;
	vec2 coordinate = vec2(index, 0.0);
	return texture(uTexture, coordinate).rgb;
}

void main() {
	vec4 texColor = texture(uTexture, texCoord) * color;
	
	int texR = int(texColor.r * 255.0);
	int texG = int(texColor.g * 255.0);
	int texB = int(texColor.b * 255.0);
	
	fragColor.r = texColor.r;
	fragColor.g = texColor.g;
	fragColor.b = texColor.b;
	fragColor.a = 1.0;
	
	if (texG == 8) {
		vec3 decalColor = getDecalColor(texB);
		fragColor.rgb = lerp(decalColor * 0.5, decalColor, texColor.r);
	}
	
	if (texR == 1 && texG == 0 && texB == 0) {
		fragColor.a = 0.1;
	}
}