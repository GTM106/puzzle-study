using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogicalInput
{
    [Flags]
    public enum Key
    {
        Right = 1 << 0,
        Left = 1 << 1,
        RotR = 1 << 2,
        RotL = 1 << 3,
        QuickDrop = 1 << 4,
        Down = 1 << 5,

        Max = 6,//個数
    }

    const int KEY_REPEAT_START_TIME = 12;//押しっぱなしでキーリピートに入るフレーム数
    const int KEY_REPEAT_ITERATION_TIME = 1;//キーリピートに入ったあとの更新フレーム数

    Key inputRaw;//現在の値
    Key inputTrg;//入力が入ったときの値
    Key inputRel;//入力が抜けたときの値
    Key inputRep;//連続入力
    int[] _trgWaitingTime = new int[(int)Key.Max];

    public bool IsRaw(Key k) => inputRaw.HasFlag(k);
    public bool IsTrigger(Key k) => inputTrg.HasFlag(k);
    public bool IsRelease(Key k) => inputRel.HasFlag(k);
    public bool IsRepeat(Key k) => inputRep.HasFlag(k);

    public void Clear()
    {
        inputRaw = 0;
        inputTrg = 0;
        inputRel = 0;
        inputRep = 0;
        for (int i = 0; i < (int)Key.Max; i++)
        {
            _trgWaitingTime[i] = 0;
        }
    }

    public void Update(Key inputDev)
    {
        //入力が入った/抜けた
        inputTrg = (inputDev ^ inputRaw) & inputDev;
        inputRel = (inputDev ^ inputRaw) & inputRaw;

        //生データの生成
        inputRaw = inputDev;

        //キーリピートの生成
        inputRep = 0;
        for (int i = 0; i < (int)Key.Max; i++)
        {
            if (inputTrg.HasFlag((Key)(1 << i)))
            {
                inputRep |= (Key)(1 << i);
                _trgWaitingTime[i] = KEY_REPEAT_START_TIME;
            }
            else if (inputRaw.HasFlag((Key)(1 << i)))
            {
                if (--_trgWaitingTime[i] <= 0)
                {
                    inputRep |= (Key)(1 << i);
                    _trgWaitingTime[i] = KEY_REPEAT_ITERATION_TIME;
                }
            }
        }
    }
}
