using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

using NewtonVR;

public class NVRExampleReloadScene : MonoBehaviour {

    public NVRButton Button;
    public TextMesh Label;

    private int CurrentSceneIndex;

	// Use this for initialization
	void Start () {
        CurrentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (Label != null)
        {
            Label.text = string.Format("Load Next Scene\nCurrent Scene: {0}", CurrentSceneIndex);
        }
	}
	
	// Update is called once per frame
	void Update () {
        if (Button.ButtonWasPushed && Button.TurnedOn)
        {
            Button.TurnedOn = false;
            LoadNextScene();
        }
        if (!Button.TurnedOn && Button.ButtonUp)
        {
            Button.TurnedOn = true;
        }
	}

    public void LoadNextLevel()
    {
        if (Application.levelCount > (Application.loadedLevel + 1))
            Application.LoadLevel(Application.loadedLevel + 1);
    }

    public void LoadNextLevelAsync()
    {
        if (Application.levelCount > (Application.loadedLevel + 1))
            Application.LoadLevelAsync(Application.loadedLevel + 1);
    }

    public void LoadNextScene()
    {
        if (SceneManager.sceneCountInBuildSettings > (CurrentSceneIndex + 1))
            SceneManager.LoadScene(CurrentSceneIndex + 1, LoadSceneMode.Single);
    }

    public void LoadNextSceneAsync()
    {
        if (SceneManager.sceneCountInBuildSettings > (CurrentSceneIndex + 1))
            SceneManager.LoadSceneAsync(CurrentSceneIndex + 1, LoadSceneMode.Single);
    }

    public void CreateAndCopyToScene()
    {
        Scene CurrentScene = SceneManager.GetActiveScene();
        Scene NextScene = SceneManager.CreateScene(CurrentScene.name + "-copy");
        foreach(GameObject GO in CurrentScene.GetRootGameObjects())
        {
            SceneManager.MoveGameObjectToScene(GO, NextScene);
        }
        SceneManager.SetActiveScene(NextScene);
    }

}
