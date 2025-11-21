namespace Config_Assets
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.IO;
    using System.Collections.Generic;

    [System.Serializable]
    public class Game_Data
    {
        public int id = 35;
        public int thegame_id = 35;
        public string game_title = "Scratch Fortune Gem";
        public string game_image;
        public string game_image_url = "https://admin-api.ibibe.africa/gd";
        public string promotional_image_url;
        public string promotional_image_url_portrait;
        public string promotional_image_url_square;
        public string game_description;
        public string game_type = "Normal";
        public Sprite ThePromoIcon;
        public Sprite TheRichCardIcon;
        public int approved = 1;
        public bool IsLoaded_RichCard;
        public bool IsLoaded_PromoImage;
    }
    [System.Serializable]
    public class GameList
    {
        public Game_Data[] games;

    }

    public class Games_Catalog : MonoBehaviour
    {
        string appversion;
        public bool IsLoaded;
        public string ServerLink = "https://admin-api.ibibe.africa";
        public GameList gameList;
        bool IsInit;
        void ManualStart()
        {
            char[] tocken = Application.version.ToCharArray();
            for (int i = 0; i < tocken.Length; i++)
            {
                //Debug.Log(tocken[i]);
                if (tocken[i] != '.')
                {
                    appversion = appversion + tocken[i];
                }
            }
            appversion = appversion + "_";
            // appversion = Application.version+"_";
            FetchGames();
        }
        public void Init()
        {
            if (IsInit)
                return;
            IsInit = true;
            ManualStart();
        }
        [ContextMenu("FetchGames")]
        public void FetchGames()
        {
            if (ConfigMan.Instance)
            {
                ServerLink = ConfigMan.Instance.Base_url;
            }
            ExtraMan.Instance.fakeLoading.Open(1);
            IsLoaded = false;
            StartCoroutine(_FetchGames(ServerLink + "/api/v1/games/"));
        }
        IEnumerator _FetchGames(string url)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.useHttpContinue = false;
                www.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
                www.SetRequestHeader("Pragma", "no-cache");
                www.SetRequestHeader("Expires", "0");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    //Debug.Log("Received: " + www.downloadHandler.text);
                    gameList = JsonUtility.FromJson<GameList>("{\"games\":" + www.downloadHandler.text + "}");
                    Array.Reverse(gameList.games);
                    List<Game_Data> game_Datas = new List<Game_Data>();
                    Game_Data temp = new Game_Data();
                    temp.id = 0;
                    temp.game_image_url = "";
                    temp.promotional_image_url = "";
                    temp.game_title = "";
                    temp.approved = 0;
                    game_Datas.Add(temp);
                    for (int i = 0; i < gameList.games.Length; i++)
                    {
                        game_Datas.Add(gameList.games[i]);
                    }
                    gameList.games = game_Datas.ToArray();
                    DownloadPromoImages();
                    DownloadRichCard();
                }
                else
                {
                    Debug.Log("Error: " + www.error);
                }
            } // The using block ensures www.Dispo
        }
        [ContextMenu("DownloadPromoImages")]
        public void DownloadPromoImages()
        {
            for (int i = 0; i < gameList.games.Length; i++)
            {
                if (gameList.games[i].approved == 1)
                {
                    gameList.games[i].IsLoaded_PromoImage = false;
                    string TheUrl = gameList.games[i].promotional_image_url_square;
                    if (TheUrl == "")
                    {
                        TheUrl = gameList.games[i].game_image_url;
                    }
                    if (TheUrl != "")
                    {
                        Sprite TheIcon = GetSavedIcon(i);
                        // Debug.Log("ShouldDownload_" + gameList.games[i].id);

                        if (!TheIcon)
                        {
                            StartCoroutine(DownloadImage(TheUrl, i));
                        }
                        else
                        {
                            gameList.games[i].IsLoaded_PromoImage = true;
                            CheckIsFinishLoading();
                        }
                    }
                    else
                    {
                        gameList.games[i].IsLoaded_PromoImage = true;
                        CheckIsFinishLoading();
                    }
                }
                else
                {
                    gameList.games[i].IsLoaded_PromoImage = true;
                    CheckIsFinishLoading();
                }

            }



        }
        [ContextMenu("DownloadRichCard")]
        void DownloadRichCard()
        {
            for (int i = 0; i < gameList.games.Length; i++)
            {
                if (gameList.games[i].approved == 1)
                {
                    gameList.games[i].IsLoaded_RichCard = false;
                    string TheUrl = gameList.games[i].promotional_image_url;

                    Sprite TheIcon = GetSavedRichCard(i);
                    // Debug.Log("ShouldDownload_" + gameList.games[i].id);
                    if (TheUrl == "")
                    {
                        TheUrl = gameList.games[i].game_image_url;
                    }
                    if (!TheIcon && TheUrl != "")
                    {
                        StartCoroutine(DownloadRichCardImage(TheUrl, i));
                    }
                    else
                    {
                        gameList.games[i].IsLoaded_RichCard = true;
                        CheckIsFinishLoading();
                    }
                }
                else
                {
                    gameList.games[i].IsLoaded_RichCard = true;
                    CheckIsFinishLoading();
                }
            }
        }
        public int Index_id(int thegameid)
        {
            return thegameid;
            for (int i = 0; i < gameList.games.Length; i++)
            {
                if (gameList.games[i].id == thegameid)
                {
                    return i;
                }
            }
            return 0;
        }
        IEnumerator DownloadImage(string MediaUrl, int theId)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                gameList.games[Index_id(theId)].IsLoaded_PromoImage = true;
                CheckIsFinishLoading();
            }
            else
            {
                gameList.games[Index_id(theId)].IsLoaded_PromoImage = true;
                CheckIsFinishLoading();

                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                if (texture)
                {
                    byte[] imageBytes = texture.EncodeToPNG();
                    DestroyImmediate(texture);
                    string savePath = "/Icons";
                    string FileName = "/Game_" + appversion + theId.ToString() + ".png";
                    if (SystemInfo.deviceType == DeviceType.Handheld)
                    {
                        savePath = "Icons";
                    }
                    else
                    {
                        savePath = Application.persistentDataPath + "/Icons";
                    }
                    DirectoryInfo DataFolder = new DirectoryInfo(savePath);
                    if (!DataFolder.Exists)
                    {
                        Directory.CreateDirectory(savePath);
                    }
                    if (DataFolder.Exists)
                    {
                        //Debug.Log("PathAvailable");

                    }
                    //Debug.Log(savePath);
                    System.IO.File.WriteAllBytes(savePath + FileName, imageBytes);

                }
            }
        }
        void CheckIsFinishLoading()
        {
            bool isfinished = true;
            for (int i = 0; i < gameList.games.Length; i++)
            {
                if (!gameList.games[i].IsLoaded_RichCard || !gameList.games[i].IsLoaded_PromoImage)
                {
                    isfinished = false;
                    break;
                }
            }
            if (isfinished)
            {
                IsLoaded = true;

                MissionStat[] missionstats = FindObjectsOfType<MissionStat>();
                for (int i = 0; i < missionstats.Length; i++)
                {
                    missionstats[i].Refresh();
                }

    ;
            }
        }
        public Sprite GetSavedIcon(int theId)
        {
           
            Debug.Log(theId);
            string savePath = "/Icons";
            string FileName = "/Game_" + appversion + theId.ToString() + ".png";
            if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                savePath = "Icons";
            }
            else
            {
                savePath = Application.persistentDataPath + "/Icons";
            }
            //DirectoryInfo DataFolder = new DirectoryInfo(savePath);
            if (!File.Exists(savePath + FileName))
            {
                //Debug.Log("NoFile_"+ savePath + FileName);
                return null;
            }
            //        Debug.Log(theId);
            byte[] byteArray = File.ReadAllBytes(savePath + FileName);

            Texture2D texture = new Texture2D(8, 8);
            texture.LoadImage(byteArray);
            Vector2 Resolution = new Vector2(texture.width, texture.height);
            Sprite s = Sprite.Create(texture, new Rect(0, 0, Resolution.x, Resolution.y), Vector2.zero, 0.001f);
            gameList.games[theId].ThePromoIcon = s;
            // RectTransform rt = Go.GetComponent(typeof(RectTransform)) as RectTransform;
            //rt.sizeDelta = Resolution / 5;
            //        Debug.Log(FileName);

            return s;
        }
        public string GameName(int theId)
        {
            return gameList.games[Index_id(theId)].game_title;
        }
        public Sprite GetSavedRichCard(int theId)
        {
            string savePath = "/Icons";
            string FileName = "/RichCard_" + appversion + theId.ToString() + ".png";
            if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                savePath = "Icons";
            }
            else
            {
                savePath = Application.persistentDataPath + "/Icons";
            }
            //DirectoryInfo DataFolder = new DirectoryInfo(savePath);
            if (!File.Exists(savePath + FileName))
            {
                //Debug.Log("NoFile_"+ savePath + FileName);
                return null;
            }
            //        Debug.Log(theId);
            byte[] byteArray = File.ReadAllBytes(savePath + FileName);

            Texture2D texture = new Texture2D(8, 8);
            texture.LoadImage(byteArray);
            Vector2 Resolution = new Vector2(texture.width, texture.height);
            Sprite s = Sprite.Create(texture, new Rect(0, 0, Resolution.x, Resolution.y), Vector2.zero, 0.001f);
            gameList.games[Index_id(theId)].TheRichCardIcon = s;
            // RectTransform rt = Go.GetComponent(typeof(RectTransform)) as RectTransform;
            //rt.sizeDelta = Resolution / 5;
            //        Debug.Log(FileName);

            return s;
        }
        IEnumerator DownloadRichCardImage(string MediaUrl, int theId)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                gameList.games[Index_id(theId)].IsLoaded_RichCard = true;
                CheckIsFinishLoading();
            }
            else
            {
                gameList.games[Index_id(theId)].IsLoaded_RichCard = true;
                CheckIsFinishLoading();

                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                if (texture)
                {
                    byte[] imageBytes = texture.EncodeToPNG();
                    DestroyImmediate(texture);
                    string savePath = "/Icons";
                    string FileName = "/RichCard_" + appversion + theId.ToString() + ".png";
                    if (SystemInfo.deviceType == DeviceType.Handheld)
                    {
                        savePath = "Icons";
                    }
                    else
                    {
                        savePath = Application.persistentDataPath + "/Icons";
                    }
                    DirectoryInfo DataFolder = new DirectoryInfo(savePath);
                    if (!DataFolder.Exists)
                    {
                        Directory.CreateDirectory(savePath);
                    }
                    if (DataFolder.Exists)
                    {
                        //Debug.Log("PathAvailable");

                    }
                    //Debug.Log(savePath);
                    System.IO.File.WriteAllBytes(savePath + FileName, imageBytes);

                }
            }
        }


    }
}
