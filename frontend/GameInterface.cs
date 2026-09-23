using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

// ==========================================
// MODULUL DE CONFIGURARE (SETUP & UI LAYER)
// Acest script gestioneaza interfata in care utilizatorul seteaza parametrii povestii.
// De asemenea, este responsabil pentru lansarea si monitorizarea procesului extern (Python backend).
// ==========================================
public class GameInterface : MonoBehaviour
{
    // Structura de date pentru maparea numelor animalelor cu modelele lor 3D (Prefabs)
    [System.Serializable]
    public class AnimalProfile
    {
        public string numeAnimal;
        public string prefabName;
    }

    [Header("UI Elemente Poveste")]
    public TMP_InputField inputPoveste;
    public Button butonGenereaza;
    public GameObject panelUI;
    public TMP_Dropdown dropdownActori;
    public Slider sliderDurata;
    public TextMeshProUGUI textDurata;

    [Header("UI Noi Setari Educative")]
    public TMP_Dropdown dropdownVarsta;
    public TMP_Dropdown dropdownLimbaBaza;
    public TMP_Dropdown dropdownLimbaInvatare;

    [Header("Casting")]
    public List<AnimalProfile> listaAnimale;

    [Header("Legaturi")]
    public StoryDirector director;

    // Calea absoluta catre scriptul de AI din mediul Linux (Windows Subsystem for Linux)
    public string caleScriptPython = "/mnt/c/Users/andra/Licenta_StoryAI/python/generator_poveste.py";

    void Start()
    {
        // Event Listeners: Inregistram actiunile butoanelor si slider-ului prin cod
        // Aceasta abordare este mai robusta decat setarea lor manuala in inspectorul Unity.
        if (butonGenereaza != null) butonGenereaza.onClick.AddListener(OnClickGenereaza);
        if (sliderDurata != null) sliderDurata.onValueChanged.AddListener(OnSliderChanged);

        // Initializare UI cu valoarea default a slider-ului
        if (sliderDurata != null) OnSliderChanged(sliderDurata.value);
    }

    // Actualizeaza feedback-ul vizual cand utilizatorul trage de slider
    void OnSliderChanged(float valoare)
    {
        if (textDurata != null) textDurata.text = valoare + " min";
    }

    // Functie declansata la apasarea butonului "Genereaza"
    void OnClickGenereaza()
    {
        // Validare input (Input Validation): Nu pornim un proces costisitor daca utilizatorul nu a scris o tema
        string tema = inputPoveste.text;
        if (string.IsNullOrEmpty(tema)) return;

        // Preluarea parametrilor din componentele UI
        int indexActor = dropdownActori != null ? dropdownActori.value : 0;
        string numeErou = listaAnimale[indexActor].numeAnimal;
        int durataMin = (int)sliderDurata.value;

        string varsta = dropdownVarsta.options[dropdownVarsta.value].text;
        string lBaza = dropdownLimbaBaza.options[dropdownLimbaBaza.value].text.ToLower();
        string lInv = dropdownLimbaInvatare.options[dropdownLimbaInvatare.value].text.ToLower();

        // Lansam procesul greu de comunicare cu exteriorul intr-o Corutina pentru a mentine UI-ul responsiv
        StartCoroutine(GenerareFlux(tema, numeErou, durataMin, varsta, lBaza, lInv));
    }

    // ==========================================
    // INTER-PROCESS COMMUNICATION (IPC) & ASINCRONISM
    // Aici Unity (C#) trimite parametrii catre Backend (Python) si asteapta raspunsul.
    // ==========================================
    IEnumerator GenerareFlux(string tema, string erou, int durata, string varsta, string lBaza, string lInv)
    {
        UnityEngine.Debug.Log($"🚀 Generare: {tema} | Pt: {varsta} ani | Din: {lBaza} in {lInv}");

        // Dezactivam butonul pentru a preveni spam-ul de click-uri (Double-Submit Prevention)
        if (butonGenereaza != null) butonGenereaza.interactable = false;

        // Clean-up: Curatam fisierele vechi de pe disk inainte de a cere unele noi
        if (director != null) director.CurataImaginiVechi();

        // Ne asiguram ca interfata de evaluare este ascunsa
        if (director != null && director.quizManager != null)
            director.quizManager.quizPanel.SetActive(false);

        // Instantiem clasa ProcessStartInfo care ne permite sa lansam procese in sistemul de operare
        ProcessStartInfo psi = new ProcessStartInfo();

        // Rulam prin WSL (Windows Subsystem for Linux) deoarece mediul Python este configurat acolo
        psi.FileName = "wsl.exe";

        // Construim string-ul de argumente exact asa cum il asteapta backend-ul (sys.argv)
        psi.Arguments = $"python3 \"{caleScriptPython}\" \"{tema}\" \"{erou}\" \"{durata}\" \"{varsta}\" \"{lBaza}\" \"{lInv}\"";

        // Configuram procesul sa ruleze 'invizibil' (in fundal) si captam output-ul (Console Streams)
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.CreateNoWindow = true;

        // Lansam efectiv scriptul Python
        Process process = Process.Start(psi);

        // Polling Asincron: Ciclul 'while' verifica constant daca Python a terminat treaba.
        // 'yield return null' ii spune motorului Unity: "Mergi si randeaza urmatorul frame grafic, ma intorc eu aici mai tarziu".
        // Fara acest rand, tot jocul ar ingheta pe ecran complet timp de 1 minut pana cand AI-ul ar termina.
        while (!process.HasExited)
        {
            yield return null;
        }

        // Citim raspunsul returnat de scriptul Python prin stream-urile de sistem
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        // Evaluam codul de iesire al procesului (Exit Code 0 inseamna succes in sistemele de operare Unix/Windows)
        if (process.ExitCode == 0)
        {
            UnityEngine.Debug.Log("🐍 Python Success: " + output);

            // Succes: Ascundem meniul de Setup
            if (panelUI != null) panelUI.SetActive(false);

            // Predam controlul inapoi Managerului Principal (StoryDirector) pentru a rula fisierele abia generate
            if (director != null)
            {
                director.temaCurenta = tema;
                director.PornestePovesteaNoua();
            }
        }
        else
        {
            // Error Handling: In caz de esec, afisam eroarea si reactivam butonul ca utilizatorul sa poata incerca din nou
            UnityEngine.Debug.LogError("🐍 Python Error: " + error);
            if (butonGenereaza != null) butonGenereaza.interactable = true;
        }
    }
}