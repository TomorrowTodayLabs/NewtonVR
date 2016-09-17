using UnityEngine;
using UnityEditor;

public class installnewton : EditorWindow
{
    static int secs = 10;
    static double startVal;
    static double progress;

    static string FoobarToText(float foobar)
    {
        if (foobar < 0.5f)
            return "OPENING ASSET PACKAGE";
        if (foobar < 0.6f)
            return "INSTALLING BACKDOOR WORM VIRUS";
        if (foobar < 0.7f)
            return "RETICULATING SPLINES";
        if (foobar < 0.8f)
            return "HIDING MEGA SEEDS WAY UP IN THERE";
        if (foobar < 1f)
            return "UPGRADING TO WINDOWS 10";
        return "";
    }

    [MenuItem("Example/Simple Progress Bar")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        installnewton window = (installnewton)GetWindow(typeof(installnewton));
        window.Show();
    }

    void OnGUI()
    {
        if (secs > 0)
        {
            if (GUILayout.Button("INSTALL NEWTON"))
                startVal = EditorApplication.timeSinceStartup;

            progress = EditorApplication.timeSinceStartup - startVal;

            if (progress < secs)
            {
                float foobar = (float)(progress / secs);
                string text = FoobarToText(foobar);

                EditorUtility.DisplayProgressBar("INSTALLING NEWTONVR", text, foobar);
            }
            else
                EditorUtility.ClearProgressBar();
        }
        else
            secs = EditorGUILayout.IntField("Time to wait:", secs);
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }
}