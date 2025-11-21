
using UnityEngine;
namespace Config_Assets
{
    using UnityEngine.UI;
    public class CanvasScaleHelper : MonoBehaviour
    {
        public bool IsLandScape;
        public Vector2 CurrentScale;
        Vector2 RefScale = new Vector2(1280, 720);
        public Vector2 ScaleMultiplier;
        public float NewScaleMultiplier;
        public float ScaleFactor;
        public CanvasScaler canvasScaler;
        AnimationCurve ScaleCurve = new AnimationCurve();
        private void Start()
        {
            ScaleCurve.AddKey(0, 1.5f);
            ScaleCurve.AddKey(1, 1);
        }
        private void Update()
        {
            CurrentScale = new Vector2(Screen.width, Screen.height);
            ScaleMultiplier.x = RefScale.x / CurrentScale.x;
            ScaleMultiplier.y = RefScale.y / CurrentScale.y;
            NewScaleMultiplier = (ScaleMultiplier.x / ScaleMultiplier.y);
            if (NewScaleMultiplier > 1.21f)
            {
                IsLandScape = false;
              
            }
            else
            {
                IsLandScape = true;
               
            }
            if (canvasScaler)
            {
                ScaleFactor = ((3.2f / NewScaleMultiplier));
                ScaleFactor *= ScaleCurve.Evaluate(ScaleFactor);
                if (NewScaleMultiplier < 3.2f)
                {
                    canvasScaler.matchWidthOrHeight = 1;
                    if (IsLandScape)
                    {
                        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

                    }
                    else
                    {
                        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

                    }
                }
                else
                {
                    canvasScaler.matchWidthOrHeight = ScaleFactor;

                    canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                }
            }

        }
    }
}
