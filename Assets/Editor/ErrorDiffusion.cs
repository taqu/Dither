/**
@brief Contrast-Aware Halftoning(http://gigl.scs.carleton.ca/node/84)を使った, 拡散誤差法
*/
#define ERRORDIFFUSION_MASKCENTER
#define ERRORDIFFUSION_VARIANT
#define ERRORDIFFUSION_ERRORABS
using UnityEngine;


public class ErrorDifusion
{
    private const int MaskSize = 5;
    private const int HalfMaskSize = MaskSize / 2;
    private const float K = 2.0f;

    private float[,] radius_ = new float[MaskSize, MaskSize];
    private float[,] weights_ = new float[MaskSize, MaskSize];

#if ERRORDIFFUSION_VARIANT
    private Heap heap_ = new Heap();
    private Heap.Node[,] pixels_;
#endif

    public void convert(Texture2D texture)
    {
        initialize(texture.width, texture.height);


        int size = texture.width * texture.height;

        float[] H = new float[size];
        float[] S = new float[size];
        float[] V = new float[size];
        float[] A = new float[size];

        for(int i = 0; i < texture.height; ++i) {
            for(int j = 0; j < texture.width; ++j) {
                Color color = texture.GetPixel(j, i);
                int index = i * texture.width + j;

#if true
                H[index] = color.r;
                S[index] = color.g;
                V[index] = color.b;
                A[index] = color.a;
#elif false
                ColorToHSV(color, out H[index], out S[index], out V[index], out A[index]);
                H[index] *= 255.0f;
                S[index] *= 255.0f;
                V[index] *= 255.0f;
                A[index] *= 255.0f;
#else
                ColorToYUV(color, out H[index], out S[index], out V[index], out A[index]);
#endif
            }
        }

        process(H, texture.width, texture.height);

        process(S, texture.width, texture.height);

        process(V, texture.width, texture.height);

        process(A, texture.width, texture.height);

        for(int i = 0; i < texture.height; ++i) {
            for(int j = 0; j < texture.width; ++j) {
                int index = i * texture.width + j;
#if true
                Color color;
                color.r = Mathf.Clamp01(H[index]);
                color.g = Mathf.Clamp01(S[index]);
                color.b = Mathf.Clamp01(V[index]);
                color.a = Mathf.Clamp01(A[index]);
#elif false
                Color color = ColorFromHSV(H[index]/255.0f, S[index]/255.0f, V[index]/255.0f, A[index]/255.0f);
#else
                Color color = ColorFromYUV(H[index], S[index], V[index], A[index]);
#endif
                texture.SetPixel(j, i, color);
            }
        }
        texture.Apply();
    }

    private void initialize(int width, int height)
    {
        float totalWeight = 0.0f;
        for(int i = 0; i < MaskSize; ++i) {
#if ERRORDIFFUSION_MASKCENTER
            float h = i - HalfMaskSize;
#else
                float h = i;
#endif
            for(int j = 0; j < MaskSize; ++j) {
#if ERRORDIFFUSION_MASKCENTER
                float w = j - HalfMaskSize;
                if(j == HalfMaskSize && i == HalfMaskSize) {
#else
                    float w = j;
                    if(j == 0 && i == 0) {
#endif
                    radius_[i, j] = 0.0f;
                    continue;
                }
                radius_[i, j] = Mathf.Sqrt(h * h + w * w);
                radius_[i, j] = 1.0f / Mathf.Pow(radius_[i, j], K);
                totalWeight += radius_[i, j];
            }
        }
        if(!isEqual(totalWeight, 0.0f)) {
            float invTotalWeight = 1.0f / totalWeight;
            for(int i = 0; i < MaskSize; ++i) {
                for(int j = 0; j < MaskSize; ++j) {
                    radius_[i, j] *= invTotalWeight;
                }
            }
        }

#if ERRORDIFFUSION_VARIANT
        pixels_ = new Heap.Node[height, width];
        for(int i = 0; i < height; ++i) {
            for(int j = 0; j < width; ++j) {
                pixels_[i, j] = new Heap.Node();
                pixels_[i, j].x_ = j;
                pixels_[i, j].y_ = i;
            }
        }
#endif
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

    private void carryError(float[] v, int width, int height, int x, int y, float error)
    {
#if ERRORDIFFUSION_VARIANT
#if ERRORDIFFUSION_MASKCENTER
        int sx = Mathf.Max(0, x - HalfMaskSize);
        int ex = Mathf.Min(width, x + HalfMaskSize + 1);
        int sy = Mathf.Max(0, y - HalfMaskSize);
        int ey = Mathf.Min(height, y + HalfMaskSize + 1);
#else
        int sx = x;
        int ex = Mathf.Min(width, x+MaskSize);
        int sy = y;
        int ey = Mathf.Min(height, y+MaskSize);
#endif
        for(int i = sy; i < ey; ++i) {
#if ERRORDIFFUSION_MASKCENTER
            int wy = i - y + HalfMaskSize;
#else
            int wy = i-sy;
#endif
            for(int j = sx; j < ex; ++j) {
#if ERRORDIFFUSION_MASKCENTER
                int wx = j - x + HalfMaskSize;
#else
                int wx = j-sx;
#endif
                if(0 <= pixels_[i, j].index_) {
                    v[i * width + j] += error * radius_[wy, wx];
                }
            }
        }

#else //ERRORDIFFUSION_VARIANT
            if(x < width - 1) {
                v[y * width + x + 1] += error;
                return;
            }
            if(y < height - 1) {
                v[(y + 1) * width + x] += error;
                return;
            }
#endif
    }

    private void diffuse(float[] v, int width, int height, int x, int y)
    {
        int index = width * y + x;
        float error = error16(ref v[index]);

#if ERRORDIFFUSION_MASKCENTER
        int sx = Mathf.Max(0, x - HalfMaskSize);
        int ex = Mathf.Min(width, x + HalfMaskSize + 1);
        int sy = Mathf.Max(0, y - HalfMaskSize);
        int ey = Mathf.Min(height, y + HalfMaskSize + 1);
#else
            int sx = x;
            int ex = Mathf.Min(width, x + MaskSize);
            int sy = y;
            int ey = Mathf.Min(height, y + MaskSize);
#endif
        float totalWeight = 0.0f;
        for(int i = sy; i < ey; ++i) {
#if ERRORDIFFUSION_MASKCENTER
            int wy = i - y + HalfMaskSize;
#else
                int wy = i - sy;
#endif
            for(int j = sx; j < ex; ++j) {
                if(x == j && y == i) {
                    continue;
                }
#if ERRORDIFFUSION_MASKCENTER
                int wx = j - x + HalfMaskSize;
#else
                    int wx = j - sx;
#endif
                float l = Mathf.Abs(error16(v[i * width + j]));
                float weight = l * radius_[wy, wx];

                weights_[wy, wx] = weight;
                totalWeight += weight;
            }
        }
        if(isEqual(totalWeight, 0.0f)) {
            carryError(v, width, height, x, y, error);
            return;
        }
        float residualError = 0.0f;
        float invTotal = 1.0f / totalWeight;
        for(int i = sy; i < ey; ++i) {
#if ERRORDIFFUSION_MASKCENTER
            int wy = i - y + HalfMaskSize;
#else
                int wy = i - sy;
#endif

            for(int j = sx; j < ex; ++j) {
                if(x == j && y == i) {
                    continue;
                }

#if ERRORDIFFUSION_MASKCENTER
                int wx = j - x + HalfMaskSize;
#else
                    int wx = j - sx;
#endif
                float tv = v[i * width + j] + error * weights_[wy, wx] * invTotal;
                if(tv < 0.0f) {
                    residualError += tv;
                    tv = 0.0f;
                } else if(1.0f < tv) {
                    residualError += tv - 1.0f;
                    tv = 1.0f;
                }
                v[i * width + j] = tv;
            }
        }
        carryError(v, width, height, x, y, residualError);
    }

#if ERRORDIFFUSION_VARIANT
    private void recalcError(float[] v, int width, int height, int x, int y)
    {
        int sx = Mathf.Max(0, x - HalfMaskSize);
        int ex = Mathf.Min(width, x + HalfMaskSize + 1);
        int sy = Mathf.Max(0, y - HalfMaskSize);
        int ey = Mathf.Min(height, y + HalfMaskSize + 1);

        for(int i = sy; i < ey; ++i) {
            for(int j = sx; j < ex; ++j) {
                if(x == j && y == i) {
                    continue;
                }
                if(pixels_[i, j].index_ < 0) {
                    continue;
                }
#if ERRORDIFFUSION_ERRORABS
                float newError = Mathf.Abs(error16(v[i * width + j]));
#else
                float newError = error16(v[i*width+j]);
#endif
                heap_.update(pixels_[i, j], newError);
            }
        }
    }
#endif

    private void process(float[] v, int width, int height)
    {
#if ERRORDIFFUSION_VARIANT
        heap_.clear();
        for(int i = 0; i < height; ++i) {
            for(int j = 0; j < width; ++j) {
#if ERRORDIFFUSION_ERRORABS
                float error = Mathf.Abs(error16(v[i * width + j]));
#else
                float error = error16(v[i*width+j]);
#endif
                pixels_[i, j].error_ = error;
                heap_.add(pixels_[i, j]);
            }
        }

        while(0 < heap_.Count) {
            Heap.Node node = heap_.pop_max();
            diffuse(v, width, height, node.x_, node.y_);
            recalcError(v, width, height, node.x_, node.y_);
        }
#else
            for(int i = 0; i < height; ++i) {
                for(int j = 0; j < width; ++j) {
                    diffuse(v, width, height, j, i);
                }
            }
#endif
    }

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


    private static void ColorToYUV(Color color, out float Y, out float U, out float V, out float a)
    {
        Y = 0.29891f * color.r + 0.58661f * color.g + 0.11448f * color.b;
        U = -0.16874f * color.r - 0.33126f * color.g + 0.50000f * color.b;
        V = 0.50000f * color.r - 0.41869f * color.g - 0.08131f * color.b;

        Y *= 255.0f;
        U *= 255.0f;
        V *= 255.0f;

        U += 128.0f; //-128-127 -> 0-255;
        V += 128.0f; //-128-127 -> 0-255;
        a = color.a * 255.0f;
    }

    private static Color ColorFromYUV(float Y, float U, float V, float a)
    {
        U -= 128.0f; //0-255 -> -128-127;
        V -= 128.0f; //0-255 -> -128-127;

        float ia = Mathf.Clamp01(a / 255.0f);
        float r = Mathf.Clamp01((Y + 1.40200f * V) / 255.0f);
        float g = Mathf.Clamp01((Y - 0.34414f * U - 0.71414f * V) / 255.0f);
        float b = Mathf.Clamp01((Y + 1.77200f * U) / 255.0f);

        return new Color(r, g, b, ia);
    }
}
