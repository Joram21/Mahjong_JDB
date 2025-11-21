namespace Config_Assets
{
    using UnityEngine;

    public class FakeLoading : MonoBehaviour
    {
        public float timestamp;
        public void Open(float thet = 3)
        {
            timestamp += thet;
            if (timestamp > 3)
            {
                timestamp = 3;
            }
            gameObject.SetActive(true);
        }
        [ContextMenu("TestLoad")]
        void TestLoading()
        {
            Open(5);
        }
        void Update()
        {
            if (ExtraMan.Instance&&!ExtraMan.Instance.games_Catalog.IsLoaded)
                return;
            if (timestamp < 0)
            {
                gameObject.SetActive(false);
            }
            else
            {
                timestamp -= Time.deltaTime;
            }
        }
    }
}
