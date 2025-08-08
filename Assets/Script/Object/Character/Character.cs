using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Character : MonoBehaviour
{

    public Status status { get; protected set; }

    public event Action<Character> OnDied;

    public virtual void TakeDamage(float damage)
    {
        status.ModifyHP(-damage);

        if (status.HP <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        OnDied?.Invoke(this);
        //Die 이벤트 발생시키기.
    }
}
