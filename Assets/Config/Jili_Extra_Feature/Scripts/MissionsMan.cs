namespace Config_Assets
{
    using UnityEngine;
    using System;
    using UnityEngine.Networking;
    using System.Runtime.InteropServices;
    public class MissionsMan : MonoBehaviour
    {

        public string TheTimeZone = "Africa/Nairobi";
        public float MaxTime = 43200;
        public float TheTime = 7200;
        public float[] MissionGoals;
        float savetimestamp;

        [DllImport("__Internal")]
        private static extern string getLocaltime();


        private void Start()
        {
            //DateTime appStart = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(System.DateTime.Now, "America/New_York");
            //DateTime appStart = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(System.DateTime.Now, "Africa/Nairobi");
            //Debug.Log(appStart);
            // var timeZones = TimeZoneInfo.GetSystemTimeZones();
            // Debug.Log("Timezones_"+timeZones.Count);
            // for(int i = 0; i < 5; i++)
            // {
            //   Debug.Log(timeZones[i].DisplayName);
            //}


            ///TheTime = GetSavedTime();
        }
        public DateTime LocalTime()
        {
            DateTime thetime = new DateTime();
            if (Application.isEditor)
            {

                // Original date string

                // string savedt = "Wed Mar 26 2025 16:04:17 GMT+0300 (East Africa Time)";
                //string cleanDateString = savedt.Split('(')[0].Trim();
                // thetime = ParseString(savedt);
                // Debug.Log(thetime.ToShortDateString());
                // Debug.Log(thetime.ToShortTimeString());
                thetime = System.DateTime.Now;
            }
            else
            {
                string _thetime = getLocaltime();
                //Debug.Log(_thetime);
                thetime = ParseString(_thetime);
            }
            ///DateTime thetime = DateTime.Parse("03-03-2021 23:59");
            return thetime;
        }
        public DateTime ParseString(string TheDate)
        {
            // Original date string
            string dateString = TheDate;

            // Step 1: Clean the string by removing the day of the week and timezone info
            string cleanDateString = dateString.Split('(')[0].Trim(); // Remove anything after the first '('
            cleanDateString = cleanDateString.Replace("GMT", "").Trim(); // Remove the GMT part

            // Step 2: Reformat the string to a standard format (yyyy-MM-dd HH:mm:ss)
            // The clean string should look like: "Mar 26 2025 16:04:17"
            // Convert it to the format "2025-03-26 16:04:17"

            string[] dateParts = cleanDateString.Split(' '); // Split the string by spaces
            string month = dateParts[1]; // "Mar"
            string day = dateParts[2]; // "26"
            string year = dateParts[3]; // "2025"
            string time = dateParts[4]; // "16:04:17"

            // Convert month name to month number
            string monthNumber = DateTime.ParseExact(month, "MMM", System.Globalization.CultureInfo.InvariantCulture).Month.ToString("D2");

            // Construct the formatted string as "yyyy-MM-dd HH:mm:ss"
            string formattedDateString = $"{year}-{monthNumber}-{day} {time}";

            // Step 3: Try parsing the newly formatted string
            DateTime parsedDate = new DateTime();
            if (DateTime.TryParseExact(formattedDateString, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
            {
                // Successfully parsed the date
                // Debug.Log("Parsed DateTime: " + parsedDate);
                return parsedDate;
            }
            else
            {
                // Parsing failed
                // Debug.LogError("Failed to parse date string.");

            }
            return parsedDate;
        }
        public DateTime MidnightTime()
        {
            // TimeSpan.FromHours(24);
            DateTime thetime = DateTime.Parse("03-03-2021 23:59");
            thetime = thetime.AddSeconds(100);
            //DateTime appStart = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(thetime, TheTimeZone);
            DateTime appStart = thetime;
            return appStart;
        }
        [ContextMenu("GetDiff")]
        public TimeSpan GetDiffToMidnight()
        {
            DateTime currenttime = CurrentTime();
            TimeSpan ToMidnight = TimeSpan.FromHours(24) - currenttime.TimeOfDay;
            // Debug.Log(ToMidnight);
            return ToMidnight;
        }
        public DateTime CurrentTime()
        {
            //DateTime appStart = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(System.DateTime.Now,TheTimeZone);
            // DateTime appStart = System.DateTime.Now;
            DateTime appStart = LocalTime();
            return appStart;
        }
        [ContextMenu("AddMissionPoints")]
        void TestAddMissionPoints()
        {
            AddMissionPoints(MissionGoals[PlayerPrefs.GetInt("ActiveMissionLevel")] / 5);
        }
        [ContextMenu("Reset")]
        public void ResetPoints()
        {
            for (int i = 0; i < MissionGoals.Length; i++)
            {
                PlayerPrefs.SetFloat("TotalMissionPoints_" + i.ToString(), 0);
                PlayerPrefs.SetInt("MissionUnlocked" + i.ToString(), 0);
                PlayerPrefs.SetInt("AwardRichCard_MissionLevel_" + i.ToString(), 0);
            }
            PlayerPrefs.SetInt("ActiveMissionLevel", 0);
            Refresh();
        }
        public void AddMissionPoints(float Amount = 500)
        {
            bool gotonextlevel = false;
            int TheActiveLevel = PlayerPrefs.GetInt("ActiveMissionLevel");
            float NewAmount = PlayerPrefs.GetFloat("TotalMissionPoints_" + TheActiveLevel.ToString()) + Amount;
            if (NewAmount >= MissionGoals[TheActiveLevel])
            {
                gotonextlevel = true;
                NewAmount = MissionGoals[TheActiveLevel];
                PlayerPrefs.SetInt("MissionUnlocked" + TheActiveLevel.ToString(), 1);
                //NextLevel
                GoToNextLevel();


            }
            PlayerPrefs.SetFloat("TotalMissionPoints_" + TheActiveLevel.ToString(), NewAmount);
            Refresh();
            if (gotonextlevel && PlayerPrefs.GetInt("AwardRichCard_MissionLevel_" + TheActiveLevel.ToString()) == 0)
            {
                PlayerPrefs.SetInt("AwardRichCard_MissionLevel_" + TheActiveLevel.ToString(), 1);

                ExtraMan.Instance.richCardMan.AwardGiftCard(TheActiveLevel);
            }

        }
        void Refresh()
        {
            MissionStat[] btns = FindObjectsOfType<MissionStat>();
            for (int i = 0; i < btns.Length; i++)
            {
                btns[i].Refresh();
            }
        }
        public float GetMissionPoints(int TheLevel)
        {
            return PlayerPrefs.GetFloat("TotalMissionPoints_" + TheLevel.ToString());
        }
        public bool IsMissionUnlocked(int TheLevel)
        {
            if (PlayerPrefs.GetInt("ActiveMissionLevel") >= TheLevel)
            {
                return true;
            }
            /*if (PlayerPrefs.GetInt("MissionUnlocked" + TheLevel.ToString()) == 1)
            {
                return true;
            }*/
            return false;
        }
        public void GoToNextLevel()
        {
            int TheActiveLevel = PlayerPrefs.GetInt("ActiveMissionLevel");
            if (TheActiveLevel < MissionGoals.Length - 1)
            {
                PlayerPrefs.SetInt("ActiveMissionLevel", TheActiveLevel + 1);
            }
            Refresh();
        }
        public int GetMissionLevel()
        {
            int TheActiveLevel = PlayerPrefs.GetInt("ActiveMissionLevel");
            /*if (TheActiveLevel >= MissionGoals.Length - 1 && PlayerPrefs.GetInt("ClaimMissionPoints_" + TheActiveLevel.ToString()) == 1
                )
            {
                TheActiveLevel = MissionGoals.Length;
            }*/
            return TheActiveLevel;

        }
        public string GetTime()
        {
            //TimeSpan timeSpan = TimeSpan.FromSeconds(TheTime);
            TimeSpan timeSpan = TimeSpan.FromSeconds(GetDiffToMidnight().TotalSeconds);
            //string thetime=timeSpan.ToString(@"hh\:mm\:ss");
            string thetime = timeSpan.Hours.ToString() + " H " + timeSpan.Minutes.ToString() + " M " + timeSpan.Seconds.ToString() + " S";
            return thetime;
        }
        private void Update()
        {
            string SavedDate = PlayerPrefs.GetString("SavedTime");
            //DateTime CurrentDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(System.DateTime.Now, TheTimeZone);
            DateTime CurrentDate = LocalTime();
            //        Debug.Log(SavedDate);
            if (SavedDate != CurrentDate.ToShortDateString())
            {
                // Debug.Log("ResetTime");
                ResetTime();
                // TheTime -= Time.deltaTime;
                //SaveTime(TheTime);
            }
            else
            {
                // ResetTime();
            }
        }

        [ContextMenu("ResetTime")]
        void ResetTime()
        {
            //DateTime appStart = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(System.DateTime.Now, TheTimeZone);
            DateTime appStart = LocalTime();
            PlayerPrefs.SetString("SavedTime", appStart.ToShortDateString());
            ResetPoints();
        }
        [ContextMenu("GetSavedTime")]
        public void _GetSavedTime()
        {
            Debug.Log(PlayerPrefs.GetString("SavedTime"));
        }
        [ContextMenu("SubtractDay")]
        void AddDay()
        {
            // DateTime appStart = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(System.DateTime.Now, TheTimeZone);
            DateTime appStart = LocalTime();
            appStart = appStart.AddDays(-1);
            Debug.Log(appStart.ToShortDateString());
            PlayerPrefs.SetString("SavedTime", appStart.ToShortDateString());
        }











        void SaveTime(float TheT)
        {
            if (savetimestamp > Time.time)
                return;
            savetimestamp = Time.time + 1;
            PlayerPrefs.SetFloat("ElapsedTime", TheT);

            SaveSession();
        }
        float GetSavedTime()
        {
            float elapsedtime = GetTimeDifferenceBtwSession_seconds();
            // Debug.Log(elapsedtime);
            float thet = PlayerPrefs.GetFloat("ElapsedTime") - elapsedtime;
            //float thet = GetTimeDifferenceBtwSession_seconds();
            if (thet <= 0)
            {
                thet = MaxTime;
                ResetTime();
            }
            return thet;
        }
        void SaveSession()
        {
            string prefname = "LAST_SESSION";
            DateTime _new = DateTime.Now;
            PlayerPrefs.SetString(prefname, _new.ToString());
        }
        public float GetTimeDifferenceBtwSession_seconds()
        {
            string prefname = "LAST_SESSION";
            string last_session = PlayerPrefs.GetString(prefname);
            if (string.IsNullOrEmpty(last_session))
            {
                PlayerPrefs.SetString(prefname, DateTime.Now.ToString());
                return 0;
            }
            float Diff = (float)DateTime.Now.Subtract(DateTime.Parse(last_session)).TotalSeconds;
            //Diff = Diff / 60;
            return Diff;
        }

    }
}