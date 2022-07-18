using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct FallData
{
    public readonly int X { get; }
    public readonly int Y { get; }
    public readonly int Dest { get; }//�������
    public FallData(int x, int y, int dest)
    {
        X = x;
        Y = y;
        Dest = dest;
    }
}

public class BoardController : MonoBehaviour
{
    public const int FALL_FRAME_PAR_CELL = 5;//�P�ʃZ��������̗���FRAME��
    public const int BOARD_WIDTH = 6;
    public const int BOARD_HEIGHT = 14;

    [SerializeField] GameObject prefabPuyo = default!;

    int[,] _board = new int[BOARD_HEIGHT, BOARD_WIDTH];
    GameObject[,] _Puyos = new GameObject[BOARD_HEIGHT, BOARD_WIDTH];

    uint _additiveScore = 0;

    //������ۂ̈ꎞ�I�ϐ�
    List<FallData> _falls = new();
    int _fallFrames = 0;

    //�폜����ۂ̈ꎞ�I�ϐ�
    List<Vector2Int> _erases = new();
    int _eraseFrames = 0;

    void Start()
    {
        ClearAll();
    }

    /// <summary>
    /// �Ֆʂ��N���A����
    /// </summary>
    private void ClearAll()
    {
        for (int y = 0; y < BOARD_HEIGHT; y++)
        {
            for (int x = 0; x < BOARD_WIDTH; x++)
            {
                _board[y, x] = 0;
            }
        }
    }

    public static bool IsValidated(Vector2Int pos)
    {
        return 0 <= pos.x && pos.x < BOARD_WIDTH
            && 0 <= pos.y && pos.y < BOARD_HEIGHT;
    }
    public bool CanSettle(Vector2Int pos)
    {
        if (!IsValidated(pos)) return false;

        return 0 == _board[pos.y, pos.x];
    }

    /// <summary>
    /// _board�ɒl��ݒ肷��
    /// </summary>
    /// <param name="pos">�ݒu���W</param>
    /// <param name="val">Puyo��type</param>
    /// <returns></returns>
    public bool Settle(Vector2Int pos, int val)
    {
        if (!CanSettle(pos)) return false;

        _board[pos.y, pos.x] = val;

        Debug.Assert(_Puyos[pos.y, pos.x] == null);
        Vector3 world_position = transform.position + new Vector3(pos.x, pos.y, 0.0f);
        _Puyos[pos.y, pos.x] = Instantiate(prefabPuyo, world_position, Quaternion.identity, transform);
        _Puyos[pos.y, pos.x].GetComponent<PuyoController>().SetPuyoType((PuyoType)val);

        return true;
    }

    public bool CheckFall()
    {
        _falls.Clear();
        _fallFrames = 0;

        //�������̍����̋L�^�p
        int[] dests = new int[BOARD_WIDTH];
        for (int i = 0; i < BOARD_WIDTH; i++) dests[i] = 0;

        int max_check_line = BOARD_HEIGHT - 1;
        for (int y = 0; y < max_check_line; y++)//�������Ɍ���
        {
            for (int x = 0; x < BOARD_WIDTH; x++)
            {
                if (_board[y, x] == 0) continue;

                int dest = dests[x];
                dests[x] = y + 1;//��̂Ղ悪������Ȃ玩���̏�

                if (y == 0) continue;

                if (_board[y - 1, x] != 0) continue;//��������Ȃ�ΏۊO

                _falls.Add(new FallData(x, y, dest));

                //�f�[�^�̕ύX
                _board[dest, x] = _board[y, x];
                _board[y, x] = 0;
                _Puyos[dest, x] = _Puyos[y, x];
                _Puyos[y, x] = null;

                dests[x] = dest + 1; //���̂��̂͗���������ɏ�ɏ��
            }
        }

        return _falls.Count != 0;
    }

    public bool Fall()
    {
        _fallFrames++;

        float dy = _fallFrames / (float)FALL_FRAME_PAR_CELL;
        int di = (int)dy;

        for (int i = _falls.Count - 1; i >= 0; i--)//���[�v���ɍ폜���Ă����S�Ȃ悤�Ɍ�납�猟��
        {
            FallData f = _falls[i];

            Vector3 pos = _Puyos[f.Dest, f.X].transform.localPosition;
            pos.y = f.Y - dy;

            if (f.Y <= f.Dest + di)
            {
                pos.y = f.Dest;
                _falls.RemoveAt(i);
            }
            _Puyos[f.Dest, f.X].transform.localPosition = pos;//�\���ʒu�̍X�V
        }

        return _falls.Count != 0;
    }

    static readonly uint[] chainBonusTbl = new uint[]
    {
        0,8,16,32,64,
        96,128,160,192,224,
        256,288,320,352,384,
        416,448,480,512
    };

    static readonly uint[] connectBonusTbl = new uint[]
    {
        0,0,0,0,0,2,3,4,5,6,7
    };

    static readonly uint[] colorBonusTbl = new uint[]
    {
        0,3,6,12,24
    };

    static readonly Vector2Int[] search_tbl = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    public bool CheckErase(int chainCount)
    {
        _eraseFrames = 0;
        _erases.Clear();

        uint[] isChecked = new uint[BOARD_HEIGHT];//�������𑽂��g���͖̂��ʂȂ��߃r�b�g����

        //���_�v�Z�p
        int puyoCount = 0;
        uint colorBits = 0;
        uint connectBonus = 0;

        List<Vector2Int> addList = new();
        for (int y = 0; y < BOARD_HEIGHT; y++)
        {
            for (int x = 0; x < BOARD_WIDTH; x++)
            {
                if ((isChecked[y] & (1u << x)) != 0) continue;

                isChecked[y] |= (1u << x);

                int type = _board[y, x];
                if (type == 0) continue;//���

                puyoCount++;

                System.Action<Vector2Int> getConnection = null;//�ċA�̍ۂɕK�v
                getConnection = (pos) =>
                {
                    addList.Add(pos);//�폜�Ώ�

                    foreach (Vector2Int d in search_tbl)
                    {
                        Vector2Int target = pos + d;
                        if (target.x < 0 || BOARD_WIDTH <= target.x
                         || target.y < 0 || BOARD_HEIGHT <= target.y) continue;//�͈͊O
                        if (_board[target.y, target.x] != type) continue;//�F���Ⴄ
                        if ((isChecked[target.y] & (1u << target.x)) != 0) continue;//�����ς�

                        isChecked[target.y] |= (1u << target.x);
                        getConnection(target);
                    }
                };

                addList.Clear();
                getConnection(new Vector2Int(x, y));

                if (addList.Count >= 4)
                {
                    connectBonus += connectBonusTbl[System.Math.Min(addList.Count, connectBonusTbl.Length - 1)];
                    colorBits |= (1u << type);
                    _erases.AddRange(addList);
                }
            }
        }

        if (chainCount != -1)//���������ȊO�Ȃ�
        {
            //�{�[�i�X�v�Z
            uint colorNum = 0;
            for (; 0 < colorBits; colorBits >>= 1)
            {
                colorNum += (colorBits & 1u);
            }

            uint colorBonus = colorBonusTbl[System.Math.Min(colorNum, colorBonusTbl.Length - 1)];
            uint chainBonus = chainBonusTbl[System.Math.Min(chainCount, chainBonusTbl.Length - 1)];
            uint bonus = System.Math.Max(1, chainBonus + connectBonus + colorBonus);
            _additiveScore += 10 * (uint)_erases.Count * bonus;

            if (puyoCount == 0) _additiveScore += 1800;//�S�����{�[�i�X
        }

        return _erases.Count != 0;
    }

    public bool Erase()
    {
        _eraseFrames++;

        //1���瑝���Ă�����Ƃ�����傫���Ȃ��Ă��̂��Ə������Ȃ��ď�����itween�j
        float t = _eraseFrames * Time.deltaTime;
        t = 1f - 10f * ((t - 0.1f) * (t - 0.1f) - 0.1f * 0.1f);
        //�傫�������Ȃ�I���
        if (t <= 0f)
        {
            foreach (Vector2Int d in _erases)
            {
                Destroy(_Puyos[d.y, d.x]);
                _Puyos[d.y, d.x] = null;
                _board[d.y, d.x] = 0;
            }

            return false;
        }

        //�傫����ύX
        foreach (Vector2Int d in _erases)
        {
            _Puyos[d.y, d.x].transform.localScale = Vector3.one * t;
        }

        return true;
    }

    public uint PopScore()
    {
        uint score = _additiveScore;
        _additiveScore = 0;

        return score;
    }
}
