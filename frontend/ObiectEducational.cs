using UnityEngine;

// ==========================================
// MODULUL DE INTERACTIUNE SPATIALA (EVENT-DRIVEN ARCHITECTURE)
// Acest script este atasat pe obiectele 3D educationale (ex: birou, pat).
// El asteapta pasiv pana cand motorul fizic (PhysX) semnaleaza o coliziune.
// ==========================================
public class ObiectEducational : MonoBehaviour
{
    [Header("Resurse Audio Localizate")]
    // Referinte catre fisierele audio statice din proiect. 
    // Aceasta reprezinta o forma simpla de Localizare (L10n).
    public AudioClip audioEnglish;
    public AudioClip audioRomanian;
    public AudioClip audioItalian;
    public AudioClip audioSpanish;
    public AudioClip audioFrench;

    private AudioSource sursaAudio;
    private StoryDirector director;

    void Start()
    {
        // Generare dinamica a componentelor: 
        // Adaugam componenta AudioSource prin cod pentru a reduce munca manuala de setup in editorul Unity.
        sursaAudio = gameObject.AddComponent<AudioSource>();

        // Decuplare: Cautam in scena referinta catre Manager-ul central (StoryDirector).
        // Avem nevoie de el strict pentru a interoga "starea" curenta (State) a aplicatiei - adica limba aleasa de utilizator.
        director = FindFirstObjectByType<StoryDirector>();
    }

    // Aceasta este esenta arhitecturii bazate pe evenimente (Event-Driven).
    // In loc ca scriptul sa calculeze la fiecare cadru distanta in Update() (tehnica de tip Polling, ineficienta),
    // scriptul nu consuma resurse pana cand motorul fizic declanseaza acest eveniment (Trigger) cand obiectele se suprapun.
    void OnTriggerEnter(Collider other)
    {
        // Validarea tipului de obiect (Type Checking): 
        // Ne asiguram ca obiectul care a intrat in zona invizibila de coliziune este jucatorul, 
        // verificand daca poseda componenta logica 'ActorController'.
        if (other.GetComponent<ActorController>() != null)
        {
            // Mecanism de tip "Debounce" / Prevenire Spam:
            // Opreste suprapunerea sunetelor (ecoul) daca jucatorul face pasi mici, intrand si iesind rapid din raza trigger-ului.
            if (sursaAudio.isPlaying) return;

            // Preluam starea globala din manager (normalizare la lowercase pentru a evita erori de tip Case-Sensitive)
            string limba = director.limbaSelectataPentruExplore.ToLower();
            AudioClip clipDeRedat = null;

            // Logica de rutare audio (Audio Routing):
            // Selectam resursa corecta in functie de dropdown. Folosim functia string.Contains() 
            // pentru a acoperi atat diferente de spelling cat si variatii din interfata UI.
            if (limba.Contains("rom")) clipDeRedat = audioRomanian;
            else if (limba.Contains("eng")) clipDeRedat = audioEnglish;
            else if (limba.Contains("ita")) clipDeRedat = audioItalian;
            else if (limba.Contains("spa")) clipDeRedat = audioSpanish;
            else if (limba.Contains("fre") || limba.Contains("fra")) clipDeRedat = audioFrench;

            // Daca sistemul a alocat cu succes un clip localizat, trimitem instructiunea de redare catre placa de sunet.
            if (clipDeRedat != null)
            {
                sursaAudio.PlayOneShot(clipDeRedat);
            }
        }
    }
}