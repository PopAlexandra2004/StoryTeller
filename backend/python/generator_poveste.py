"""
Sistem Backend de Generare Dinamica a Continutului (StoryAI)
Acest script actioneaza ca o punte intre motorul Unity si API-urile de Inteligenta Artificiala externe.
"""
import json
import os
import requests
import sys
import urllib.parse
from gtts import gTTS
import random
import time
import glob
import socket
import builtins

def print(*args, **kwargs):
    kwargs['flush'] = True
    builtins.print(*args, **kwargs)

socket.setdefaulttimeout(20)

UNITY_ASSETS_PATH = "/mnt/c/Users/andra/Licenta_StoryAI/Assets/StreamingAssets"
STORY_FILE = os.path.join(UNITY_ASSETS_PATH, "story.json")

# Cheia de autentificare pentru Google Gemini API
GOOGLE_API_KEY = "AQ.Ab8RN6IowVYUQfndtgGczm5rd9qOgiS0u-hMsYb4hahcrEPe4A" 

MODELE_POSIBILE = [
    "gemini-3.1-pro-preview", 
    "gemini-2.5-flash", 
    "gemini-2.0-flash"
]

LANG_MAP = {
    "romana": "ro",
    "engleza": "en",
    "spaniola": "es",
    "franceza": "fr",
    "italiana": "it"
}

def prompt_ingineresc(tema, hero_name, dimensiune_quiz, variante_quiz, l_baza, l_inv):
    durata_poveste_minute = 3
    numar_imagini = durata_poveste_minute * 2
    
    return f"""
    You are an adaptive Educational Director.
    
    TASK: Create a unique story based EXCLUSIVELY on the user's chosen theme, teach vocabulary and then generate a quiz.
    CURRENT THEME: "{tema}"
    
    INSTRUCTIONS:
    1. Everything you write MUST be about "{tema}". 
    2. Teach exactly {numar_imagini} different concepts/words related to the theme.
    3. FOR EACH concept, generate a separate 'SHOW_IMAGE' action in the actions list.
    4. QUIZ GENERATION: Generate exactly {dimensiune_quiz} questions in the 'quiz' array.
       - Each question must be related to the theme.
       - Each question must have exactly {variante_quiz} options in the 'options' array.
       - Provide the index of the correct answer in 'correct_index' (0-based).
       - FOR EVERY QUESTION, you MUST provide a Pollinations prompt in 'imagineEnunt' describing the question visually. DO NOT leave it empty!
       - For visual options set "tipIntrebare": "ImagineAB" and populate "optiuniImagini" with exact Pollinations prompts for each option.
    
    CONFIG:
    - Base Language: {l_baza} (For narration).
    - Learning Language: {l_inv} (For key vocabulary).
    - Hero: {hero_name}

    JSON STRUCTURE:
    {{
      "actors": [ {{ "id": "hero", "prefabName": "{hero_name}", "initialPosition": {{ "x": 0.5, "y": 0.05, "z": -1 }} }} ],
      "actions": [
         {{ "type": "SPEAK", "actorId": "hero", "text": "...", "animation": "doWave", "camera": "general" }},
         {{ "type": "SHOW_IMAGE", "text": "...", "imagePrompt": "...", "camera": "monitor" }}
      ],
      "quiz": [
         {{ 
           "question": "Question text?", 
           "options": ["Option 1", "Option 2", "Option 3"], 
           "correct_index": 0,
           "tipIntrebare": "Text",
           "imagineEnunt": "",
           "optiuniImagini": []
         }}
      ]
    }}
    Return ONLY JSON.
    """

def salveaza_progres_json(story_data):
    """ Salveaza faza curenta a povestii pe disc, prevenind blocajele Unity. """
    with open(STORY_FILE, 'w', encoding='utf-8') as f:
        json.dump(story_data, f, indent=4, ensure_ascii=False)

def genereaza_audio(story_data, lang_code):
    actions = story_data.get("actions", [])
    print(f"🔊 Generare Audio ({lang_code})...")
    
    for i, action in enumerate(actions):
        if action.get("type") in ["SPEAK", "SHOW_IMAGE"]:
            text = action.get("text", "")
            if not text: continue
            try:
                tts = gTTS(text=text, lang=lang_code, slow=False)
                filename = f"audio_{i}.mp3"
                tts.save(os.path.join(UNITY_ASSETS_PATH, filename))
                action["audioFile"] = filename
            except Exception as e:
                print(f"  Audio Error: {e}")
                
        salveaza_progres_json(story_data)
        
    return story_data

def descarca_si_salveaza_pollinations(prompt, filename_destinatie):
    headers_browser = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8"
    }
    safe_prompt = urllib.parse.quote(prompt)
    seed = random.randint(1, 100000)
    url = f"https://image.pollinations.ai/prompt/{safe_prompt}?width=800&height=600&nologo=true&seed={seed}"
    
    max_incercari = 3
    for incercare in range(max_incercari):
        print(f"   Descarcare [{filename_destinatie}] (incercarea {incercare + 1}/{max_incercari})...")
        time.sleep(2)
        try:
            res = requests.get(url, headers=headers_browser, timeout=20) 
            if res.status_code == 200:
                with open(os.path.join(UNITY_ASSETS_PATH, filename_destinatie), 'wb') as f:
                    f.write(res.content)
                print(f"  Fişierul {filename_destinatie} salvat cu succes!")
                return True
            elif res.status_code == 429:
                print("  ⚠️ Eroare 429. Pauza...")
                time.sleep(10)
        except requests.exceptions.Timeout:
            print(f"   Timeout la imaginea {filename_destinatie}.")
        except Exception as e:
            print(f"   Eroare conexiune: {e}")
    return False

def genereaza_imagini_securizat(story_data):
    print(" Generare Imagini cu Garanţie Secvenţială (1, 2, 3...)...")
    
    # Contor secvențial strict, indiferent de id-urile dictate de AI
    contor_secvential = 1
    
    # 1. Procesare imagini din poveste (Actions)
    actions = story_data.get("actions", [])
    for action in actions:
        if action.get("type") == "SHOW_IMAGE" and action.get("imagePrompt"):
            prompt = action["imagePrompt"]
            nume_secvential = f"imagine_{contor_secvential}.jpg"
            
            if descarca_si_salveaza_pollinations(prompt, nume_secvential):
                action["imageFile"] = nume_secvential
                salveaza_progres_json(story_data)
            
            contor_secvential += 1

    # 2. Procesare imagini din Quiz (Suport robust pentru ambele variante de chei)
    quiz_items = story_data.get("quiz", [])
    for q_item in quiz_items:
        # Verificăm imaginea de enunț (indiferent cum a numit-o Gemini: imagineEnunt sau enuntImagine)
        cheie_enunt = None
        if "imagineEnunt" in q_item and q_item["imagineEnunt"]:
            cheie_enunt = "imagineEnunt"
        elif "enuntImagine" in q_item and q_item["enuntImagine"]:
            cheie_enunt = "enuntImagine"
            
        if cheie_enunt:
            prompt_enunt = q_item[cheie_enunt]
            nume_secvential = f"imagine_{contor_secvential}.jpg"
            print(f"  Descarcare imagine enunt quiz: {prompt_enunt}")
            if descarca_si_salveaza_pollinations(prompt_enunt, nume_secvential):
                q_item[cheie_enunt] = nume_secvential
                salveaza_progres_json(story_data)
            contor_secvential += 1

        # Verificăm opțiunile de imagini (ImagineAB)
        if q_item.get("optiuniImagini"):
            optiuni_rezolvate = []
            for prompt_imagine in q_item["optiuniImagini"]:
                nume_secvential = f"imagine_{contor_secvential}.jpg"
                print(f"  Descarcare optiune imagine quiz: {prompt_imagine}")
                if descarca_si_salveaza_pollinations(prompt_imagine, nume_secvential):
                    optiuni_rezolvate.append(nume_secvential)
                    salveaza_progres_json(story_data)
                contor_secvential += 1
            q_item["optiuniImagini"] = optiuni_rezolvate

    return story_data

def ruleaza_ai(tema, hero, dim_quiz, var_quiz, l_baza, l_inv):
    print(f" EXECUTIE: {tema} | Quiz Questions: {dim_quiz} | Options: {var_quiz} | {l_baza} -> {l_inv}")

    print("🧹 Curatare cache...")
    try:
        if os.path.exists(STORY_FILE):
            os.remove(STORY_FILE)
            
        for f in glob.glob(os.path.join(UNITY_ASSETS_PATH, "imagine_*.jpg*")):
            os.remove(f)
        for f in glob.glob(os.path.join(UNITY_ASSETS_PATH, "audio_*.mp3*")):
            os.remove(f)
    except Exception as e:
        print(f"  Eroare curatare cache: {e}")
    
    prompt = prompt_ingineresc(tema, hero, dim_quiz, var_quiz, l_baza, l_inv)
    headers = {'Content-Type': 'application/json'}
    data = {"contents": [{"parts": [{"text": prompt}]}]}

    for model in MODELE_POSIBILE:
        url = f"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={GOOGLE_API_KEY}"
        for incercare in range(5): 
            try:
                res = requests.post(url, headers=headers, json=data, timeout=60) 
                
                if res.status_code == 200:
                    raw_text = res.json()['candidates'][0]['content']['parts'][0]['text']
                    
                    clean_json = raw_text.split('{', 1)[-1].rsplit('}', 1)[0]
                    clean_json = '{' + clean_json + '}'
                    story_data = json.loads(clean_json)
                    
                    audio_lang_code = LANG_MAP.get(l_baza.lower(), "ro")
                    
                    story_data = genereaza_audio(story_data, audio_lang_code)
                    #  Rulam noul sistem de generare/redenumire a imaginilor
                    story_data = genereaza_imagini_securizat(story_data)
                    
                    return story_data 
                
                elif res.status_code == 429:
                    wait_time = (incercare + 1) * 10
                    print(f"⚠️ Rate Limit! Aștept {wait_time}s...")
                    time.sleep(wait_time)
                else:
                    print(f"⚠️ Eroare Server {res.status_code}. Trec la alt model.")
                    break
            except requests.exceptions.Timeout:
                print(f" Modelul {model} a expirat (Timeout). Reîncerc...")
            except Exception as e:
                print(f" Eroare neasteptata: {e}")
                time.sleep(5)
            
    return None 

if __name__ == "__main__":
    final_data = None
    if len(sys.argv) >= 7:
        p_tema = sys.argv[1]
        p_hero = sys.argv[2]
        p_dim_quiz = sys.argv[3]
        p_var_quiz = sys.argv[4]
        p_l_baza = sys.argv[5]
        p_l_inv = sys.argv[6]
        
        final_data = ruleaza_ai(p_tema, p_hero, p_dim_quiz, p_var_quiz, p_l_baza, p_l_inv)
    
    if final_data:
        salveaza_progres_json(final_data)
        print(" Proces Finalizat cu succes.")
    else:
        with open(STORY_FILE, 'w', encoding='utf-8') as f:
            f.write('{"error": "AI_FAILED"}')
        print(" Procesul a esuat. Fisierul de avarie a foi creat.")