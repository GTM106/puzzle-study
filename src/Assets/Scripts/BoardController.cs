using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct FallData
{
    public readonly int X { get; }
    public readonly int Y { get; }
    public readonly int Dest { get; }//落ちる先
    public FallData(int x, int y, int dest)
    {
        X = x;
        Y = y;
        Dest = dest;
    }
}

public class BoardController : MonoBehaviour
{
    public const int FALL_FRAME_PAR_CELL = 5;//単位セルあたりの落下FRAME数
    public const int BOARD_WIDTH = 6;
    public const int BOARD_HEIGHT = 14;

    [SerializeField] GameObject prefabPuyo = default!;

    int[,] _board = new int[BOARD_HEIGHT, BOARD_WIDTH];
    GameObject[,] _Puyos = new GameObject[BOARD_HEIGHT, BOARD_WIDTH];

    uint _additiveScore = 0;

    //落ちる際の一時的変数
    List<FallData> _falls = new();
    int _fallFrames = 0;

    //削除する際の一時的変数
    List<Vector2Int> _erases = new();
    int _eraseFrames = 0;

    void Start()
    {
        ClearAll();
    }

    /// <summary>
    /// 盤面をクリアする
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
    /// _boardに値を設定する
    /// </summary>
    /// <param name="pos">設置座標</param>
    /// <param name="val">Puyoのtype</param>
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

        //落ちる先の高さの記録用
        int[] dests = new int[BOARD_WIDTH];
        for (int i = 0; i < BOARD_WIDTH; i++) dests[i] = 0;

        int max_check_line = BOARD_HEIGHT - 1;
        for (int y = 0; y < max_check_line; y++)//下から上に検索
        {
            for (int x = 0; x < BOARD_WIDTH; x++)
            {
                if (_board[y, x] == 0) continue;

                int dest = dests[x];
                dests[x] = y + 1;//上のぷよが落ちるなら自分の上

                if (y == 0) continue;

                if (_board[y - 1, x] != 0) continue;//下があるなら対象外

                _falls.Add(new FallData(x, y, dest));

                //データの変更
                _board[dest, x] = _board[y, x];
                _board[y, x] = 0;
                _Puyos[dest, x] = _Puyos[y, x];
                _Puyos[y, x] = null;

                dests[x] = dest + 1; //次のものは落ちたさらに上に乗る
            }
        }

        return _falls.Count != 0;
    }

    public bool Fall()
    {
        _fallFrames++;

        float dy = _fallFrames / (float)FALL_FRAME_PAR_CELL;
        int di = (int)dy;

        for (int i = _falls.Count - 1; i >= 0; i--)//ループ中に削除しても安全なように後ろから検索
        {
            FallData f = _falls[i];

            Vector3 pos = _Puyos[f.Dest, f.X].transform.localPosition;
            pos.y = f.Y - dy;

            if (f.Y <= f.Dest + di)
            {
                pos.y = f.Dest;
                _falls.RemoveAt(i);
            }
            _Puyos[f.Dest, f.X].transform.localPosition = pos;//表示位置の更新
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

        uint[] isChecked = new uint[BOARD_HEIGHT];//メモリを多く使うのは無駄なためビット処理

        //得点計算用
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
                if (type == 0) continue;//空間

                puyoCount++;

                System.Action<Vector2Int> getConnection = null;//再帰の際に必要
                getConnection = (pos) =>
                {
                    addList.Add(pos);//削除対象

                    foreach (Vector2Int d in search_tbl)
                    {
                        Vector2Int target = pos + d;
                        if (target.x < 0 || BOARD_WIDTH <= target.x
                         || target.y < 0 || BOARD_HEIGHT <= target.y) continue;//範囲外
                        if (_board[target.y, target.x] != type) continue;//色が違う
                        if ((isChecked[target.y] & (1u << target.x)) != 0) continue;//検索済み

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

        if (chainCount != -1)//初期化時以外なら
        {
            //ボーナス計算
            uint colorNum = 0;
            for (; 0 < colorBits; colorBits >>= 1)
            {
                colorNum += (colorBits & 1u);
            }

            uint colorBonus = colorBonusTbl[System.Math.Min(colorNum, colorBonusTbl.Length - 1)];
            uint chainBonus = chainBonusTbl[System.Math.Min(chainCount, chainBonusTbl.Length - 1)];
            uint bonus = System.Math.Max(1, chainBonus + connectBonus + colorBonus);
            _additiveScore += 10 * (uint)_erases.Count * bonus;

            if (puyoCount == 0) _additiveScore += 1800;//全消しボーナス
        }

        return _erases.Count != 0;
    }

    public bool Erase()
    {
        _eraseFrames++;

        //1から増えてちょっとしたら大きくなってそのあと小さくなって消える（tween）
        float t = _eraseFrames * Time.deltaTime;
        t = 1f - 10f * ((t - 0.1f) * (t - 0.1f) - 0.1f * 0.1f);
        //大きさが負なら終わり
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

        //大きさを変更
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
