using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//最終的に未使用です。
public class PuyoDirector : MonoBehaviour
{
    [SerializeField] private GameObject player = default!;
    private PlayerController _playerController = null;

    private NextQueue _nextQueue = new();

    // Start is called before the first frame update
    void Start()
    {
        _playerController = player.GetComponent<PlayerController>();

        _nextQueue.Initialize();
    }

    private void FixedUpdate()
    {
        if (!player.activeSelf)
        {
            Spawn(_nextQueue.Update());
        }
    }

    bool Spawn(Vector2Int next) => _playerController.Spawn((PuyoType)next[0], (PuyoType)next[1]);
}
