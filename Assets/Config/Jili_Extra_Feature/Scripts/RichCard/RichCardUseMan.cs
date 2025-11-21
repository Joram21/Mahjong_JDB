namespace Config_Assets
{
    using UnityEngine;

    public class RichCardUseMan : MonoBehaviour
    {
        public GameObject ThePref;
        public Transform SpawnPos;
        public Transform TargetPos;
        public GameObject ExplosionPref;
        private void Start()
        {
            TargetPos.gameObject.SetActive(false);
        }
        [ContextMenu("Spawn")]
        public void Spawn()
        {
            GameObject Go = Instantiate(ThePref, SpawnPos);
            Go.GetComponent<RichCardUse>().TargetPos = TargetPos;
        }
        public void SpawnExplosion(Vector2 ThePos)
        {
            GameObject Go = Instantiate(ExplosionPref, ThePos, Quaternion.identity, SpawnPos);
            Destroy(Go, 1f);
        }
    }
}
