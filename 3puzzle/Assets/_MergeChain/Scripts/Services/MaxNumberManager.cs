using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MergeChain
{
    public class MaxNumberManager : MonoBehaviour
    {

        public static MaxNumberManager Instance{ get; private set; }

        //	public bool isResetMaxNumber;

        public int MaxNumber{ get; private set; }

        public int HighMaxNumber{ get; private set; }

        public bool HasNewHighMaxNumber{ get; private set; }

        public static event Action<int> MaxNumberUpdated=delegate{};
        public static event Action<int> HighMaxNumberUpdated=delegate{};

        private const string HIGHMAXNUMBER = "High max number";
        //key name to store high max number in PlayerPrefs

        void Awake()
        {
            if (Instance)
                DestroyImmediate(gameObject);
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        void Start()
        {
//		if (isResetMaxNumber)
            Reset();		
        }

        public void Reset()
        {
            MaxNumber = 0;
            HighMaxNumber = PlayerPrefs.GetInt(HIGHMAXNUMBER, 0);
            HasNewHighMaxNumber = false;
        }

        public void UpdateMaxNumber(int newValue)
        {
            MaxNumber = newValue;
            MaxNumberUpdated(MaxNumber);
            if (MaxNumber > HighMaxNumber)
            {
                UpdateHighMaxNumber(MaxNumber);
                HasNewHighMaxNumber = true;
            }
            else
                HasNewHighMaxNumber = false;
        }

        public void UpdateHighMaxNumber(int newHighValue)
        {
            //Update high maxnumber if player has made a new one
            if (newHighValue > HighMaxNumber)
            {
                HighMaxNumber = newHighValue;
                PlayerPrefs.SetInt(HIGHMAXNUMBER, HighMaxNumber);
                HighMaxNumberUpdated(HighMaxNumber);
            }
        }

    }
}