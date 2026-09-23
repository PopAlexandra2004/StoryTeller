using UnityEngine;

// ==========================================
// MODULUL DE NAVIGARE FIZICA (GAMIFICATION LAYER)
// Controleaza miscarea avatarului 3D in modul 'Explore', bazandu-se strict pe motorul de fizica.
// ==========================================

// [RequireComponent] este o masura de siguranta arhitecturala (Dependency Management).
// Garanteaza ca acest script nu va da erori la runtime din cauza lipsei corpului fizic (Rigidbody).
// Daca pui scriptul pe un obiect, Unity adauga automat si Rigidbody-ul.
[RequireComponent(typeof(Rigidbody))]
public class ActorController : MonoBehaviour
{
    [Header("Parametri Cinematica")]
    public float viteza = 3.0f;
    public float vitezaRotatie = 10.0f;

    // Variabila de stare (State Flag) controlata din exterior (StoryDirector)
    // Determina daca jucatorul are voie sa trimita input de miscare la un moment dat.
    public bool poateMisca = false;

    // Caching de referinte pentru optimizare (evitam apelarea GetComponent in bucla)
    private Animator anim;
    private Rigidbody rb;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    // ==========================================
    // DE CE FIXED UPDATE? (Explicatie pentru prof)
    // Functia Update() ruleaza in functie de capacitatea placii video (poate rula de 30 sau de 144 de ori pe secunda).
    // Daca faceam calcule fizice acolo, pe un calculator bun personajul ar fi mers mai repede decat pe un laptop slab.
    // FixedUpdate() are un ciclu determinist (ex: fix 50 de ori pe secunda pe orice PC), 
    // fiind singurul loc corect pentru calculele motorului de fizica (Nvidia PhysX).
    // ==========================================
    void FixedUpdate()
    {
        // Early Exit: Daca nu ne aflam in modul Explore, ignoram logica pentru a economisi resurse
        if (!poateMisca) return;

        // Preluarea input-ului hardware (Tastatura: WASD sau Sageti). 
        // Returneaza valori intre -1 si 1.
        float miscareFataSpate = Input.GetAxis("Vertical");
        float miscareStangaDreapta = Input.GetAxis("Horizontal");

        // Construirea vectorului de directie.
        // Utilizarea '.normalized' este cruciala matematic: previne "exploit-ul" prin care 
        // mersul pe diagonala (apasand W si D simultan) ar insuma vectorii si ar misca personajul mai rapid.
        Vector3 directie = new Vector3(miscareStangaDreapta, 0.0f, miscareFataSpate).normalized;

        // Verificam magnitudinea (lungimea vectorului) pentru a sti daca utilizatorul chiar apasa o tasta
        if (directie.magnitude >= 0.1f)
        {
            // 1. ROTATIA (Interpolare Sferica)
            // Quaternion.LookRotation calculeaza unghiul matematic necesar pentru a privi catre vectorul de miscare.
            Quaternion rotatieTinta = Quaternion.LookRotation(directie);
            // Slerp (Spherical Linear Interpolation) asigura o tranzitie fina intre unghiul curent si cel tinta, 
            // inmultit cu Time.fixedDeltaTime pentru fluiditate independenta de framerate.
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, rotatieTinta, vitezaRotatie * Time.fixedDeltaTime));

            // 2. TRANSLATIA (Miscare bazata pe fizica)
            // Spre deosebire de transform.Translate (care doar redeseneaza pixelii ignorand coliziunile),
            // rb.MovePosition cere motorului de fizica sa mute masa obiectului catre noul vector.
            // Aceasta abordare declanseaza corect calculul de reactiune la contactul cu peretii (nu mai trecem prin pereti).
            rb.MovePosition(rb.position + directie * viteza * Time.fixedDeltaTime);

            // 3. ACTUALIZARE STARE UI/ANIMATIE
            if (anim != null) anim.SetBool("isWalking", true);
        }
        else
        {
            // Daca nu exista input, oprim animatia de miscare si trecem in starea "Idle"
            if (anim != null) anim.SetBool("isWalking", false);
        }
    }
}