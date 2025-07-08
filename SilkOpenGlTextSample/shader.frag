#version 330 core
in vec3 fragColor;
in vec2 texCoord;

uniform sampler2D glyphTexture;
uniform bool isTextured;

out vec4 FragColor;

void main()
{
    if (isTextured)
    {
        float alpha = texture(glyphTexture, texCoord).a;
        FragColor = vec4(fragColor, alpha);
    }
    else
    {
        FragColor = vec4(fragColor, 1.0);
    }
}