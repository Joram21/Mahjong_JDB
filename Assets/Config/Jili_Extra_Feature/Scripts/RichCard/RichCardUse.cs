namespace Config_Assets
{
    using UnityEngine;
    using UnityEngine.UI;
    public class RichCardUse : MonoBehaviour
    {
        public string TheName = "Game_A";
        public Image TheImg;
        public Transform TargetPos;
        bool CanMove;
        private void Start()
        {
            UpdateImage(ExtraMan.Instance.games_Catalog.GetSavedRichCard(ExtraMan.Instance.GameId));
        }
        public void UpdateImage(Sprite _S)
        {
            TheImg.sprite = _S;
        }
        private void Update()
        {
            if (CanMove)
            {
                transform.position = Vector3.Lerp(transform.position, TargetPos.position, 5 * Time.deltaTime);
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 5 * Time.deltaTime);
                float TheDist = Vector2.Distance(transform.position, TargetPos.position);
                if (TheDist < 10)
                {
                    ExtraMan.Instance.richCardMan.richCardUseMan.SpawnExplosion(transform.position);
                    ExtraMan.Instance.richCardMan.AddAvailableRichCards();
                    Destroy(gameObject);
                }
            }
        }
        public void ActivateMove()
        {
            CanMove = true;
        }
    }
}
