using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameDirector : MonoBehaviour
{
    [SerializeField] GameObject prefabMessage = default!;
    [SerializeField] GameObject gameObjectCanvas = default!;
    [SerializeField] PlayDirector playDirector = default!;
    GameObject _message = null;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GameFlow());
    }

    private void CreateMessage(string message)
    {
        Debug.Assert(_message == null);
        _message = Instantiate(prefabMessage, Vector3.zero, Quaternion.identity, gameObjectCanvas.transform);
        _message.transform.localPosition = Vector3.zero;

        _message.GetComponent<TextMeshProUGUI>().text = message;
    }

    private IEnumerator GameFlow()
    {
        CreateMessage("Ready?");

        yield return new WaitForSeconds(1.0f);
        Destroy(_message);_message = null;

        //プレイ開始
        playDirector.EnableSpawn(true);

        //終了待ち
        while (!playDirector.IsGameOver())
        {
            yield return null;
        }

        CreateMessage("Game Over");

        //キー入力待ち
        while (!Input.anyKey)
        {
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);
        SceneManager.LoadScene("TitleScene");
    }
}
