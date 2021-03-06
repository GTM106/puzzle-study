using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PuyoType
{
    Blank = 0,

    Green = 1,
    Red = 2,
    Yellow = 3,
    Blue = 4,
    Purple = 5,
    Cyan = 6,

    Invalid= 7,
}

[RequireComponent(typeof(Renderer))]
public class PuyoController : MonoBehaviour
{
    static readonly Color[] color_table = new Color[]
    {
        Color.black,

        Color.green,
        Color.red,
        Color.yellow,
        Color.blue,
        Color.magenta,
        Color.cyan,

        Color.gray,
    };

    //自分自身のマテリアルを登録することでGetComponentをなくす
    [SerializeField] private Renderer my_renderer = default!;
    private PuyoType _type = PuyoType.Invalid;

    public void SetPuyoType(PuyoType type)
    {
        _type = type;

        my_renderer.material.color = color_table[(int)_type];
    }

    public PuyoType GetPuyoType() => _type;

    public void SetPos(Vector3 pos)
    {
        this.transform.localPosition = pos;
    }
}
