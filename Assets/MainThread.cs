using System;
using System.Collections.Generic;
using UnityEngine;


public class MainThread : MonoBehaviour
{
    private Queue<Action> _actions = new Queue<Action>();
    static MainThread instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        lock (_actions)
        {
            while (_actions.Count > 0)
            {
                _actions.Dequeue().Invoke();
            }
        }
    }

    public static void RunOnMainThread(Action action)
    {
        lock (instance._actions)
        {
            instance._actions.Enqueue(action);
        }
    }
    
}
