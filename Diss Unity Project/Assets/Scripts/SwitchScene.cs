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
    public void SwitchToSelectLanguagesScreen()
    {
        SceneManager.LoadScene(2);
    }
    public void SwitchToARCameraScreen()
    {
        SceneManager.LoadScene(3);
    }
    public void SwitchToMapScreen()
    {
        SceneManager.LoadScene(4);
    }
    public void SwitchToTutorialOneScreen()
    {
        SceneManager.LoadScene(5);
    }
    public void SwitchToTutorialTwoScreen()
    {
        SceneManager.LoadScene(6);
    }
    public void SwitchToTutorialThreeScreen()
    {
        SceneManager.LoadScene(7);
    }


}
