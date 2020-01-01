using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MergeChain
{
    public class MatchItem : MonoBehaviour
    {

        public static MatchItem Instance { get; private set; }

        [Header("Reference Objects")]
        public ParticleSystem par;

        [SerializeField]
        private Text displayText;

        public Cell cell;
        public ItemPoint point;

        LineRenderer segmentBetweenItems;

        public int score { get; private set; }

        [HideInInspector]
        public int ID;

        SpriteRenderer outLineImage;
        SpriteRenderer itemImage;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            segmentBetweenItems = GetComponent<LineRenderer>();
            outLineImage = transform.GetChild(0).GetComponent<SpriteRenderer>();
            itemImage = transform.GetComponent<SpriteRenderer>();
        }

        void SetupSegmentLine()
        {
            segmentBetweenItems.sortingLayerName = "Default";
            segmentBetweenItems.sortingOrder = 3;
        }

        void Start()
        {
            Color outLineColor = ItemDataManager.Instance.outLineColor;
            outLineImage.color = outLineColor;
            Color textColor = ItemDataManager.Instance.displayContentColor;
            displayText.color = textColor;
        }

        public void SetItemData(ItemData itemData)
        {
            score = itemData.score;
            switch (GridItemManager.Instance.displayWay)
            {
                case DisplayContent.Numbers:
                    displayText.text = itemData.number.ToString();
                    break;
                case DisplayContent.Characters:
                    displayText.text = itemData.character.ToString();
                    break;
            }

        }

        private void OnDrawGizmos()
        {
            Debug.DrawRay(cell.positionInParent, Vector3.up * 0.5f, Color.magenta);
            Debug.DrawRay(cell.positionInParent, Vector3.left * 0.5f, Color.magenta);
        }

        public void LevelUp()
        {
            StartCoroutine(CR_ScaleUpnDown());
            StartCoroutine(CR_FlickColor());
        }

        IEnumerator CR_ScaleUpnDown()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 levelUpScale = originalScale * GridItemManager.Instance.levelUpScaleIndex;
            float speedScale = ((levelUpScale.x - originalScale.x) / GridItemManager.Instance.levelUpTime) * 2;

            while (transform.localScale.x < levelUpScale.x)
            {
                transform.localScale += Vector3.one * speedScale * Time.deltaTime;
                yield return null;
            }
            transform.localScale = levelUpScale;
            while (transform.localScale.x > originalScale.x)
            {
                transform.localScale -= Vector3.one * speedScale * Time.deltaTime;
                yield return null;
            }
       
            transform.localScale = originalScale;
        }

        IEnumerator CR_FlickColor()
        {
            Color originalColor = itemImage.color;
            Color levelUpColor = Color.Lerp(originalColor, Color.white, 0.5f);
            bool isSwitched = false;
            float effecTimetOut = GridItemManager.Instance.levelUpTime;
            float switchTimeOut = effecTimetOut / 10;
            float t1 = 0;
            float t2 = 0;
            while (t1 < effecTimetOut)
            {
                t2 += Time.deltaTime;
                if (t2 >= switchTimeOut)
                {
                    if (isSwitched)
                        itemImage.color = originalColor;
                    else
                        itemImage.color = levelUpColor;
                    isSwitched = !isSwitched;
                    t2 = 0;
                }

                t1 += Time.deltaTime;
                yield return null;
            }
            itemImage.color = originalColor;
        }
    }
}