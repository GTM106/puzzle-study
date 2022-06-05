using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController
{
    private int _time = 0;
    private float _inv_time_max = 1.0f;

    public void Set(int max_time)
    {
        Debug.Assert(max_time > 0.0f);

        _time = max_time;
        _inv_time_max = 1.0f / max_time;
    }

    //アニメーション中ならtrueを返す
    public bool Update()
    {
        //_timeを一つずつ減らして0未満にならないようにMaxメソッドでクランプする
        _time = Math.Max(--_time, 0);
        return (0 < _time);
    }

    public float GetNormalized() => _time * _inv_time_max;
}
