# StoryTeller

> An autonomous and scalable software framework that decouples pedagogical narrative content from rigid graphical resources, integrating a Unity 3D interactive frontend with a Python-driven artificial intelligence backend for real-time 3D learning environments and an integrated Learning Management System (LMS).
> 
> 

---

## 🏗️ System Architecture

StoryTeller uses a decoupled Client-Server and Middleware architecture to isolate heavy 3D graphical rendering from asynchronous AI generation logic:

* **Interactive 3D Client (Frontend):** Developed in Unity (C#), utilizing Cinemachine for dynamic camera tracking and DOTween for smooth procedural animations.


* **Backend Middleware:** Executed via Python through the Windows Subsystem for Linux (WSL), managing string sanitization, command-line arguments, and API orchestration.


* **External AI APIs:** Powered by Google Gemini (`gemini-2.5-flash`) for structured JSON narrative/quiz generation, Google TTS (`gTTS`) for audio synthesis, and Pollinations AI for dynamic texture generation.



---

## 🛠️ Technology Stack

* **Frontend Engine:** Unity 3D (C# .NET)


* **Backend Orchestration:** Python 3 (WSL)


* **AI & Multimedia Services:** Google Gemini API, Google Text-to-Speech (gTTS), Pollinations AI


* **Data Persistence:** Localized NoSQL document-based JSON storage (`Application.persistentDataPath`)



---

## 🚀 Key Features

* **Decoupled Architecture:** Separates pedagogical story generation from static graphical assets, allowing real-time 3D environment adaptation.


* **Role-Based Access Control (RBAC):** Secure authentication workflow routing users into **Admin**, **Teacher**, or **Student** dashboards.


* **Asynchronous IPC Pipeline:** Non-blocking communication bridge between Unity and the Python backend using process execution and frame-rate-safe coroutine polling (`while (!process.HasExited)`).


* **Fault Tolerance & Graceful Degradation:** Includes multi-model fallback tiers, automated JSON schema cleaning, and the `AI_FAILED` error protocol to prevent game engine crashes.


* **Automated Telemetry & Prerequisite Self-Healing:** Tracks student performance metrics, quiz attempts, and scores while automatically enforcing sequential curriculum locks.



---

## ⚙️ Installation and Deployment

1. **Clone or Transfer Repository:** Place the project source directory on your target system (e.g., `C:\Users\Public\Licenta_StoryAI`).


2. **Configure Python Environment (WSL):**
```bash
cd /mnt/c/Users/<Username>/Licenta_StoryAI
python3 -m venv venv
source venv/bin/activate
pip install --upgrade pip
pip install requests gTTS

```


3. **Configure API Keys:** Inject your secure Google Gemini API key into `generator_poveste.py` under the `GOOGLE_API_KEY` constant.


4. **Launch the Application:** Open the project in Unity 3D (version 2022.3 LTS or newer) or run the standalone Windows build (`StoryTeller.exe`).



---

## 📄 License & Academic Context

Developed as a Graduate License Thesis at the Technical University of Cluj-Napoca (UTCN), Faculty of Automation and Computer Science, Computer Science Department.
