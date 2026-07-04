using System.IO;
using UnityEngine;

/// <summary>
/// Offscreen still capture: renders a camera into a RenderTexture and writes
/// the result as a PNG. Used for the gallery shots. This deliberately avoids
/// ScreenCapture, which garbles on this editor whenever an IMGUI HUD is on
/// screen; IMGUI never renders into a camera RenderTexture, so captures taken
/// this way are always clean.
/// </summary>
public static class ShotCapture
{
    /// <summary>Render <paramref name="cam"/> at width x height and write a PNG to <paramref name="path"/>. Returns the full path.</summary>
    public static string Capture(Camera cam, string path, int width = 1600, int height = 900)
    {
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;
        Texture2D tex = null;
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }
        finally
        {
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            rt.Release();
            Object.DestroyImmediate(rt);
            if (tex != null) Object.DestroyImmediate(tex);
        }
        return Path.GetFullPath(path);
    }
}
