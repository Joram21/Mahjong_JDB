using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System;
public class FetchUserInfo : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern string getLocaltime();

    [Header("Assign 'ConfigMan' here")]
    public ConfigMan configMan;

    [Space(10)]
    [Header("These will autofill")]
    public string Game_url;

    [System.Serializable]
    public class GameData
    {
        public string currency;
        public string clientId;
        public string gameId;
        public string language;
        public string mode;
        public string token;
        public string userId;
        public string timestamp;
        public string Base_url = "https://admin-api3.ibibe.africa";
    }
    public GameData data;
    private void Start()
    {
        CheckUrl();
    }
    void CheckUrl()
    {
        //string theurl = "https://crazy777-73k.pages.dev/?data=tf44b2k3okJFt9pGNyYRuixDm3dsOeOWHPU/O1ljGkwmLaIiAXyDTdqJVoK7VVNcvSDYqwuRK5jY3SBxShazLzJ5R+QJK92quO+MUKSCGYXTITqWIEQsNRvXjtrlSAbm2v4R/iS65S6q/et+HmnmE0XYMybNY/L9UEG6f+o/svXWmwlxThOWTUHFJhjRLhgMaCQS1fUHV5PK319TfGM40GDqKj9OrlW2nZ+FZ/RCJ6k5J3iIn4JtN2gYowHgYbokUrEexMUv+99be1bUwMQFLEbvnE8XozdN9ekkS7gdEBg=";
        if (!Application.isEditor)
        {
            Game_url = Application.absoluteURL;
        }
        string[] tockens = Game_url.Split("data=");
        for(int i = 0; i < tockens.Length; i++)
        {
            //Debug.Log(tockens[i]);
        }
        //tockens = tockens[tockens.Length-1].Split("data=");
        if (tockens.Length <2)
            return;
        string thetocken=tockens[1];
       // Debug.Log(thetocken);
        string DecryptedText = GetComponent<UrlDecryptTocken>().DecryptString(thetocken);
       // Debug.Log(DecryptedText);

        data = JsonUtility.FromJson<GameData>(DecryptedText);

       
        //TimeSpan ToMidnight = TimeSpan.FromHours(24) - TimeSpan;
      //  float TimeElapsed=
       // data.TimestampTime = localTime;
       // data.timestamp = localTime.ToString();
        if (data.userId != "")
        {
            SetPlayerId(data.userId);
            SetClientId(data.clientId);
            SetGameId(data.gameId);
            SetLanguage(data.language);
            SetMode(int.Parse(data.mode));
            //SetMode(data.mode);
            SetCurrency(data.currency);
            SetLanguage(data.language);
            SetUrl(data.Base_url);

            string isoTime = data.timestamp;
            Debug.Log("IsoTime_"+isoTime);
            DateTime utcTime = DateTime.Parse(isoTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
            Debug.Log("Utc_" + utcTime.ToString());
            //DateTime localTime = utcTime.ToLocalTime();
            TimeZoneInfo localZone = TimeZoneInfo.Local;
            DateTime localTime =TimeZoneInfo.ConvertTimeFromUtc(utcTime,localZone);
            Debug.Log("Local Time: " + localTime.ToString());

            double timediff = GetTimeElapsed(localTime).TotalSeconds;
            Debug.Log("Elapsed_" + timediff);
            if (timediff > (60 * 10))
            {
               // Debug.Log("ShouldQuit");
                configMan.ExpiredSessionObj.SetActive(true);
                //Application.Quit();
            }

        }
        
      //  Debug.Log(GetTimeElapsed(localTime).TotalSeconds);
        // Debug.Log("TheUrl_"+theurl);
    }
    public TimeSpan GetTimeElapsed(DateTime Timestamp)
    {
        DateTime currenttime = CurrentTime();
        TimeSpan Diff = currenttime-Timestamp;
        return Diff;
    }
    public DateTime CurrentTime()
    {
        DateTime appStart = LocalTime();
        return appStart;
    }
    public DateTime LocalTime()
    {
        DateTime thetime = new DateTime();
        if (Application.isEditor)
        {
            thetime = System.DateTime.Now;
        }
        else
        {
            string _thetime = getLocaltime();
            thetime = ParseString(_thetime);
        }
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
    public void SetPlayerId(string userId)
    {
        configMan.PassPlayerId(userId);
       configMan.PlayerIdText.text = userId;

        // Use the userId in your game logic
    }
    public void SetMode(int mode)
    {
       // Debug.Log("TheMode: " +mode);
        if (mode == 0)
        {
            configMan.IsDemo = true;
        }
        else
        {
            configMan.IsDemo = false;
        }
        if (configMan.DemoToggle)
        {
           configMan.DemoToggle.isOn = configMan.IsDemo;
        }
    }
  
    public void SetGameId(string id)
    {

        //Debug.Log("TheGameId: " + id.ToString());
        configMan.PassGameId(id);
       configMan.GameIdText.text =  id;

    }
    public void SetClientId(string id)
    {
        //Debug.Log("TheGameId: " + id.ToString());
        configMan.PassClientId(id);
       configMan.ClientIdText.text = id;

    }
    public void SetCurrency(string Which)
    {
       // Debug.Log("TheCurrency: " + Which.ToString());
        configMan.PassCurrency(Which);

    }
    public void SetUrl(string which)
    {
       // Debug.Log("url: " + which);
        configMan.Base_url = which;
    }
    public void SetLanguage(string id)
    {
       // Debug.Log("TheLanguage: " + id.ToString());
        if (id == "en")
        {
            LanguageMan.instance._SetLanguage(TheLanguage.English);
        }
        else if (id == "zh") {
            LanguageMan.instance._SetLanguage(TheLanguage.Chinese);
        }
        else if (id == "es")
        {
            LanguageMan.instance._SetLanguage(TheLanguage.Spanish);
        }
        else if (id == "ja")
        {
            LanguageMan.instance._SetLanguage(TheLanguage.Japan);
        }
        else if (id == "sw")
        {
            LanguageMan.instance._SetLanguage(TheLanguage.Swahili);
        }
        else if (id == "da")
        {
            LanguageMan.instance._SetLanguage(TheLanguage.Danish);
        }
        else if (id == "th")
        {
            LanguageMan.instance._SetLanguage(TheLanguage.Thai);
        }
        else if (id == "id")
        {
            LanguageMan.instance._SetLanguage(TheLanguage.Indonesia);
        }
        else if (id == "vi")
        {
            LanguageMan.instance._SetLanguage(TheLanguage.Vietnam);
        }
        else if (id == "pt-PT")
        {
            LanguageMan.instance._SetLanguage(TheLanguage.Portoguese);
        }
        else if (id == "ko")
        {
            LanguageMan.instance._SetLanguage(TheLanguage.Korea);
        }
        else if (id == "my")
        {
            LanguageMan.instance._SetLanguage(TheLanguage.Burmese);
        }
    }
}
