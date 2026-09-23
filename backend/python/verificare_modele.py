import requests
import json

# PUNE CHEIA TA AICI
GOOGLE_API_KEY = "AQ.Ab8RN6IowVYUQfndtgGczm5rd9qOgiS0u-hMsYb4hahcrEPe4A" 

def listeaza_modele():
    print(" Interoghez Google despre modelele disponibile...")
    
    url = f"https://generativelanguage.googleapis.com/v1beta/models?key={GOOGLE_API_KEY}"
    
    try:
        response = requests.get(url)
        
        if response.status_code == 200:
            data = response.json()
            if "models" in data:
                print("\n Modele disponibile pentru cheia ta:")
                for model in data["models"]:
                    # Afisam doar modelele care pot genera continut (nu doar embeddings)
                    if "generateContent" in model["supportedGenerationMethods"]:
                        print(f"   • {model['name']}")
            else:
                print(" Nu am găsit nicio listă de modele în răspuns.")
                print(data)
        else:
            print(f" Eroare HTTP {response.status_code}:")
            print(response.text)
            
    except Exception as e:
        print(f" Eroare conexiune: {e}")

if __name__ == "__main__":
    listeaza_modele()