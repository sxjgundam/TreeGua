using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MergeChain
{
    [System.Serializable]
    public struct ItemData
    {
        public Color color;
        public int number;
        public char character;
        public int score;


    }

    public class ItemDataManager : MonoBehaviour
    {
        public static ItemDataManager Instance { get; private set; }

        public Color displayContentColor;

        public Color outLineColor;

        public List<ItemData> ItemDataList;

        void Awake()
        {
            if (Instance)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}