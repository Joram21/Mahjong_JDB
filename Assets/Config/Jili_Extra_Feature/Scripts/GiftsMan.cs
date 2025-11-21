namespace Config_Assets
{
    using UnityEngine;

    public class GiftsMan : MonoBehaviour
    {
        public float[] FreeMaxGiftPoints;
        [ContextMenu("AddGiftPoints")]
        void TestAddGiftPoints()
        {
            AddGiftPoints(FreeMaxGiftPoints[PlayerPrefs.GetInt("ActiveGiftLevel")] / 5);
        }
        public void AddGiftPoints(float Amount = 500)
        {
            int TheActiveLevel = PlayerPrefs.GetInt("ActiveGiftLevel");
            float NewAmount = PlayerPrefs.GetFloat("TotalGiftPoints_" + TheActiveLevel.ToString()) + Amount;
            if (NewAmount >= FreeMaxGiftPoints[TheActiveLevel])
            {
                NewAmount = FreeMaxGiftPoints[TheActiveLevel];
                PlayerPrefs.SetInt("GiftUnlocked" + TheActiveLevel.ToString(), 1);

                //NextLevel

            }
            PlayerPrefs.SetFloat("TotalGiftPoints_" + TheActiveLevel.ToString(), NewAmount);

            FreeGiftBtn[] btns = FindObjectsOfType<FreeGiftBtn>();
            for (int i = 0; i < btns.Length; i++)
            {
                btns[i].Refresh();
            }
        }
        public float GetGiftPoints(int TheLevel)
        {
            return PlayerPrefs.GetFloat("TotalGiftPoints_" + TheLevel.ToString());
        }
        public bool IsGiftUnlocked(int TheLevel)
        {
            if (PlayerPrefs.GetInt("GiftUnlocked" + TheLevel.ToString()) == 1)
            {
                return true;
            }
            return false;
        }
        public void GoToNextLevel()
        {
            int TheActiveLevel = PlayerPrefs.GetInt("ActiveGiftLevel");
            if (TheActiveLevel < FreeMaxGiftPoints.Length - 1)
            {
                PlayerPrefs.SetInt("ActiveGiftLevel", TheActiveLevel + 1);
            }
        }
        public int GetJiliLevel()
        {
            int TheActiveLevel = PlayerPrefs.GetInt("ActiveGiftLevel");
            if (TheActiveLevel >= FreeMaxGiftPoints.Length - 1 && PlayerPrefs.GetInt("ClaimGiftPoints_" + TheActiveLevel.ToString()) == 1
                )
            {
                TheActiveLevel = FreeMaxGiftPoints.Length;
            }
            return TheActiveLevel;

        }
    }
}