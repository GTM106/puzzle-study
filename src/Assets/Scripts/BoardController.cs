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

    //落ちる際の一時的変数
    List<FallData> _falls = new();
    int _fallFrames = 0;

    void Start()
    {
        ClearAll();

        //for(int y = 0; y < BOARD_HEIGHT; y++)
        //{
        //    for(int x = 0; x < BOARD_WIDTH; x++)
        //    {
        //        Settle(new Vector2Int(x,y),Random.Range(1, 7));
        //    }
        //}
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
        for(int y=0; y < max_check_line; y++)//下から上に検索
        {
            for(int x=0; x < BOARD_WIDTH; x++)
            {
                if (_board[y, x] == 0) continue;

                int dest = dests[x];
                dests[x] = y + 1;//上のぷよが落ちるなら自分の上

                if (y == 0) continue;

                if (_board[y-1,x] != 0) continue;//下があるなら対象外

                _falls.Add(new FallData(x, y, dest));

                //データの変更
                _board[dest, x] = _board[y, x];
                _board[y, x] = 0;
                _Puyos[dest, x] = _Puyos[y,x];
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

        for(int i = _falls.Count-1; i>=0; i--)//ループ中に削除しても安全なように後ろから検索
        {
            FallData f=_falls[i];

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
}
