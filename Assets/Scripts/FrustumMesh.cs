using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural truncated cone (frustum) - a "cylinder with one side bigger than
/// the other". Set topRadius = 0 for a sharp cone, or topRadius = bottomRadius
/// for a plain cylinder. Caps are skipped automatically when a radius is 0.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FrustumMesh : MonoBehaviour
{
    public float bottomRadius = 0.5f;
    public float topRadius = 0.25f;
    public float height = 1f;
    [Range(3, 64)] public int segments = 24;

    void Awake() => GetComponent<MeshFilter>().sharedMesh = Build();

    [ContextMenu("Rebuild")]
    void Rebuild() => GetComponent<MeshFilter>().sharedMesh = Build();

    public Mesh Build()
    {
        var v = new List<Vector3>();
        var t = new List<int>();

        // side wall
        for (int i = 0; i < segments; i++)
        {
            float a0 = (float)i / segments * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;
            Vector3 b0 = new(Mathf.Cos(a0) * bottomRadius, 0, Mathf.Sin(a0) * bottomRadius);
            Vector3 b1 = new(Mathf.Cos(a1) * bottomRadius, 0, Mathf.Sin(a1) * bottomRadius);
            Vector3 u0 = new(Mathf.Cos(a0) * topRadius, height, Mathf.Sin(a0) * topRadius);
            Vector3 u1 = new(Mathf.Cos(a1) * topRadius, height, Mathf.Sin(a1) * topRadius);
            int s = v.Count;
            v.Add(b0); v.Add(b1); v.Add(u0); v.Add(u1);
            t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3 });
        }

        AddCap(v, t, bottomRadius, 0f, false); // bottom faces -Y
        AddCap(v, t, topRadius, height, true);  // top faces +Y

        var mesh = new Mesh { name = "Frustum" };
        mesh.SetVertices(v);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void AddCap(List<Vector3> v, List<int> t, float radius, float y, bool up)
    {
        if (radius <= 0f) return;
        int c = v.Count;
        v.Add(new Vector3(0, y, 0)); // center
        for (int i = 0; i <= segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            v.Add(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
        }
        for (int i = 1; i <= segments; i++)
            if (up) t.AddRange(new[] { c, c + i, c + i + 1 });
            else t.AddRange(new[] { c, c + i + 1, c + i });
    }
}
