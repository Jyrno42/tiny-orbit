using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Surface-of-revolution (lathe) mesh: spins a 2D silhouette around the Y axis.
/// The profile IS the part - swap profiles to get a command pod, nose cone,
/// frustum, heat-shield dome, engine bell, etc. from one generator.
/// Profile points are (x = radius, y = height), ordered bottom -> top.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LatheMesh : MonoBehaviour
{
    [Range(3, 64)] public int sides = 24;

    void Awake() => GetComponent<MeshFilter>().sharedMesh = Build(CommandPodProfile());

    [ContextMenu("Rebuild (command pod)")]
    void Rebuild() => GetComponent<MeshFilter>().sharedMesh = Build(CommandPodProfile());

    public Mesh Build(IReadOnlyList<Vector2> profile)
    {
        int rings = profile.Count, cols = sides + 1;
        var v = new Vector3[rings * cols];
        for (int r = 0; r < rings; r++)
            for (int s = 0; s < cols; s++)
            {
                float a = (float)s / sides * Mathf.PI * 2f;
                v[r * cols + s] = new Vector3(
                    Mathf.Cos(a) * profile[r].x, profile[r].y, Mathf.Sin(a) * profile[r].x);
            }

        var t = new List<int>();
        for (int r = 0; r < rings - 1; r++)
            for (int s = 0; s < sides; s++)
            {
                int a = r * cols + s, b = a + 1, c = a + cols, d = c + 1;
                t.AddRange(new[] { a, c, b, b, c, d }); // swap a/b if it renders inside-out
            }

        var mesh = new Mesh { name = "Lathe" };
        mesh.vertices = v;
        mesh.triangles = t.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>Mercury/Apollo-style command pod: flat base, tapered body, rounded top dome.</summary>
    public static List<Vector2> CommandPodProfile(
        float baseR = 0.6f, float topR = 0.25f, float bodyH = 0.8f, float domeH = 0.3f, int domeSteps = 6)
    {
        var p = new List<Vector2>
        {
            new(0f, 0f),       // base center -> flat bottom cap
            new(baseR, 0f),    // base rim
            new(topR, bodyH),  // straight taper up to the shoulder
        };
        for (int i = 1; i <= domeSteps; i++) // quarter-circle dome over the top
        {
            float a = (float)i / domeSteps * Mathf.PI * 0.5f;
            p.Add(new Vector2(topR * Mathf.Cos(a), bodyH + domeH * Mathf.Sin(a)));
        }
        return p;
    }

    /// <summary>Pointed nose cone (top radius 0).</summary>
    public static List<Vector2> NoseConeProfile(float baseR = 0.5f, float height = 1f)
        => new() { new(0f, 0f), new(baseR, 0f), new(0f, height) };
}
