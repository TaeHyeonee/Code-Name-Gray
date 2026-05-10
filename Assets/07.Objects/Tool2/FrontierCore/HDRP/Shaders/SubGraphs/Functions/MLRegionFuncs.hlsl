// MLJRegionFuncs.hlsl
// Helper functions for MLJ multi-material shader

// -------- Normalize (float + half) --------
void MLJ_Normalize5_core_float(float w0, float w1, float w2, float w3, float w4,
                               out float o0, out float o1, out float o2, out float o3, out float o4)
{
    float s = max(w0 + w1 + w2 + w3 + w4, 1e-4);
    o0 = w0 / s;
    o1 = w1 / s;
    o2 = w2 / s;
    o3 = w3 / s;
    o4 = w4 / s;
}

void MLJ_Normalize5_float(float w0, float w1, float w2, float w3, float w4,
                          out float o0, out float o1, out float o2, out float o3, out float o4)
{
    MLJ_Normalize5_core_float(w0, w1, w2, w3, w4, o0, o1, o2, o3, o4);
}

void MLJ_Normalize5(float w0, float w1, float w2, float w3, float w4,
                    out float o0, out float o1, out float o2, out float o3, out float o4)
{
    MLJ_Normalize5_core_float(w0, w1, w2, w3, w4, o0, o1, o2, o3, o4);
}

void MLJ_Normalize5_half(half w0, half w1, half w2, half w3, half w4,
                         out half o0, out half o1, out half o2, out half o3, out half o4)
{
    half s = max(w0 + w1 + w2 + w3 + w4, (half) 1e-4);
    o0 = w0 / s;
    o1 = w1 / s;
    o2 = w2 / s;
    o3 = w3 / s;
    o4 = w4 / s;
}

// -------- Top-1 (float + half) --------
void MLJ_Top1_5_core_float(float w0, float w1, float w2, float w3, float w4,
                           out float o0, out float o1, out float o2, out float o3, out float o4)
{
    float m = max(max(w0, w1), max(w2, max(w3, w4)));
    o0 = (w0 >= m) ? 1.0 : 0.0;
    o1 = (w1 >= m) ? 1.0 : 0.0;
    o2 = (w2 >= m) ? 1.0 : 0.0;
    o3 = (w3 >= m) ? 1.0 : 0.0;
    o4 = (w4 >= m) ? 1.0 : 0.0;
}

void MLJ_Top1_5_float(float w0, float w1, float w2, float w3, float w4,
                      out float o0, out float o1, out float o2, out float o3, out float o4)
{
    MLJ_Top1_5_core_float(w0, w1, w2, w3, w4, o0, o1, o2, o3, o4);
}

void MLJ_Top1_5(float w0, float w1, float w2, float w3, float w4,
                out float o0, out float o1, out float o2, out float o3, out float o4)
{
    MLJ_Top1_5_core_float(w0, w1, w2, w3, w4, o0, o1, o2, o3, o4);
}

void MLJ_Top1_5_half(half w0, half w1, half w2, half w3, half w4,
                     out half o0, out half o1, out half o2, out half o3, out half o4)
{
    half m = max(max(w0, w1), max(w2, max(w3, w4)));
    o0 = (w0 >= m) ? (half) 1.0 : (half) 0.0;
    o1 = (w1 >= m) ? (half) 1.0 : (half) 0.0;
    o2 = (w2 >= m) ? (half) 1.0 : (half) 0.0;
    o3 = (w3 >= m) ? (half) 1.0 : (half) 0.0;
    o4 = (w4 >= m) ? (half) 1.0 : (half) 0.0;
}
