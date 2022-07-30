using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleDirector : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.anyKey)
        {
            Invoke("ChangeScene", 1.0f);
        }
    }

    private void ChangeScene()
    {
        SceneManager.LoadScene("PlayScene");
    }
}
