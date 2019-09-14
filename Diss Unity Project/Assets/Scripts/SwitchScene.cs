using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
    public void SwitchToStartScreen()
    {
        SceneManager.LoadScene(0);
    }

    public void SwitchToMenuScreen()
    {
        SceneManager.LoadScene(1);
    }
    public void SwitchToARCameraScreen()
    {
        SceneManager.LoadScene(2);
    }
    public void SwitchToMapScreen()
    {
        SceneManager.LoadScene(3);
    }
    public void SwitchToTutorialScreen()
    {
        SceneManager.LoadScene(4);
    }



}
