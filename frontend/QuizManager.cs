using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
using System.Collections;

public class QuizManager : MonoBehaviour
{
    [Header("Referinte Panou Quiz")]
    public GameObject quizPanel;
    public TextMeshProUGUI textIntrebare;
    public TextMeshProUGUI textRezultat;

    [Header("Butoane Optiuni (Trebuie sa aiba RawImage copii)")]
    public Button[] butoaneOptiuni;

    [Header("Ecran Imagine Enunt (Sus)")]
    public RawImage imagineEnuntQuiz;

    private List<QuizData> listaIntrebari = new List<QuizData>();
    private int indiceIntrebareCurenta = 0;
    private int raspunsuriCorecte = 0;

    private Dictionary<string, string[]> traduceriFeedback = new Dictionary<string, string[]>()
    {
        { "romana", new string[] { "Bravo! Ai învățat corect!", "Mai încearcă!" } },
        { "engleza", new string[] { "Great job! That's correct!", "Try again!" } },
        { "spaniola", new string[] { "¡Muy bien! ¡Es correcto!", "¡Inténtalo de nuevo!" } },
        { "franceza", new string[] { "Bravo ! C'est correct !", "Réessaie !" } },
        { "italiana", new string[] { "Bravo! È corretto!", "Riprova!" } }
    };

    public void ArataQuiz(List<QuizData> setIntrebari)
    {
        if (setIntrebari == null || setIntrebari.Count == 0)
        {
            InchideQuizLaFinal();
            return;
        }

        listaIntrebari = setIntrebari;
        indiceIntrebareCurenta = 0;
        raspunsuriCorecte = 0;

        quizPanel.SetActive(true);
        StartCoroutine(IncarcaIntrebareCurentaRutina());
    }

    private IEnumerator IncarcaIntrebareCurentaRutina()
    {
        if (indiceIntrebareCurenta >= listaIntrebari.Count)
        {
            InchideQuizLaFinal();
            yield break;
        }

        QuizData intrebare = listaIntrebari[indiceIntrebareCurenta];
        textIntrebare.text = intrebare.question;
        textRezultat.text = "";

        // 1. Gestionare Imagine Enunț (Sus)
        if (!string.IsNullOrEmpty(intrebare.imagineEnunt))
        {
            if (imagineEnuntQuiz != null)
            {
                imagineEnuntQuiz.gameObject.SetActive(true);
                yield return StartCoroutine(IncarcaTexturaPeRawImage(intrebare.imagineEnunt, imagineEnuntQuiz));
            }
        }
        else
        {
            if (imagineEnuntQuiz != null)
            {
                imagineEnuntQuiz.gameObject.SetActive(false);
            }
        }

        // 2. Detectăm dacă răspunsurile sunt sub formă de imagini (ImagineAB)
        bool folosesteImaginiPeButoane = (intrebare.tipIntrebare == "ImagineAB" &&
                                          intrebare.optiuniImagini != null &&
                                          intrebare.optiuniImagini.Count > 0);

        for (int i = 0; i < butoaneOptiuni.Length; i++)
        {
            if (i < (folosesteImaginiPeButoane ? intrebare.optiuniImagini.Count : intrebare.options.Count))
            {
                butoaneOptiuni[i].gameObject.SetActive(true);
                int index = i;

                RawImage imgOption = butoaneOptiuni[i].GetComponentInChildren<RawImage>();
                TextMeshProUGUI textComp = butoaneOptiuni[i].GetComponentInChildren<TextMeshProUGUI>();

                if (folosesteImaginiPeButoane)
                {
                    if (textComp != null) textComp.text = ""; // Golim textul
                    if (imgOption != null)
                    {
                        imgOption.gameObject.SetActive(true);
                        yield return StartCoroutine(IncarcaTexturaPeRawImage(intrebare.optiuniImagini[i], imgOption));
                    }
                }
                else
                {
                    if (imgOption != null) imgOption.gameObject.SetActive(false); // Ascundem imaginea
                    if (textComp != null)
                    {
                        textComp.text = intrebare.options[i];
                    }
                }

                butoaneOptiuni[i].onClick.RemoveAllListeners();
                butoaneOptiuni[i].onClick.AddListener(() => VerificaRaspuns(index));
            }
            else
            {
                butoaneOptiuni[i].gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator IncarcaTexturaPeRawImage(string fileName, RawImage rawImg)
    {
        string caleFizica = Path.Combine(Application.streamingAssetsPath, fileName);
        string pathClean = "file://" + caleFizica.Replace("\\", "/");

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(pathClean))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(www);
                if (rawImg != null)
                {
                    rawImg.texture = tex;
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Nu am putut încărca textura [{fileName}] pe buton: " + www.error);
            }
        }
    }

    void VerificaRaspuns(int indexAles)
    {
        QuizData intrebareCurenta = listaIntrebari[indiceIntrebareCurenta];

        string limbaBaza = "engleza";
        var gameInterface = FindFirstObjectByType<GameInterface>();
        if (gameInterface != null && gameInterface.dropdownLimbaBaza != null)
        {
            limbaBaza = gameInterface.dropdownLimbaBaza.options[gameInterface.dropdownLimbaBaza.value].text.ToLower();
        }

        string mesajSucces = "Correct!";
        string mesajEroare = "Wrong!";

        if (traduceriFeedback.ContainsKey(limbaBaza))
        {
            mesajSucces = traduceriFeedback[limbaBaza][0];
            mesajEroare = traduceriFeedback[limbaBaza][1];
        }

        if (indexAles == intrebareCurenta.correct_index)
        {
            textRezultat.text = $"<color=green>{mesajSucces}</color>";
            raspunsuriCorecte++;
            Invoke("UrmatoareIntrebare", 1.5f);
        }
        else
        {
            textRezultat.text = $"<color=red>{mesajEroare}</color>";
        }
    }

    void UrmatoareIntrebare()
    {
        indiceIntrebareCurenta++;
        StartCoroutine(IncarcaIntrebareCurentaRutina());
    }

    void InchideQuizLaFinal()
    {
        quizPanel.SetActive(false);
        textRezultat.text = "";

        int notaFinala = 0;
        if (listaIntrebari.Count > 0)
        {
            notaFinala = Mathf.RoundToInt((float)raspunsuriCorecte / listaIntrebari.Count * 100);
        }

        StoryDirector director = FindFirstObjectByType<StoryDirector>();
        if (director != null)
        {
            director.Elev_FinalizeazaQuiz(notaFinala);
        }
        else
        {
            Debug.LogError("❌ Nu am găsit StoryDirector în scenă pentru a salva progresul!");
        }
    }
}