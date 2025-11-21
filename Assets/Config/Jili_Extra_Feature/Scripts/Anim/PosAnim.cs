namespace Config_Assets
{
    using UnityEngine;
    using UnityEngine.Events;
    [System.Serializable]
    public class PosAnimEnd : UnityEvent { }

    [System.Serializable]
    public class Posanimdata
    {
        public float TargetTime = 0.1f;
        public float Speed = 2f;
        public Vector3 TargetPos = Vector3.one;
        public float ScaleSpeed = 2f;
        public Vector3 TargetScale = Vector3.one;
    }
    public class PosAnim : MonoBehaviour
    {
        public bool UseScale;
        public bool IsLoop = true;
        float timestamp;
        public int Target;
        public bool ResetSnap = true;

        public Posanimdata[] TargetData;
        Vector3 ThePos;
        Vector3 TheScale;
        float Speed;
        float ScaleSpeed;
        public int SnapToEndCount;
        public Transform TargetTrans;
        public PosAnimEnd OnPosAnimEnd;
        private void OnEnable()
        {
            ResetAnim();
        }
        public void ResetAnim()
        {

            if (SnapToEndCount > 0)
            {
                SnapToEndCount -= 1;
                Target = TargetData.Length-1;


            }
            else
            {
                Target = 0;

            }
            ThePos = TargetData[Target].TargetPos;
            timestamp = Time.time + TargetData[Target].TargetTime;
            if (ResetSnap)
            {
                TargetTrans.localPosition = ThePos;

            }


            if (UseScale)
            {
                TheScale = TargetData[Target].TargetScale;
                TargetTrans.localScale = TargetData[Target].TargetScale;
            }

        }
        void Update()
        {
            if (timestamp < Time.time)
            {


                if (Target == TargetData.Length)
                {
                    if (IsLoop)
                    {
                        ResetAnim();

                    }
                    OnPosAnimEnd.Invoke();
                }
                else
                {
                    timestamp = Time.time + TargetData[Target].TargetTime;
                    ThePos = TargetData[Target].TargetPos;
                    TheScale = TargetData[Target].TargetScale;
                    Speed = TargetData[Target].Speed;
                    ScaleSpeed = TargetData[Target].ScaleSpeed;
                    Target += 1;
                }
            }
            TargetTrans.localPosition = Vector3.Lerp(TargetTrans.localPosition, ThePos, Speed * Time.deltaTime);
            if (UseScale)
            {
                TargetTrans.localScale = Vector3.Slerp(TargetTrans.localScale, TheScale, ScaleSpeed * Time.deltaTime);
            }
        }

    }
}