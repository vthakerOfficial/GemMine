using UnityEditor;
using UnityEngine;

public class WebGLBuilder
{
    public static void Build()
    {
        string projectPath = System.IO.Path.GetFullPath(".");
        string outputPath = System.IO.Path.Combine(projectPath, "WebGL_Build");

        string[] scenes = {
            "Assets/Scenes/InterfaceManager.unity",
            "Assets/Scenes/Startup.unity",
            "Assets/Scenes/Tutorial.unity",
            "Assets/Scenes/SMaze2.unity"
        };

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.Development
        };

        BuildPipeline.BuildPlayer(opts);
    }
}
