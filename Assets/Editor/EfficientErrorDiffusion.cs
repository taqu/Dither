/***
@brief Victor Ostromoukhov's Error Diffusion (http://liris.cnrs.fr/victor.ostromoukhov/publications/pdf/SIGGRAPH01_varcoeffED.pdf)
*/

using UnityEngine;

public class EfficientErrorDiffusion
{
    public static void convert(Texture2D texture)
    {
        int size = texture.width * texture.height;
        Color[] color = new Color[size];

        for(int i = 0; i < texture.height; ++i) {
            for(int j = 0; j < texture.width; ++j) {
                Color c = texture.GetPixel(j, i);
                int index = i * texture.width + j;

                //ColorToHSV(color, out H[index], out S[index], out V[index], out A[index]);

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

                //Color color = ColorFromHSV(H[index], S[index], V[index], A[index]);
                //texture.SetPixel(j, i, color);

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

    static void to16(float[] v, int width, int height)
    {
        for(int i = 0; i < width * height; ++i) {
            v[i] = quantize16(v[i]);
        }
    }


    private static void calcInputLevel(int[] level, float[] v)
    {
        for(int i = 0; i < level.Length; ++i) {
            level[i] = calcInputLevel(v[i]);
        }
    }

    public static int calcInputLevel(float v)
    {
        float l = v * 255.0f;
        if(l >= 127.5f) {
            return Mathf.Max(Mathf.Min(Mathf.RoundToInt(255.0f - l), 127), 0);
        } else {
            return Mathf.Max(Mathf.Min(Mathf.RoundToInt(l), 127), 0);
        }
    }

    public static int calcInputLevel(float r, float g, float b)
    {
        float l = (0.29891f * r + 0.58661f * g + 0.11448f * b) * 255.0f;
        if(l >= 127.5f) {
            return Mathf.Max(Mathf.Min(Mathf.RoundToInt(255.0f - l), 127), 0);
        } else {
            return Mathf.Max(Mathf.Min(Mathf.RoundToInt(l), 127), 0);
        }
    }

    private static void processRight(Color[] v, int y, int width, int height)
    {
        for(int j = 0; j < width; ++j) {
            int index = y * width + j;

            int level_r = calcInputLevel(v[index].r);//calcInputLevel(v[index].r, v[index].g, v[index].b);
            int level_g = calcInputLevel(v[index].g);//calcInputLevel(v[index].r, v[index].g, v[index].b);
            int level_b = calcInputLevel(v[index].b);//calcInputLevel(v[index].r, v[index].g, v[index].b);
            int level_a = calcInputLevel(v[index].a);

            float error_r = error16(ref v[index].r);
            float error_g = error16(ref v[index].g);
            float error_b = error16(ref v[index].b);
            float error_a = error16(ref v[index].a);

            //int level_r = calcInputLevel(v[index].r, v[index].g, v[index].b);
            //int level_g = level_r;
            //int level_b = level_r;
            //int level_a = calcInputLevel(v[index].a);

            if(j < width - 1) {
                v[index + 1].r += error_r * table[level_r].d0_;
                v[index + 1].g += error_g * table[level_g].d0_;
                v[index + 1].b += error_b * table[level_b].d0_;
                v[index + 1].a += error_a * table[level_a].d0_;
            }

            if(y < height - 1) {
                index += width;

                //if(j < width - 1) {
                //    v[index + 1].r += error_r * table[level_r].d0_;
                //    v[index + 1].g += error_g * table[level_g].d0_;
                //    v[index + 1].b += error_b * table[level_b].d0_;
                //    v[index + 1].a += error_a * table[level_a].d0_;
                //}

                if(0 < j) {
                    v[index - 1].r += error_r * table[level_r].d1_;
                    v[index - 1].g += error_g * table[level_g].d1_;
                    v[index - 1].b += error_b * table[level_b].d1_;
                    v[index - 1].a += error_a * table[level_a].d1_;
                }

                v[index].r += error_r * table[level_r].d2_;
                v[index].g += error_g * table[level_g].d2_;
                v[index].b += error_b * table[level_b].d2_;
                v[index].a += error_a * table[level_a].d2_;
            }

        }//for(int j = 0;
    }

    private static void processLeft(Color[] v, int y, int width, int height)
    {
        for(int j = width - 1; 0 <= j; --j) {
            int index = y * width + j;

            int level_r = calcInputLevel(v[index].r);//calcInputLevel(v[index].r, v[index].g, v[index].b);
            int level_g = calcInputLevel(v[index].g);//calcInputLevel(v[index].r, v[index].g, v[index].b);
            int level_b = calcInputLevel(v[index].b);//calcInputLevel(v[index].r, v[index].g, v[index].b);
            int level_a = calcInputLevel(v[index].a);

            float error_r = error16(ref v[index].r);
            float error_g = error16(ref v[index].g);
            float error_b = error16(ref v[index].b);
            float error_a = error16(ref v[index].a);

            //int level_r = calcInputLevel(v[index].r, v[index].g, v[index].b);
            //int level_g = level_r;
            //int level_b = level_r;
            //int level_a = calcInputLevel(v[index].a);

            if(0 < j) {
                v[index - 1].r += error_r * table[level_r].d0_;
                v[index - 1].g += error_g * table[level_g].d0_;
                v[index - 1].b += error_b * table[level_b].d0_;
                v[index - 1].a += error_a * table[level_a].d0_;
            }

            if(y < height - 1) {
                index += width;
                if(j < width - 1) {
                    v[index + 1].r += error_r * table[level_r].d1_;
                    v[index + 1].g += error_g * table[level_g].d1_;
                    v[index + 1].b += error_b * table[level_b].d1_;
                    v[index + 1].a += error_a * table[level_a].d1_;
                }

                v[index].r += error_r * table[level_r].d2_;
                v[index].g += error_g * table[level_g].d2_;
                v[index].b += error_b * table[level_b].d2_;
                v[index].a += error_a * table[level_a].d2_;
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


    private struct Entry
    {
        public Entry(float d0, float d1, float d2)
        {
            float m = 1.0f / (d0 + d1 + d2);
            d0_ = d0 * m;
            d1_ = d1 * m;
            d2_ = d2 * m;
        }

        public float d0_, d1_, d2_;
    };

    static readonly Entry[] table = new Entry[128]
{
new Entry(13, 0, 5),
new Entry(13, 0, 5),
new Entry(21, 0, 10),
new Entry(7, 0, 4),
new Entry(8, 0, 5),
new Entry(47, 3, 28),
new Entry(23, 3, 13),
new Entry(15, 3, 8),
new Entry(22, 6, 11),
new Entry(43, 15, 20),
new Entry(7, 3, 3),
new Entry(501, 224, 211),
new Entry(249, 116, 103),
new Entry(165, 80, 67),
new Entry(123, 62, 49),
new Entry(489, 256, 191),
new Entry(81, 44, 31),
new Entry(483, 272, 181),
new Entry(60, 35, 22),
new Entry(53, 32, 19),
new Entry(237, 148, 83),
new Entry(471, 304, 161),
new Entry(3, 2, 1),
new Entry(481, 314, 185),
new Entry(354, 226, 155),
new Entry(1389,866, 685),
new Entry(227, 138, 125),
new Entry(267, 158, 163),
new Entry(327, 188, 220),
new Entry(61, 34, 45),
new Entry(627, 338, 505),
new Entry(1227,638, 1075),

new Entry(20, 10, 19),
new Entry(1937,1000,1767),
new Entry(977, 520, 855),
new Entry(657, 360, 551),
new Entry(71, 40, 57),
new Entry(2005,1160,1539),
new Entry(337, 200, 247),
new Entry(2039,1240,1425),
new Entry(257, 160, 171),
new Entry(691, 440, 437),
new Entry(1045,680, 627),
new Entry(301, 200, 171),
new Entry(177, 120, 95),
new Entry(2141,1480,1083),
new Entry(1079,760, 513),
new Entry(725, 520, 323),
new Entry(137, 100, 57),
new Entry(2209,1640,855),
new Entry(53, 40, 19),
new Entry(2243,1720,741),
new Entry(565, 440, 171),
new Entry(759, 600, 209),
new Entry(1147,920, 285),
new Entry(2311,1880,513),
new Entry(97, 80, 19),
new Entry(335, 280, 57),
new Entry(1181,1000,171),
new Entry(793, 680, 95),
new Entry(599, 520, 57),
new Entry(2413,2120,171),
new Entry(405, 360, 19),
new Entry(2447,2200,57),

new Entry(11, 10, 0),
new Entry(158, 151, 3),
new Entry(178, 179, 7),
new Entry(1030,1091,63),
new Entry(248, 277, 21),
new Entry(318, 375, 35),
new Entry(458, 571, 63),
new Entry(878, 1159,147),
new Entry(5, 7, 1),
new Entry(172, 181, 37),
new Entry(97, 76, 22),
new Entry(72, 41, 17),
new Entry(119, 47, 29),
new Entry(4, 1, 1),
new Entry(4, 1, 1),
new Entry(4, 1, 1),
new Entry(4, 1, 1),
new Entry(4, 1, 1),
new Entry(4, 1, 1),
new Entry(4, 1, 1),
new Entry(4, 1, 1),
new Entry(4, 1, 1),
new Entry(65, 18, 17),
new Entry(95, 29, 26),
new Entry(185, 62, 53),
new Entry(30, 11, 9),
new Entry(35, 14, 11),
new Entry(85, 37, 28),
new Entry(55, 26, 19),
new Entry(80, 41, 29),
new Entry(155, 86, 59),
new Entry(5, 3, 2),

new Entry(5, 3, 2),
new Entry(5, 3, 2),
new Entry(5, 3, 2),
new Entry(5, 3, 2),
new Entry(5, 3, 2),
new Entry(5, 3, 2),
new Entry(5, 3, 2),
new Entry(5, 3, 2),
new Entry(5, 3, 2),
new Entry(5, 3, 2),
new Entry(5, 3, 2),
new Entry(5, 3, 2),
new Entry(305, 176, 119),
new Entry(155, 86, 59),
new Entry(105, 56, 39),
new Entry(80, 41, 29),
new Entry(65, 32, 23),
new Entry(55, 26, 19),
new Entry(335, 152, 113),
new Entry(85, 37, 28),
new Entry(115, 48, 37),
new Entry(35, 14, 11),
new Entry(355, 136, 109),
new Entry(30, 11, 9),
new Entry(365, 128, 107),
new Entry(185, 62, 53),
new Entry(25, 8, 7),
new Entry(95, 29, 26),
new Entry(385, 112, 103),
new Entry(65, 18, 17),
new Entry(395, 104, 101),
new Entry(4, 1, 1),
        };

    private static bool isEqual(float x0, float x1)
    {
        float d = Mathf.Abs(x1 - x0);
        return d <= 1.0e-5f;
    }

    private static void ColorToHSV(Color color, out float hue, out float saturation, out float value, out float a)
    {
        float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));

        hue = max - min;

        if(0.0f < hue) {
            if(isEqual(max, color.r)) {
                hue = (color.g - color.b) / hue;
                if(hue < 0.0f) {
                    hue += 6.0f;
                }
            } else if(isEqual(max, color.g)) {
                hue = 2.0f + (color.b - color.r) / hue;
            } else {
                hue = 4.0f + (color.r - color.g) / hue;
            }
        }
        hue /= 6.0f;
        saturation = max - min;
        if(!isEqual(max, 0.0f)) {
            saturation /= max;
        }
        value = max;
        a = color.a;
    }

    private static Color ColorFromHSV(float hue, float saturation, float value, float a)
    {
        float ia = a;
        float iv = value;

        if(saturation <= 1.0e-5f) {
            //return Color.FromArgb(ia, iv, iv, iv);
            return new Color(iv, iv, iv, ia);
        }

        hue *= 6.0f;

        int i = (int)Mathf.Floor(hue);
        float f = hue - i;
        float p = value * (1.0f - saturation);
        float q = value * (1.0f - saturation * f);
        float t = value * (1.0f - saturation * (1.0f - f));

        float r, g, b;
        switch(i) {
        case 0:
            r = value;
            g = t;
            b = p;
            break;
        case 1:
            r = q;
            g = value;
            b = p;
            break;
        case 2:
            r = p;
            g = value;
            b = t;
            break;
        case 3:
            r = p;
            g = q;
            b = value;
            break;
        case 4:
            r = t;
            g = p;
            b = value;
            break;
        default:
            r = value;
            g = p;
            b = q;
            break;
        }

        r = Mathf.Clamp01(r);
        g = Mathf.Clamp01(g);
        b = Mathf.Clamp01(b);
        ia = Mathf.Clamp01(ia);
        return new Color(r, g, b, ia);
    }
}
