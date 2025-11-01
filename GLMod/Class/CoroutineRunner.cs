using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Injection;
using System.Collections;
using UnityEngine;

namespace GLMod.Class
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;

        public static void Init()
        {
            if (_instance != null) return;

            // Enregistrement du type pour IL2CPP
            ClassInjector.RegisterTypeInIl2Cpp<CoroutineRunner>();

            GameObject go = new GameObject("CoroutineRunner");
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<CoroutineRunner>();
        }

        public static Coroutine Run(IEnumerator routine)
        {
            if (_instance == null)
                Init();

            return _instance.StartCoroutine(routine);
        }
    }
}