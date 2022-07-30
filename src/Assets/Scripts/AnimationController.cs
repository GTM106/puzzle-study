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

    //�A�j���[�V�������Ȃ�true��Ԃ�
    public bool Update()
    {
        //_time��������炵��0�����ɂȂ�Ȃ��悤��Max���\�b�h�ŃN�����v����
        _time = Math.Max(--_time, 0);
        return (0 < _time);
    }

    public float GetNormalized() => _time * _inv_time_max;
}
