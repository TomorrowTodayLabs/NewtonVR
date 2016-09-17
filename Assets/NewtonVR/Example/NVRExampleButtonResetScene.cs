﻿using UnityEngine;
using UnityEngine.SceneManagement;

namespace NewtonVR.Example
{
    public class NVRExampleButtonResetScene : MonoBehaviour
    {
        public void ResetScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}