namespace Config_Assets
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using UnityEngine.Events;
    [System.Serializable]
    public class ColorAnimEnd : UnityEvent { }
    [System.Serializable]
    public class Color_animdata
    {
        public float TargetTime = 0.1f;
        public float Speed = 2f;
        public Color TargetColor = Color.white;
    }

    public class ColorAnim : MonoBehaviour
    {
        public bool IsRandom;
        public bool _IsLoop = true;
        float timestamp;
        public int Target;
        public float StartOffsetTime;
        public Color_animdata[] TargetData;
        public Image TheImg;
        public TMP_Text TheText;
        public Outline TheOutline;
        Color TheColor;
        float Speed;
        public ColorAnimEnd OnColorAnimEnd;
        private void OnEnable()
        {
            ResetAnim();
        }
        public void ResetAnim()
        {
            if (IsRandom)
            {
                for (int i = 0; i < TargetData.Length; i++)
                {
                    TargetData[i].TargetTime = Random.Range(0.1f, 0.5f);
                }
            }
            Target = 0;
            timestamp = Time.time + TargetData[Target].TargetTime + StartOffsetTime;
            TheColor = TargetData[Target].TargetColor;
            Speed = TargetData[Target].Speed;
            if (TheImg)
            {
                TheImg.color = TheColor;
            }
            if (TheText)
            {
                TheText.color = TheColor;
            }
            if (TheOutline)
            {
                TheOutline.effectColor = TheColor;
            }
        }
        void Update()
        {
            if (timestamp < Time.time)
            {
                if (Target == TargetData.Length)
                {
                    if (_IsLoop)
                    {
                        ResetAnim();
                    }
                    OnColorAnimEnd.Invoke();


                }
                else
                {
                    timestamp = Time.time + TargetData[Target].TargetTime;
                    TheColor = TargetData[Target].TargetColor;

                    Speed = TargetData[Target].Speed;
                    Target += 1;
                }


            }
            if (TheImg)
            {
                TheImg.color = Vector4.Lerp(TheImg.color, TheColor, Speed * Time.deltaTime);
            }
            if (TheText)
            {
                TheText.color = Vector4.Lerp(TheText.color, TheColor, Speed * Time.deltaTime);
            }
            if (TheOutline)
            {
                TheOutline.effectColor = Vector4.Lerp(TheOutline.effectColor, TheColor, Speed * Time.deltaTime);
            }
        }
    }
}