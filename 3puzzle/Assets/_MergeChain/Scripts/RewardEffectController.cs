using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MergeChain
{
    public class RewardEffectController : MonoBehaviour
    {

        public Text scoreText;
        public Text powerUpText;

        public float speedMoveUp = 3.5f;

        void OnEnable()
        {
            StartCoroutine(CR_MoveUp());
        }

        IEnumerator CR_MoveUp()
        {
            float timeOut = 0.5f;
            float t = 0;
            while (t <= timeOut)
            {
                transform.position += Vector3.up * speedMoveUp * Time.deltaTime;
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }

        public void SetRewardUI(Vector3 position, int score, int powerUp)
        {
            transform.position = position;
            scoreText.text = "+" + score.ToString();
            if (powerUp == 0)
            {
                powerUpText.enabled = false;
            }
            else
            {
                powerUpText.enabled = true;
                powerUpText.text = "+" + powerUp.ToString();
            }
        }
    }
}