using UnityEngine;
using UnityEditor;

public class installnewton : EditorWindow
{
    static int secs = 10;
    static double startVal = 0;
    static double progress = 0;


    [MenuItem("Example/Simple Progress Bar")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        installnewton window = (installnewton)EditorWindow.GetWindow(typeof(installnewton));
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
                string text = "";

                if (foobar < 0.5f)
                {
                    text = "OPENING ASSET PACKAGE";
                }
                else if (foobar < 0.6f)
                {
                    text = "INSTALLING BACKDOOR WORM VIRUS";
                }
                else if (foobar < 0.7f)
                {
                    text = "RETICULATING SPLINES";
                }
                else if (foobar < 0.8f)
                {
                    text = "HIDING MEGA SEEDS WAY UP IN THERE";
                }
                else if (foobar < 1f)
                {
                    text = "UPGRADING TO WINDOWS 10";
                }

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