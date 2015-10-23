/**
@brief Floyd-Steinberg Error Diffusion
*/
using UnityEngine;

public class FloydSteinbergErrorDiffusion
{
    const float K12 = 7.0f / 16.0f;
    const float K20 = 3.0f / 16.0f;
    const float K21 = 5.0f / 16.0f;
    const float K22 = 1.0f / 16.0f;

    public static void convert(Texture2D texture)
    {
        int size = texture.width * texture.height;
        Color[] color = new Color[size];

        for(int i = 0; i < texture.height; ++i) {
            for(int j = 0; j < texture.width; ++j) {
                Color c = texture.GetPixel(j, i);
                int index = i * texture.width + j;
                color[index].r = c.r;
                color[index].g = c.g;
                color[index].b = c.b;
                color[index].a = c.a;
            }
        }

        process(color, texture.width, texture.height);

        for(int i = 0; i < texture.height; ++i) {
            for(int j = 0; j < texture.width; ++j) {
                int index = i * texture.width + j;

                float a = Mathf.Clamp01(color[index].a);
                float r = Mathf.Clamp01(color[index].r);
                float g = Mathf.Clamp01(color[index].g);
                float b = Mathf.Clamp01(color[index].b);
                texture.SetPixel(j, i, new Color(r, g, b, a));
            }
        }
        texture.Apply();
    }


    private static float error16(float v)
    {
        float rv = quantize16(v);
        float error = v - rv;
        return error;
    }

    private static float error16(ref float v)
    {
        float rv = quantize16(v);
        float error = v - rv;
        v = rv;
        return error;
    }

    private static float quantize16(float v)
    {
        return Mathf.Round(v * 15.0f) / 15.0f;
    }

    private static void processRight(Color[] v, int y, int width, int height)
    {
        for(int j = 0; j < width; ++j) {
            int index = y * width + j;

            float error_r = error16(ref v[index].r);
            float error_g = error16(ref v[index].g);
            float error_b = error16(ref v[index].b);
            float error_a = error16(ref v[index].a);

            if(j < width - 1) {
                v[index + 1].r += error_r * K12;
                v[index + 1].g += error_g * K12;
                v[index + 1].b += error_b * K12;
                v[index + 1].a += error_a * K12;
            }

            if(y < height - 1) {
                index += width;

                if(0 < j) {
                    v[index - 1].r += error_r * K20;
                    v[index - 1].g += error_g * K20;
                    v[index - 1].b += error_b * K20;
                    v[index - 1].a += error_a * K20;
                }

                v[index].r += error_r * K21;
                v[index].g += error_g * K21;
                v[index].b += error_b * K21;
                v[index].a += error_a * K21;

                if(j < width - 1) {
                    v[index + 1].r += error_r * K22;
                    v[index + 1].g += error_g * K22;
                    v[index + 1].b += error_b * K22;
                    v[index + 1].a += error_a * K22;
                }
            }

        }//for(int j = 0;
    }

    private static void processLeft(Color[] v, int y, int width, int height)
    {
        for(int j = width - 1; 0 <= j; --j) {
            int index = y * width + j;

            float error_r = error16(ref v[index].r);
            float error_g = error16(ref v[index].g);
            float error_b = error16(ref v[index].b);
            float error_a = error16(ref v[index].a);

            if(0 < j) {
                v[index - 1].r += error_r * K12;
                v[index - 1].g += error_g * K12;
                v[index - 1].b += error_b * K12;
                v[index - 1].a += error_a * K12;
            }

            if(y < height - 1) {
                index += width;

                if(0 < j) {
                    v[index - 1].r += error_r * K22;
                    v[index - 1].g += error_g * K22;
                    v[index - 1].b += error_b * K22;
                    v[index - 1].a += error_a * K22;
                }

                v[index].r += error_r * K21;
                v[index].g += error_g * K21;
                v[index].b += error_b * K21;
                v[index].a += error_a * K21;

                if(j < width - 1) {
                    v[index + 1].r += error_r * K20;
                    v[index + 1].g += error_g * K20;
                    v[index + 1].b += error_b * K20;
                    v[index + 1].a += error_a * K20;
                }
            }

        }//for(int j = 0;
    }

    private static void process(Color[] v, int width, int height)
    {
        for(int i = 0; i < height; ++i) {
            bool scanline = (i & 0x01) == 0;
            if(scanline) {
                processRight(v, i, width, height);
            } else {
                processLeft(v, i, width, height);
            }
        }//for(int i = 0;
    }
}
