using UnityEngine;

namespace ZG
{
public class Utilities : MonoBehaviour
{
    public bool isGoActive;
    
    public void GameobjectToggle()
    {
        isGoActive = !isGoActive;
        gameObject.SetActive(isGoActive);
    }

    public void QuitApp()
    {
        Application.Quit();
    }
}
}
