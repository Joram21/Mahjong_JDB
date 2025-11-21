using UnityEngine;
using UnityEditor;
public class TranslationHelper : Editor
{
	[MenuItem("Translation/_Assign")]
	static void Assign()
	{
		FindFirstObjectByType<LanguageMan>().AssignCodes();
	}
	[MenuItem("Translation/English")]
	static void SetEnglish()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage =TheLanguage.English;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}
		FindFirstObjectByType<LanguageMan>().ActiveLanguage = TheLanguage.English;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}
	[MenuItem("Translation/Chinese")]
	static void SetChinese()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage = TheLanguage.Chinese;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}

		FindFirstObjectByType<LanguageMan>().ActiveLanguage = TheLanguage.Chinese;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}
	[MenuItem("Translation/Japan")]
	static void SetJapan()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage = TheLanguage.Japan;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}
		FindFirstObjectByType<LanguageMan>().ActiveLanguage = TheLanguage.Japan;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}
	[MenuItem("Translation/Spanish")]
	static void SetSpanish()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage = TheLanguage.Spanish;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}
		FindFirstObjectByType<LanguageMan>().ActiveLanguage = TheLanguage.Spanish;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}
	[MenuItem("Translation/Swahili")]
	static void SetSwahili()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage = TheLanguage.Swahili;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}
		FindFirstObjectByType<LanguageMan>().ActiveLanguage = TheLanguage.Swahili;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}

	[MenuItem("Translation/Danish")]
	static void SetDanish()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage = TheLanguage.Danish;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}
		FindFirstObjectByType<LanguageMan>().ActiveLanguage = TheLanguage.Danish;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}
	[MenuItem("Translation/Thai")]
	static void SetThai()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage = TheLanguage.Thai;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}
		FindFirstObjectByType<LanguageMan>().ActiveLanguage =TheLanguage.Thai;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}
	[MenuItem("Translation/Indonesia")]
	static void SetIndonesia()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage = TheLanguage.Indonesia;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}
		FindFirstObjectByType<LanguageMan>().ActiveLanguage = TheLanguage.Indonesia;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}
	[MenuItem("Translation/Vietnam")]
	static void SetSetVietnam()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage =TheLanguage.Vietnam;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}
		FindFirstObjectByType<LanguageMan>().ActiveLanguage = TheLanguage.Vietnam;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}
	[MenuItem("Translation/Portoguese")]
	static void SetPortoguese()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage = TheLanguage.Portoguese;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}
		FindFirstObjectByType<LanguageMan>().ActiveLanguage = TheLanguage.Portoguese;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}
	[MenuItem("Translation/Korea")]
	static void SetKorea()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage = TheLanguage.Korea;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}
		FindFirstObjectByType<LanguageMan>().ActiveLanguage = TheLanguage.Korea;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}
	[MenuItem("Translation/Burmese")]
	static void SetBurmese()
	{
		if (!Application.isPlaying)
			return;
		if (FindFirstObjectByType<Extra_LanguageMan>())
		{
			FindFirstObjectByType<Extra_LanguageMan>().ActiveLanguage = TheLanguage.Burmese;
			FindFirstObjectByType<Extra_LanguageMan>().RefreshAll();
		}
		FindFirstObjectByType<LanguageMan>().ActiveLanguage = TheLanguage.Burmese;
		FindFirstObjectByType<LanguageMan>().RefreshAll();
	}

}
