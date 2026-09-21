using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// Thread-safe dispatcher that runs background actions on the Unity main thread.
    /// Moved into the YawVR namespace as part of the SOLID refactor (it previously
    /// lived in the global namespace) — safe because this class has no serialized
    /// fields for Unity to lose track of.
    /// </summary>
    public class ActionBus : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> actionQueue = new();
        private static ActionBus instance;
        private static bool isQuitting = false;

        public static ActionBus Instance
        {
            get
            {
                if (isQuitting) return null;

                if (instance == null)
                {
                    throw new Exception("[ActionBus] Instance is null. Ensure ActionBus is present in the scene.");
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                isQuitting = true;
            }
        }

        private void Update()
        {
            if (actionQueue.IsEmpty) return;

            int actionsToRun = actionQueue.Count;
            int executed = 0;

            while (executed < actionsToRun && actionQueue.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ActionBus] Error executing action: {e}");
                }

                executed++;
            }
        }

        public void Add(Action action)
        {
            if (isQuitting || action == null) return;
            actionQueue.Enqueue(action);
        }
    }
}
