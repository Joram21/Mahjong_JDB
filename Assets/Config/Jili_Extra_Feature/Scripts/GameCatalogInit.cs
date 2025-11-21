namespace Config_Assets
{
    using UnityEngine;

    public class GameCatalogInit : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            ExtraMan.Instance.games_Catalog.Init();
        }


    }
}
