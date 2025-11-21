namespace Config_Assets
{
    using UnityEngine;
    using UnityEngine.UI;
    public class ExtraMan : MonoBehaviour
    {
        public int GameId = 1;
        public static ExtraMan Instance;
        public GiftsMan giftsMan;
        public MissionsMan missionsMan;
        public ExtraInfo infoTab;
        public FakeLoading fakeLoading;
        public Games_Catalog games_Catalog;
        public RichCardMan richCardMan;

        //Use this to refresh your scriptss
        public delegate void RefreshAll();
        public RefreshAll refreshAll;


        [Header("StartAnim")]
        public Button OpenButton;
        public Button CloseButton;
        private void Awake()
        {
            if (Instance)
            {
                Destroy(Instance);
            }
            Instance = this;
        }
        private void Start()
        {
            Setup();
        }
        [ContextMenu("StartAnim")]
        public void ShowStartAnim()
        {
            OpenButton.onClick.Invoke();
            Invoke(nameof(FinishAnim), 4f);
        }
        void FinishAnim()
        {
            CloseButton.onClick.Invoke();
        }

        public void ForceRefresh()
        {
            // Debug.Log("Refresh");
            if (refreshAll != null)
            {
                refreshAll.Invoke();
            }
            else
            {
                // Debug.Log("NoDelegatesFound");
            }

        }
        void Setup()
        {
            if (!ConfigMan.Instance.ReceivedConfigs)
            {
                Invoke(nameof(Setup), 1f);
                return;
            }
            GameId = int.Parse(ConfigMan.Instance.GameId);

        }


    }
}
