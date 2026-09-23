using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

// ==========================================
// STRATUL DE DATE (DATA LAYER)
// ==========================================

[System.Serializable]
public class ActionData
{
    public string type;
    public string actorId;
    public string text;
    public string audioFile;
    public string animation;
    public string imageFile;
    public string camera;
    public Vector3Data targetPos;
    public float duration;
}

[System.Serializable]
public class QuizData
{
    public string question;
    public List<string> options;
    public int correct_index;

    // Adăugăm aceste atribute pentru noul sistem extins de quiz
    public string tipIntrebare;      // "Text", "TextImagine", "ImagineAB"
    public string imagineEnunt;      // Numele fișierului imagine (ex: imagine_2.jpg)
    public List<string> optiuniImagini; // Numele fișierelor imagine pentru variante vizuale
}

[System.Serializable]
public class StoryData
{
    public List<ActorData> actors;
    public List<ActionData> actions;
    public List<QuizData> quiz;
}

[System.Serializable]
public class Vector3Data
{
    public float x, y, z;
    public Vector3 ToVector3() { return new Vector3(x, y, z); }
}

[System.Serializable]
public class ActorData
{
    public string id;
    public string prefabName;
    public Vector3Data initialPosition;
}

[System.Serializable]
public class UserData
{
    public string username;
    public string password;
    public string rol;
}

[System.Serializable]
public class ClassData
{
    public string idClasa;
    public string idProfesor;
    public List<string> idElevi = new List<string>();
    public List<string> lectiiGenerate = new List<string>();
}

[System.Serializable]
public class LectieProgres
{
    public string idLectie;
    public string status;
    public int incercariRamase;
    public int scorCurent;
    public int numarRedari;
    public List<int> istoricNote = new List<int>();
}

[System.Serializable]
public class ElevProgresData
{
    public string idElev;
    public List<LectieProgres> progresLectii = new List<LectieProgres>();
}

// ==========================================
// STRATUL DE CONTROL (MANAGER / ORCHESTRATOR)
// ==========================================

public class StoryDirector : MonoBehaviour
{
    [Header("Setup Scena")]
    public List<GameObject> libraryPrefabs;
    public CinemachineCamera vcamGeneral;
    public CinemachineCamera vcamMonitor;
    public AudioSource sursaAudio;
    public QuizManager quizManager;

    [Header("UI Subtitrari")]
    public TextMeshProUGUI textSubtitrare;

    [Header("Setari Ecran")]
    public Renderer magicScreen;

    [Header("Sistem Salvare & End Menu")]
    public string utilizatorCurent = "Vizitator";
    public string rolCurent = "";
    public string temaCurenta = "Poveste_Noua";
    public GameObject endPanel;

    [Header("UI Login")]
    public TMP_InputField inputEmail;
    public TMP_InputField inputPassword;
    public TextMeshProUGUI textFeedbackLogin;
    public GameObject loginPanel;

    [Header("UI Panouri Roluri (LMS)")]
    public GameObject adminPanel;
    public GameObject profesorPanel;
    public GameObject elevPanel;

    [Header("UI Admin - Gestiune Utilizatori")]
    public TMP_InputField inputNouUser;
    public TMP_InputField inputNouaParola;
    public TMP_Dropdown dropdownNouRol;
    public TextMeshProUGUI textFeedbackAdmin;

    [Header("UI Admin - Gestiune Clase")]
    public TMP_InputField inputNumeClasa;
    public TextMeshProUGUI textFeedbackClasa;

    [Header("UI Admin - Alocare (Relational Mapping)")]
    public TMP_Dropdown dropdownSelectieClasa;
    public TMP_Dropdown dropdownSelectieProfesor;
    public TMP_Dropdown dropdownSelectieElev;
    public TextMeshProUGUI textFeedbackAlocare;

    [Header("Sistem Istoric (Doar Profesor)")]
    public GameObject historyPanel;
    public Transform listContainer;
    public GameObject storyButtonPrefab;
    public Button btnSterge;
    public Button btnRedenumeste;
    public GameObject renamePanel;
    public TMP_InputField inputNumeNou;

    [Header("UI Profesor - Publicare Lectie (pe EndPanel)")]
    public TMP_InputField inputNumeSalvare;
    public TMP_Dropdown dropdownClaseProfesor;
    public TextMeshProUGUI textFeedbackPublicare;

    [Header("UI Profesor - Navigatie (LMS)")]
    public GameObject profesorDashboardPanel;
    public GameObject profesorManagePanel;
    public GameObject profesorGeneratePanel;

    [Header("UI Profesor - Gestiune Clase (Vizualizare)")]
    public TMP_Dropdown dropdownClaseVizualizare;
    public TextMeshProUGUI textListaElevi;

    [Header("UI Elev - Navigatie Noua")]
    public GameObject elevDashboardPanel;
    public GameObject elevLectiiPanel;
    public GameObject elevProgresPanel;
    public TextMeshProUGUI textWelcomeElev;
    public TextMeshProUGUI textStatsElev;

    public Transform containerLectiiElev;
    public GameObject butonLectieElevPrefab;

    [Header("Sistem Explore Mode")]
    public GameObject explorePanel;
    public TMP_Dropdown dropdownLimbaExplore;
    public string limbaSelectataPentruExplore = "English";
    public GameObject btnExitExplore;

    private Dictionary<string, GameObject> activeActors = new Dictionary<string, GameObject>();
    private string customStoryPath = "";
    private List<string> povestiSelectate = new List<string>();
    private string ultimulDraftSalvat = "";

    private string caleDBUtilizatori;
    private string caleDBClase;
    private string caleDBProgres;
    private string caleDBProfesori;

    void Start()
    {
        InitDatabase();
    }

    private void InitDatabase()
    {
        string rootDB = Path.Combine(Application.persistentDataPath, "BazaDeDate_Scoala");
        caleDBUtilizatori = Path.Combine(rootDB, "Utilizatori");
        caleDBClase = Path.Combine(rootDB, "Clase");
        caleDBProgres = Path.Combine(rootDB, "Progres_Elevi");
        caleDBProfesori = Path.Combine(rootDB, "Sertar_Profesori");

        if (!Directory.Exists(caleDBUtilizatori)) Directory.CreateDirectory(caleDBUtilizatori);
        if (!Directory.Exists(caleDBClase)) Directory.CreateDirectory(caleDBClase);
        if (!Directory.Exists(caleDBProgres)) Directory.CreateDirectory(caleDBProgres);
        if (!Directory.Exists(caleDBProfesori)) Directory.CreateDirectory(caleDBProfesori);

        string caleAdmin = Path.Combine(caleDBUtilizatori, "admin.json");
        if (!File.Exists(caleAdmin))
        {
            UserData adminData = new UserData { username = "admin", password = "scoala2026", rol = "Admin" };
            File.WriteAllText(caleAdmin, JsonUtility.ToJson(adminData, true));
            Debug.Log(" Contul de Super-Admin a fost generat automat.");
        }
    }

    public void LoginCont()
    {
        string user = inputEmail.text.Trim().ToLower();
        string parola = inputPassword.text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(parola))
        {
            textFeedbackLogin.text = "Completeaza user-ul si parola!";
            textFeedbackLogin.color = Color.red;
            return;
        }

        string caleUser = Path.Combine(caleDBUtilizatori, user + ".json");

        if (File.Exists(caleUser))
        {
            string jsonContent = File.ReadAllText(caleUser);
            UserData dateCont = JsonUtility.FromJson<UserData>(jsonContent);

            if (dateCont.password == parola)
            {
                textFeedbackLogin.text = "Login reusit! Se incarca profilul...";
                textFeedbackLogin.color = Color.green;
                rolCurent = dateCont.rol;
                StartCoroutine(IntraInJoc(user));
            }
            else
            {
                textFeedbackLogin.text = "Parola incorecta!";
                textFeedbackLogin.color = Color.red;
            }
        }
        else
        {
            textFeedbackLogin.text = "Contul nu exista! Contactati secretariatul.";
            textFeedbackLogin.color = Color.red;
        }
    }

    public void CurataCampuri()
    {
        inputEmail.text = "";
        inputPassword.text = "";
        textFeedbackLogin.text = "";
    }

    private IEnumerator IntraInJoc(string user)
    {
        utilizatorCurent = user;
        yield return new WaitForSeconds(1.5f);

        loginPanel.SetActive(false);

        if (rolCurent == "Admin")
        {
            if (adminPanel != null)
            {
                adminPanel.SetActive(true);
                Admin_IncarcaListeAlocare();
            }
        }
        else if (rolCurent == "Profesor")
        {
            if (profesorDashboardPanel != null) profesorDashboardPanel.SetActive(true);
            Profesor_IncarcaClase();
            Debug.Log(" Accesat Dashboard-ul de profesor.");
        }
        else if (rolCurent == "Elev")
        {
            if (elevPanel != null) elevPanel.SetActive(true);
            Elev_IncarcaDashboard();
        }
    }

    public void Admin_CreeazaContNou()
    {
        string newUser = inputNouUser.text.Trim().ToLower();
        string newPass = inputNouaParola.text;

        if (string.IsNullOrEmpty(newUser) || string.IsNullOrEmpty(newPass))
        {
            if (textFeedbackAdmin != null) { textFeedbackAdmin.text = "Completati toate campurile!"; textFeedbackAdmin.color = Color.red; }
            return;
        }

        string rolSelectat = dropdownNouRol.options[dropdownNouRol.value].text;
        string caleFisier = Path.Combine(caleDBUtilizatori, newUser + ".json");

        if (File.Exists(caleFisier))
        {
            if (textFeedbackAdmin != null) { textFeedbackAdmin.text = "Acest user exista deja!"; textFeedbackAdmin.color = Color.red; }
            return;
        }

        UserData dateNoi = new UserData { username = newUser, password = newPass, rol = rolSelectat };
        File.WriteAllText(caleFisier, JsonUtility.ToJson(dateNoi, true));

        if (rolSelectat == "Elev")
        {
            ElevProgresData progresGol = new ElevProgresData { idElev = newUser, progresLectii = new List<LectieProgres>() };
            string caleProgres = Path.Combine(caleDBProgres, newUser + "_progres.json");
            File.WriteAllText(caleProgres, JsonUtility.ToJson(progresGol, true));
        }

        if (textFeedbackAdmin != null) { textFeedbackAdmin.text = $"Contul {rolSelectat} creat cu succes!"; textFeedbackAdmin.color = Color.green; }

        inputNouUser.text = "";
        inputNouaParola.text = "";
    }

    public void Admin_CreeazaClasaNoua()
    {
        string numeClasaBrut = inputNumeClasa.text.Trim();

        if (string.IsNullOrEmpty(numeClasaBrut))
        {
            if (textFeedbackClasa != null) { textFeedbackClasa.text = "Scrieti numele clasei!"; textFeedbackClasa.color = Color.red; }
            return;
        }

        string idClasaSigur = string.Join("_", numeClasaBrut.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_").ToLower();
        string caleFisierClasa = Path.Combine(caleDBClase, idClasaSigur + ".json");

        if (File.Exists(caleFisierClasa))
        {
            if (textFeedbackClasa != null) { textFeedbackClasa.text = "Aceasta clasa exista deja!"; textFeedbackClasa.color = Color.red; }
            return;
        }

        ClassData clasaNoua = new ClassData
        {
            idClasa = idClasaSigur,
            idProfesor = "",
            idElevi = new List<string>(),
            lectiiGenerate = new List<string>()
        };

        File.WriteAllText(caleFisierClasa, JsonUtility.ToJson(clasaNoua, true));

        if (textFeedbackClasa != null) { textFeedbackClasa.text = "Clasa creata cu succes!"; textFeedbackClasa.color = Color.green; }
        inputNumeClasa.text = "";
    }

    public void Admin_IncarcaListeAlocare()
    {
        if (dropdownSelectieClasa == null || dropdownSelectieProfesor == null || dropdownSelectieElev == null) return;

        dropdownSelectieClasa.ClearOptions();
        dropdownSelectieProfesor.ClearOptions();
        dropdownSelectieElev.ClearOptions();

        List<string> clase = new List<string>();
        List<string> profi = new List<string>();
        List<string> elevi = new List<string>();

        string[] fisiereClase = Directory.GetFiles(caleDBClase, "*.json");
        foreach (string file in fisiereClase) clase.Add(Path.GetFileNameWithoutExtension(file));

        string[] fisiereUseri = Directory.GetFiles(caleDBUtilizatori, "*.json");
        foreach (string file in fisiereUseri)
        {
            string numeUser = Path.GetFileNameWithoutExtension(file);
            if (numeUser == "admin") continue;

            string continut = File.ReadAllText(file);
            UserData data = JsonUtility.FromJson<UserData>(continut);

            if (data.rol == "Profesor") profi.Add(numeUser);
            else if (data.rol == "Elev") elevi.Add(numeUser);
        }

        dropdownSelectieClasa.AddOptions(clase);
        dropdownSelectieProfesor.AddOptions(profi);
        dropdownSelectieElev.AddOptions(elevi);
    }

    public void Admin_AsigneazaProf()
    {
        if (dropdownSelectieClasa.options.Count == 0 || dropdownSelectieProfesor.options.Count == 0) return;

        string clasaSelectata = dropdownSelectieClasa.options[dropdownSelectieClasa.value].text;
        string profSelectat = dropdownSelectieProfesor.options[dropdownSelectieProfesor.value].text;

        string caleClasa = Path.Combine(caleDBClase, clasaSelectata + ".json");
        if (File.Exists(caleClasa))
        {
            string continut = File.ReadAllText(caleClasa);
            ClassData data = JsonUtility.FromJson<ClassData>(continut);

            data.idProfesor = profSelectat;
            File.WriteAllText(caleClasa, JsonUtility.ToJson(data, true));

            if (textFeedbackAlocare != null) { textFeedbackAlocare.text = $"Profesorul {profSelectat} asignat la {clasaSelectata}!"; textFeedbackAlocare.color = Color.green; }
        }
    }

    public void Admin_AsigneazaElev()
    {
        if (dropdownSelectieClasa.options.Count == 0 || dropdownSelectieElev.options.Count == 0) return;

        string clasaSelectata = dropdownSelectieClasa.options[dropdownSelectieClasa.value].text;
        string elevSelectat = dropdownSelectieElev.options[dropdownSelectieElev.value].text;

        string caleClasa = Path.Combine(caleDBClase, clasaSelectata + ".json");
        if (File.Exists(caleClasa))
        {
            string continut = File.ReadAllText(caleClasa);
            ClassData data = JsonUtility.FromJson<ClassData>(continut);

            if (!data.idElevi.Contains(elevSelectat))
            {
                data.idElevi.Add(elevSelectat);
                File.WriteAllText(caleClasa, JsonUtility.ToJson(data, true));
                if (textFeedbackAlocare != null) { textFeedbackAlocare.text = $"Elevul {elevSelectat} a fost inscris in {clasaSelectata}!"; textFeedbackAlocare.color = Color.green; }
            }
            else
            {
                if (textFeedbackAlocare != null) { textFeedbackAlocare.text = "Elevul este deja in clasa!"; textFeedbackAlocare.color = Color.yellow; }
            }
        }
    }

    public void DeschideIstoric()
    {
        if (rolCurent != "Profesor") return;

        historyPanel.SetActive(true);
        if (profesorDashboardPanel) profesorDashboardPanel.SetActive(false);

        if (renamePanel != null) renamePanel.SetActive(false);
        povestiSelectate.Clear();
        ActualizeazaButoaneActiune();

        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }

        string caleSertarProf = Path.Combine(caleDBProfesori, utilizatorCurent);

        if (Directory.Exists(caleSertarProf))
        {
            string[] folderePovesti = Directory.GetDirectories(caleSertarProf);
            foreach (string folder in folderePovesti)
            {
                string folderCurent = folder;
                GameObject btnGO = Instantiate(storyButtonPrefab, listContainer);
                string numeFolder = Path.GetFileName(folder);
                btnGO.GetComponentInChildren<TextMeshProUGUI>().text = numeFolder.Replace("_", " ");

                btnGO.GetComponent<Button>().onClick.AddListener(() => IncarcaPovesteSalvata(folderCurent));

                Toggle toggle = btnGO.GetComponentInChildren<Toggle>();
                if (toggle != null)
                {
                    toggle.isOn = false;
                    toggle.onValueChanged.AddListener((isChecked) => OnToggleSelectie(folderCurent, isChecked));
                }
            }
        }
    }

    public void InchideIstoric()
    {
        historyPanel.SetActive(false);
        if (profesorDashboardPanel) profesorDashboardPanel.SetActive(true);
    }

    public void IncarcaPovesteSalvata(string caleCompleta)
    {
        customStoryPath = caleCompleta;
        historyPanel.SetActive(false);
        PornestePovestea();
    }

    private void OnToggleSelectie(string caleFolder, bool isSelected)
    {
        if (isSelected && !povestiSelectate.Contains(caleFolder)) povestiSelectate.Add(caleFolder);
        else if (!isSelected && povestiSelectate.Contains(caleFolder)) povestiSelectate.Remove(caleFolder);

        ActualizeazaButoaneActiune();
    }

    private void ActualizeazaButoaneActiune()
    {
        if (btnSterge != null) btnSterge.interactable = povestiSelectate.Count > 0;
        if (btnRedenumeste != null) btnRedenumeste.interactable = povestiSelectate.Count == 1;
    }

    public void StergePovestiSelectate()
    {
        foreach (string cale in povestiSelectate)
        {
            if (Directory.Exists(cale)) Directory.Delete(cale, true);
        }
        povestiSelectate.Clear();
        DeschideIstoric();
    }

    public void DeschidePanelRedenumire()
    {
        if (povestiSelectate.Count == 1)
        {
            string caleVeche = povestiSelectate[0];
            inputNumeNou.text = Path.GetFileName(caleVeche).Replace("_", " ");
            renamePanel.SetActive(true);
        }
    }

    public void ConfirmaRedenumire()
    {
        if (povestiSelectate.Count != 1 || string.IsNullOrEmpty(inputNumeNou.text)) return;

        string caleVeche = povestiSelectate[0];
        string directorBaza = Path.GetDirectoryName(caleVeche);

        string numeNouSigur = string.Join("_", inputNumeNou.text.Split(Path.GetInvalidFileNameChars()));
        string caleNoua = Path.Combine(directorBaza, numeNouSigur);

        if (!Directory.Exists(caleNoua))
        {
            Directory.Move(caleVeche, caleNoua);
            renamePanel.SetActive(false);
            DeschideIstoric();
        }
        else
        {
            Debug.LogWarning("⚠️ Exista deja o poveste cu acest nume!");
        }
    }

    public void InchidePanelRedenumire()
    {
        renamePanel.SetActive(false);
    }

    public void SaveCurrentStory()
    {
        if (rolCurent != "Profesor")
        {
            Debug.LogWarning("⚠️ Doar profesorii pot salva lectii noi!");
            return;
        }

        if (inputNumeSalvare == null || string.IsNullOrEmpty(inputNumeSalvare.text))
        {
            if (textFeedbackPublicare != null) { textFeedbackPublicare.text = "Scrie un nume pt lectie inainte sa salvezi!"; textFeedbackPublicare.color = Color.red; }
            return;
        }

        string numeNou = inputNumeSalvare.text.Trim();
        string titluSigur = string.Join("_", numeNou.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");

        string folderSalvare = Path.Combine(caleDBProfesori, utilizatorCurent, titluSigur);

        if (Directory.Exists(folderSalvare))
        {
            if (textFeedbackPublicare != null) { textFeedbackPublicare.text = "Exista deja o lectie cu acest nume!"; textFeedbackPublicare.color = Color.red; }
            return;
        }

        Directory.CreateDirectory(folderSalvare);

        File.Copy(Path.Combine(Application.streamingAssetsPath, "story.json"), Path.Combine(folderSalvare, "story.json"));

        string[] imagini = Directory.GetFiles(Application.streamingAssetsPath, "*.jpg");
        foreach (string img in imagini) File.Copy(img, Path.Combine(folderSalvare, Path.GetFileName(img)));

        string[] sunete = Directory.GetFiles(Application.streamingAssetsPath, "*.mp3");
        foreach (string aud in sunete) File.Copy(aud, Path.Combine(folderSalvare, Path.GetFileName(aud)));

        ultimulDraftSalvat = titluSigur;

        Debug.Log("<color=green>💾 Draft salvat cu succes in: </color>" + folderSalvare);
        if (textFeedbackPublicare != null) { textFeedbackPublicare.text = "Lectie salvata! Acum o poti publica."; textFeedbackPublicare.color = Color.green; }
    }

    public void Profesor_IncarcaClase()
    {
        if (rolCurent != "Profesor") return;

        if (dropdownClaseProfesor != null) dropdownClaseProfesor.ClearOptions();
        if (dropdownClaseVizualizare != null) dropdownClaseVizualizare.ClearOptions();

        List<string> claseleMele = new List<string>();

        string[] fisiereClase = Directory.GetFiles(caleDBClase, "*.json");
        foreach (string file in fisiereClase)
        {
            string continut = File.ReadAllText(file);
            ClassData data = JsonUtility.FromJson<ClassData>(continut);

            if (data.idProfesor == utilizatorCurent)
            {
                claseleMele.Add(data.idClasa);
            }
        }

        if (dropdownClaseProfesor != null) dropdownClaseProfesor.AddOptions(claseleMele);
        if (dropdownClaseVizualizare != null) dropdownClaseVizualizare.AddOptions(claseleMele);
    }

    public void Profesor_PublicaLectieDupaSalvare()
    {
        if (string.IsNullOrEmpty(ultimulDraftSalvat))
        {
            if (textFeedbackPublicare != null) { textFeedbackPublicare.text = "Trebuie intai sa salvezi lectia!"; textFeedbackPublicare.color = Color.red; }
            return;
        }

        if (dropdownClaseProfesor == null || dropdownClaseProfesor.options.Count == 0) return;

        string clasaSelectata = dropdownClaseProfesor.options[dropdownClaseProfesor.value].text;

        string caleClasa = Path.Combine(caleDBClase, clasaSelectata + ".json");
        ClassData dateClasa = JsonUtility.FromJson<ClassData>(File.ReadAllText(caleClasa));

        if (!dateClasa.lectiiGenerate.Contains(ultimulDraftSalvat))
        {
            dateClasa.lectiiGenerate.Add(ultimulDraftSalvat);
            File.WriteAllText(caleClasa, JsonUtility.ToJson(dateClasa, true));
        }
        else
        {
            if (textFeedbackPublicare != null) { textFeedbackPublicare.text = "Deja trimisa la aceasta clasa!"; textFeedbackPublicare.color = Color.yellow; }
            return;
        }

        foreach (string elev in dateClasa.idElevi)
        {
            string caleProgresElev = Path.Combine(caleDBProgres, elev + "_progres.json");
            if (File.Exists(caleProgresElev))
            {
                ElevProgresData progres = JsonUtility.FromJson<ElevProgresData>(File.ReadAllText(caleProgresElev));

                string statusNou = (progres.progresLectii.Count == 0) ? "Deblocat" : "Blocat";

                bool oAreDeja = false;
                foreach (var p in progres.progresLectii) { if (p.idLectie == ultimulDraftSalvat) oAreDeja = true; }

                if (!oAreDeja)
                {
                    LectieProgres lectieNoua = new LectieProgres
                    {
                        idLectie = ultimulDraftSalvat,
                        status = statusNou,
                        incercariRamase = 3,
                        scorCurent = 0
                    };
                    progres.progresLectii.Add(lectieNoua);
                    File.WriteAllText(caleProgresElev, JsonUtility.ToJson(progres, true));
                }
            }
        }

        if (textFeedbackPublicare != null) { textFeedbackPublicare.text = "Alocata cu succes!"; textFeedbackPublicare.color = Color.green; }
    }

    public void Profesor_AfiseazaEleviClasa()
    {
        if (dropdownClaseVizualizare == null || dropdownClaseVizualizare.options.Count == 0) return;

        string clasaSelectata = dropdownClaseVizualizare.options[dropdownClaseVizualizare.value].text;
        string caleClasa = Path.Combine(caleDBClase, clasaSelectata + ".json");

        if (File.Exists(caleClasa))
        {
            ClassData data = JsonUtility.FromJson<ClassData>(File.ReadAllText(caleClasa));

            if (data.idElevi.Count == 0)
            {
                if (textListaElevi != null) textListaElevi.text = "Nu exista elevi inscrisi in aceasta clasa inca.";
                return;
            }

            string afisaj = $"Elevii clasei {clasaSelectata}:\n\n";
            foreach (string elev in data.idElevi)
            {
                afisaj += $"• {elev.ToUpper()}\n";
            }

            if (textListaElevi != null) textListaElevi.text = afisaj;
        }
    }

    public void Elev_IncarcaDashboard()
    {
        if (elevDashboardPanel) elevDashboardPanel.SetActive(true);
        if (elevLectiiPanel) elevLectiiPanel.SetActive(false);
        if (elevProgresPanel) elevProgresPanel.SetActive(false);

        string clasaLui = "Nerepartizat";
        if (Directory.Exists(caleDBClase))
        {
            string[] fisiereClase = Directory.GetFiles(caleDBClase, "*.json");
            foreach (string f in fisiereClase)
            {
                ClassData cd = JsonUtility.FromJson<ClassData>(File.ReadAllText(f));
                if (cd.idElevi.Contains(utilizatorCurent))
                {
                    clasaLui = cd.idClasa.Replace("_", " ").ToUpper();
                    break;
                }
            }
        }

        string numeFormatat = char.ToUpper(utilizatorCurent[0]) + utilizatorCurent.Substring(1);
        if (textWelcomeElev) textWelcomeElev.text = $"Salut, {numeFormatat}!\nEști înrolat(ă) în clasa: {clasaLui}";
    }

    public void Elev_DeschideLectii()
    {
        elevDashboardPanel.SetActive(false);
        elevLectiiPanel.SetActive(true);
        Elev_IncarcaLectii();
    }

    public void Elev_DeschideProgres()
    {
        if (elevDashboardPanel) elevDashboardPanel.SetActive(false);
        if (elevProgresPanel) elevProgresPanel.SetActive(true);

        int totalLectiiClasa = 0;
        string[] fisiereClase = Directory.GetFiles(caleDBClase, "*.json");
        foreach (string f in fisiereClase)
        {
            ClassData cd = JsonUtility.FromJson<ClassData>(File.ReadAllText(f));
            if (cd.idElevi.Contains(utilizatorCurent))
            {
                totalLectiiClasa = cd.lectiiGenerate.Count;
                break;
            }
        }

        string caleProgres = Path.Combine(caleDBProgres, utilizatorCurent + "_progres.json");
        if (File.Exists(caleProgres))
        {
            ElevProgresData progres = JsonUtility.FromJson<ElevProgresData>(File.ReadAllText(caleProgres));
            int lectiiCompletate = 0;
            string detaliiNote = "";

            foreach (var l in progres.progresLectii)
            {
                if (l.status == "Finalizat")
                {
                    lectiiCompletate++;

                    string istoricAratat = (l.istoricNote != null && l.istoricNote.Count > 0)
                        ? string.Join(", ", l.istoricNote)
                        : l.scorCurent.ToString();

                    detaliiNote += $"• {l.idLectie.Replace("_", " ")}:\n" +
                                   $"  - Views: {l.numarRedari}\n" +
                                   $"  - Grade history: [{istoricAratat}] pts\n\n";
                }
            }

            if (textStatsElev != null)
            {
                textStatsElev.text = $"YOUR PROGRESS\n\nLessons completed: {lectiiCompletate} of {totalLectiiClasa}\n\n" + detaliiNote;
                if (lectiiCompletate == 0) textStatsElev.text += "You haven't completed any lessons yet.";
            }
        }
    }

    public void Elev_InapoiLaDashboard()
    {
        elevLectiiPanel.SetActive(false);
        elevProgresPanel.SetActive(false);
        elevDashboardPanel.SetActive(true);
    }

    public void Logout()
    {
        utilizatorCurent = "Vizitator";
        rolCurent = "";
        temaCurenta = "";

        if (adminPanel) adminPanel.SetActive(false);
        if (profesorDashboardPanel) profesorDashboardPanel.SetActive(false);
        if (elevPanel) elevPanel.SetActive(false);
        if (historyPanel) historyPanel.SetActive(false);

        loginPanel.SetActive(true);
        CurataCampuri();
    }

    public void Elev_IncarcaLectii()
    {
        if (containerLectiiElev == null || butonLectieElevPrefab == null) return;

        foreach (Transform child in containerLectiiElev)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        string caleProgres = Path.Combine(caleDBProgres, utilizatorCurent + "_progres.json");
        if (!File.Exists(caleProgres)) return;

        ElevProgresData progres = JsonUtility.FromJson<ElevProgresData>(File.ReadAllText(caleProgres));

        // 1. FILTRU INTELIGENT: Găsim toate lecțiile care există fizic pe disc
        List<LectieProgres> lectiiValide = new List<LectieProgres>();

        foreach (LectieProgres lectie in progres.progresLectii)
        {
            string idLectieCurenta = lectie.idLectie;
            bool lectiaExistaFizic = false;

            string[] foldereProf = Directory.GetDirectories(caleDBProfesori);
            foreach (string folderProf in foldereProf)
            {
                if (Directory.Exists(Path.Combine(folderProf, idLectieCurenta)))
                {
                    lectiaExistaFizic = true;
                    break;
                }
            }

            // Păstrăm în memorie doar lecțiile care încă există
            if (lectiaExistaFizic)
            {
                lectiiValide.Add(lectie);
            }
        }

        // 2. AUTO-REPARARE PRERECHIZITE: Ne asigurăm că prima lecție validă rămasă este deblocată
        bool sADeblocatPrima = false;
        for (int i = 0; i < lectiiValide.Count; i++)
        {
            // Dacă o lecție e finalizată, lăsăm statusul ei intact
            if (lectiiValide[i].status == "Finalizat")
            {
                continue;
            }

            // Găsim prima lecție care NU e finalizată
            if (!sADeblocatPrima)
            {
                lectiiValide[i].status = "Deblocat";
                if (lectiiValide[i].incercariRamase <= 0) lectiiValide[i].incercariRamase = 3;
                sADeblocatPrima = true;
            }
            else
            {
                // Toate celelalte de după ea devin blocate
                lectiiValide[i].status = "Blocat";
            }
        }

        // 3. Salvăm noul progres curățat în baza de date locală, păstrând notele vechi
        progres.progresLectii = lectiiValide;
        File.WriteAllText(caleProgres, JsonUtility.ToJson(progres, true));

        // 4. Desenăm butoanele pe ecran
        foreach (LectieProgres lectie in lectiiValide)
        {
            string idLectieCurenta = lectie.idLectie;

            GameObject btnGO = Instantiate(butonLectieElevPrefab, containerLectiiElev);
            btnGO.transform.localScale = Vector3.one; // S-a corectat din .identity / .Vector3.identity la Vector3.one

            TextMeshProUGUI[] texte = btnGO.GetComponentsInChildren<TextMeshProUGUI>();

            if (texte.Length >= 1) texte[0].text = lectie.idLectie.Replace("_", " ");

            if (texte.Length >= 2)
            {
                if (lectie.status == "Blocat")
                    texte[1].text = "[BLOCAT] Rezolva lectia anterioara";
                else if (lectie.status == "Deblocat")
                    texte[1].text = $"[DISPONIBIL] Incercari ramase: {lectie.incercariRamase}";
                else if (lectie.status == "Finalizat")
                    texte[1].text = $"[FINALIZAT] Scor: {lectie.scorCurent} pct";
            }

            Button btn = btnGO.GetComponent<Button>();

            if (lectie.status == "Blocat" || lectie.incercariRamase == 0)
            {
                btn.interactable = false;
            }
            else
            {
                btn.interactable = true;
                btn.onClick.AddListener(() => Elev_PornesteLectia(idLectieCurenta));
            }
        }
    }

    public void Elev_PornesteLectia(string idLectie)
    {
        string rootPathLectie = "";

        string[] foldereProf = Directory.GetDirectories(caleDBProfesori);
        foreach (string folderProf in foldereProf)
        {
            string posibilaCale = Path.Combine(folderProf, idLectie);
            if (Directory.Exists(posibilaCale))
            {
                rootPathLectie = posibilaCale;
                break;
            }
        }

        if (string.IsNullOrEmpty(rootPathLectie))
        {
            Debug.LogError(" Nu am gasit lectia fizic pe hard disk!");
            return;
        }

        elevPanel.SetActive(false);
        customStoryPath = rootPathLectie;
        temaCurenta = idLectie;

        string caleProgres = Path.Combine(caleDBProgres, utilizatorCurent + "_progres.json");
        if (File.Exists(caleProgres))
        {
            ElevProgresData progres = JsonUtility.FromJson<ElevProgresData>(File.ReadAllText(caleProgres));
            foreach (var l in progres.progresLectii)
            {
                if (l.idLectie == idLectie)
                {
                    l.numarRedari++;
                    break;
                }
            }
            File.WriteAllText(caleProgres, JsonUtility.ToJson(progres, true));
        }

        PornestePovestea();
    }

    public void Elev_FinalizeazaQuiz(int notaObtinuta)
    {
        if (rolCurent != "Elev") return;

        string caleProgres = Path.Combine(caleDBProgres, utilizatorCurent + "_progres.json");
        if (!File.Exists(caleProgres)) return;

        ElevProgresData progres = JsonUtility.FromJson<ElevProgresData>(File.ReadAllText(caleProgres));
        bool gasitCurenta = false;

        for (int i = 0; i < progres.progresLectii.Count; i++)
        {
            if (progres.progresLectii[i].idLectie == temaCurenta)
            {
                progres.progresLectii[i].status = "Finalizat";
                progres.progresLectii[i].scorCurent = notaObtinuta;

                if (progres.progresLectii[i].istoricNote == null)
                    progres.progresLectii[i].istoricNote = new List<int>();

                progres.progresLectii[i].istoricNote.Add(notaObtinuta);

                if (i + 1 < progres.progresLectii.Count)
                {
                    if (progres.progresLectii[i + 1].status == "Blocat")
                    {
                        progres.progresLectii[i + 1].status = "Deblocat";
                        progres.progresLectii[i + 1].incercariRamase = 3;
                    }
                }
                gasitCurenta = true;
                break;
            }
        }

        if (gasitCurenta)
        {
            File.WriteAllText(caleProgres, JsonUtility.ToJson(progres, true));
            if (endPanel != null) endPanel.SetActive(false);
            if (elevDashboardPanel != null) elevDashboardPanel.SetActive(false);
            Elev_IncarcaDashboard();
        }
    }

    public void DeschideExplorePanel()
    {
        explorePanel.SetActive(true);
        if (adminPanel) adminPanel.SetActive(false);
        if (profesorDashboardPanel) profesorDashboardPanel.SetActive(false);
        if (elevPanel) elevPanel.SetActive(false);
    }

    public void InchideExplorePanel()
    {
        explorePanel.SetActive(false);
        if (rolCurent == "Elev" && elevPanel != null) elevPanel.SetActive(true);
        else if (rolCurent == "Profesor" && profesorDashboardPanel != null) profesorDashboardPanel.SetActive(true);
    }

    public void StartExploreMode()
    {
        if (dropdownLimbaExplore != null) limbaSelectataPentruExplore = dropdownLimbaExplore.options[dropdownLimbaExplore.value].text;

        explorePanel.SetActive(false);
        if (btnExitExplore != null) btnExitExplore.SetActive(true);

        foreach (var actor in activeActors.Values) Destroy(actor);
        activeActors.Clear();

        if (libraryPrefabs.Count > 0)
        {
            GameObject jucatorExplore = Instantiate(libraryPrefabs[0], new Vector3(0, 0, 0), Quaternion.identity);
            activeActors.Add("jucator_explore", jucatorExplore);

            if (vcamGeneral != null)
            {
                vcamGeneral.Follow = jucatorExplore.transform;
                vcamGeneral.LookAt = jucatorExplore.transform;
                vcamGeneral.Priority = 10;
                if (vcamMonitor != null) vcamMonitor.Priority = 5;
            }

            ActorController controller = jucatorExplore.GetComponent<ActorController>();
            if (controller != null) controller.poateMisca = true;
        }
    }

    public void StopExploreMode()
    {
        if (btnExitExplore != null) btnExitExplore.SetActive(false);

        if (rolCurent == "Elev" && elevPanel != null) elevPanel.SetActive(true);
        else if (rolCurent == "Profesor" && profesorDashboardPanel != null) profesorDashboardPanel.SetActive(true);

        if (activeActors.ContainsKey("jucator_explore"))
        {
            Destroy(activeActors["jucator_explore"]);
            activeActors.Remove("jucator_explore");
        }

        if (textSubtitrare != null) textSubtitrare.text = "";
    }

    public void PornestePovesteaNoua()
    {
        customStoryPath = "";
        PornestePovestea();
    }

    private void PornestePovestea()
    {
        Time.timeScale = 1f;
        if (endPanel != null) endPanel.SetActive(false);
        vcamGeneral.Priority = 10;
        vcamMonitor.Priority = 5;
        StartCoroutine(LoadAndRunStory());
    }

    IEnumerator LoadAndRunStory()
    {
        string rootPath = string.IsNullOrEmpty(customStoryPath) ? Application.streamingAssetsPath : customStoryPath;
        string filePath = Path.Combine(rootPath, "story.json");

        // Trecem direct la citire, fără să mai așteptăm după procese externe sau să ștergem fișiere
        if (!File.Exists(filePath))
        {
            Debug.LogError("❌ Fișierele lipsesc sau nu au fost găsite la calea: " + filePath);
            ArataEndPanel();
            yield break;
        }

        // Citirea datelor existente pe disc
        string jsonContent = File.ReadAllText(filePath);

        // --- SISTEM DE AVARIE ANTI-BLOCAJ ---
        if (jsonContent.Contains("AI_FAILED"))
        {
            Debug.LogError("❌ Generarea a eșuat. Fișier de avarie detectat.");
            ArataEndPanel();
            yield break;
        }
        // ------------------------------------

        StoryData story = JsonUtility.FromJson<StoryData>(jsonContent);

        // Resetăm starea ecranului magic înainte de a randa acțiunile
        if (magicScreen != null && magicScreen.material != null)
        {
            magicScreen.material.mainTexture = null;
        }

        // Generarea mediului și rularea acțiunilor direct
        foreach (var actorData in story.actors)
        {
            SpawnActor(actorData.prefabName, actorData.id, actorData.initialPosition.ToVector3());
        }

        foreach (var action in story.actions)
        {
            if (action.camera == "monitor") { vcamMonitor.Priority = 20; }
            else { vcamMonitor.Priority = 5; }

            if (!string.IsNullOrEmpty(action.imageFile))
            {
                yield return StartCoroutine(LoadAndDisplayImage(action.imageFile, rootPath));
            }

            if (!string.IsNullOrEmpty(action.animation))
            {
                TriggerAnimation(action.actorId, action.animation);
            }

            if (action.type == "MOVE")
            {
                yield return StartCoroutine(MoveActor(action.actorId, action.targetPos.ToVector3(), 2f));
            }

            if (action.type == "SPEAK" || action.type == "SHOW_IMAGE")
            {
                yield return StartCoroutine(PlayDynamicAudio(action.audioFile, action.text, rootPath));
            }
        }

        if (quizManager != null && story.quiz != null && story.quiz.Count > 0)
        {
            //  Trimitem lista completă de întrebări (List<QuizData>) direct către noul QuizManager
            yield return StartCoroutine(AsteaptaQuizSet(story.quiz));
        }
        else
        {
            ArataEndPanel();
        }
    }

    IEnumerator AsteaptaQuizSet(List<QuizData> setIntrebari)
    {
        // Transmitem setul complet de întrebări către manager
        quizManager.ArataQuiz(setIntrebari);

        // Cât timp panoul de quiz este activ pe ecran, menținem așteptarea
        while (quizManager.quizPanel != null && quizManager.quizPanel.activeSelf)
        {
            yield return null;
        }

        ArataEndPanel();
    }



    void ArataEndPanel()
    {
        if (rolCurent == "Profesor")
        {
            if (endPanel != null) endPanel.SetActive(true);
        }
        else if (rolCurent == "Elev")
        {
            if (elevPanel != null) elevPanel.SetActive(true);
            Elev_DeschideProgres();
        }
    }

    public void ReplayStory()
    {
        endPanel.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(LoadAndRunStory());
    }

    public void ExitToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // ==========================================
    // MEDIA & SPATIALITATE (DEBRANSATE DE LA METADATE)
    // ==========================================
    IEnumerator LoadAndDisplayImage(string fileName, string rootPath)
    {
        string path = "file://" + Path.Combine(rootPath, fileName).Replace("\\", "/");
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(path))
        {
            www.timeout = 10;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                if (magicScreen != null && magicScreen.material != null)
                {
                    magicScreen.material.mainTexture = texture;
                }
            }
            else
            {
                Debug.LogWarning(" Nu am putut afisa textura de pe disc: " + www.error);
            }
        }
    }

    IEnumerator PlayDynamicAudio(string fileName, string spokenText, string rootPath)
    {
        textSubtitrare.text = spokenText;
        if (!string.IsNullOrEmpty(fileName))
        {
            string path = "file://" + Path.Combine(rootPath, fileName).Replace("\\", "/");
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG))
            {
                www.timeout = 15;
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null && sursaAudio != null)
                    {
                        sursaAudio.PlayOneShot(clip);
                        yield return new WaitForSeconds(clip.length);
                    }
                }
                else
                {
                    Debug.LogWarning(" Nu am putut reda audio-ul de pe disc: " + www.error);
                }
            }
        }
        textSubtitrare.text = "";
    }

    void SpawnActor(string prefabName, string id, Vector3 pos)
    {
        GameObject prefab = libraryPrefabs.Find(p => p != null && p.name.Contains(prefabName));
        if (prefab != null && !activeActors.ContainsKey(id))
        {
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            activeActors.Add(id, go);
            if (vcamGeneral != null)
            {
                vcamGeneral.Follow = go.transform;
                vcamGeneral.LookAt = go.transform;
            }
        }
    }

    void TriggerAnimation(string id, string trigger)
    {
        if (activeActors.TryGetValue(id, out GameObject actor) && actor != null)
        {
            Animator anim = actor.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetTrigger(trigger);
        }
    }

    IEnumerator MoveActor(string id, Vector3 dest, float time)
    {
        if (activeActors.TryGetValue(id, out GameObject actor) && actor != null)
        {
            actor.transform.DOLookAt(dest, 0.5f);
            actor.GetComponentInChildren<Animator>()?.SetBool("isWalking", true);
            yield return actor.transform.DOMove(dest, time).WaitForCompletion();
            actor.GetComponentInChildren<Animator>()?.SetBool("isWalking", false);
        }
    }

    public void CurataImaginiVechi()
    {
        string path = Application.streamingAssetsPath;
        if (!Directory.Exists(path)) return;

        string[] jpgFiles = Directory.GetFiles(path, "*.jpg");
        foreach (string file in jpgFiles) File.Delete(file);

        string[] mp3Files = Directory.GetFiles(path, "*.mp3");
        foreach (string file in mp3Files) File.Delete(file);
    }
}