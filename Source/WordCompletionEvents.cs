using System;
using UnityEngine;

public static class WordCompletionEvents
{
    public static event Action<Transform> OnWordCompleted;

    public static void TriggerWordCompleted(Transform target)
    {
        if (target != null)
            OnWordCompleted?.Invoke(target);
    }
}
