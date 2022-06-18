using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayDirector : MonoBehaviour
{
    [SerializeField] GameObject player = default!;
    PlayerController _playerController = null;
    LogicalInput _logicalInput = new();

    NextQueue _nextQueue = new();
    [SerializeField] PuyoPair[] nextPuyoPairs = { default!, default! }; //��next�̃Q�[���I�u�W�F�N�g�̐���

    // Start is called before the first frame update
    void Start()
    {
        _playerController = player.GetComponent<PlayerController>();
        _logicalInput.Clear();
        _playerController.SetLogicalInput(_logicalInput);

        _nextQueue.Initialize();
        Spawn(_nextQueue.Update());
        UpdateNextsView();
    }

    void UpdateNextsView()
    {
        _nextQueue.Each((int idx, Vector2Int n) =>
        {
            nextPuyoPairs[idx++].SetPuyoType((PuyoType)n.x, (PuyoType)n.y);
        });
    }

    static readonly KeyCode[] key_code_tbl = new KeyCode[(int)LogicalInput.Key.Max]
{
        KeyCode.RightArrow, //Right
        KeyCode.LeftArrow,  //Left
        KeyCode.X,          //RotR
        KeyCode.Z,          //RotL
        KeyCode.UpArrow,    //QuickDrop
        KeyCode.DownArrow,  //Down
};

    private void UpdateInput()
    {
        LogicalInput.Key inputDev = 0;//�f�o�C�X�l

        //�L�[���͎擾
        for (int i = 0; i < (int)LogicalInput.Key.Max; i++)
        {
            if (Input.GetKey(key_code_tbl[i]))
            {
                inputDev |= (LogicalInput.Key)(1 << i);
            }
        }

        _logicalInput.Update(inputDev);
    }

    bool Spawn(Vector2Int next) => _playerController.Spawn((PuyoType)next[0], (PuyoType)next[1]);

    private void FixedUpdate()
    {
        //���͂���荞��
        UpdateInput();

        if (!player.activeSelf)
        {
            Spawn(_nextQueue.Update());
            UpdateNextsView();
        }
    }

}