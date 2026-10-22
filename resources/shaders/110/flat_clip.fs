#version 110

const vec3 ZERO = vec3(0.0, 0.0, 0.0);

uniform vec4 uniform_color;

varying vec3 clipping_planes_dots;

void main()
{
    if (clipping_planes_dots.x < 0.0 || clipping_planes_dots.y < 0.0 || clipping_planes_dots.z < 0.0)
        discard;

    gl_FragColor = uniform_color;
}
