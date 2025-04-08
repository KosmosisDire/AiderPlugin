using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class MainThread
{
    static Queue<Action> _actions = new Queue<Action>();
    static MainThread instance;

    static MainThread()
    {
        if (instance == null)
        {
            instance = new MainThread();
            EditorApplication.update += Update;
        }
    }

    static void Update()
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
        lock (_actions)
        {
            _actions.Enqueue(action);
        }
    }

    public static void Log(string message)
    {
        RunOnMainThread(() => Debug.Log(message));
    }

    public static void LogError(string message)
    {
        RunOnMainThread(() => Debug.LogError(message));
    }

    public static void LogWarning(string message)
    {
        RunOnMainThread(() => Debug.LogWarning(message));
    }
    
}
