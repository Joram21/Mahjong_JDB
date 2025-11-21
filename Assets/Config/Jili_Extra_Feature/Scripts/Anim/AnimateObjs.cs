namespace Config_Assets
{
    using UnityEngine;
    using UnityEngine.Events;
    [System.Serializable]
    public class AnimateObjsEnd : UnityEvent { }
    [System.Serializable]
    public class AnimateObjsStart : UnityEvent { }
    public class AnimateObjs : MonoBehaviour
    {
        public bool IsLoop = true;
        public float DelayStart;
        float DelayStartTimestamp;
        public float Rate = 0.1f;
        public GameObject[] Objs;
        float Timestamp;
        int Active;
        public AnimateObjsStart OnStart;
        public AnimateObjsEnd OnEnd;
        private void OnEnable()
        {
            ResetAnim();
        }
        void Update()
        {
            if ( Time.time < DelayStartTimestamp)
                return;
            if (Timestamp < Time.time)
            {
                for (int i = 0; i < Objs.Length; i++)
                {
                    if (i == Active)
                    {
                        Objs[i].SetActive(true);
                    }
                    else
                    {
                        Objs[i].SetActive(false);
                    }
                }
                Timestamp = Time.time + Rate;
                Active += 1;
                if (Active > Objs.Length - 1)
                {
                    if (IsLoop)
                    {
                        //Active = 0;
                        ResetAnim();
                    }
                    else
                    {
                        Active = Objs.Length - 1;
                    }
                    OnEnd.Invoke();
                }
            }

        }
        public void ResetAnim()
        {
            DelayStartTimestamp = Time.time + DelayStart;
            Active = 0;
            for (int i = 0; i < Objs.Length; i++)
            {
                if (i == Active)
                {
                    Objs[i].SetActive(true);
                }
                else
                {
                    Objs[i].SetActive(false);
                }
            }
            OnStart.Invoke();
        }
    }
}