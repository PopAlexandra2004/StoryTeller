import requests

KEY = "AQ.Ab8RN6KROb_tA_Xi_fjtc0jqlpQt4RoaOPcq1W8kmvz3E8jT8w"
URL = f"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={KEY}"

data = {"contents": [{"parts": [{"text": "Say hello"}]}]}
res = requests.post(URL, json=data)

print(f"Status Code: {res.status_code}")
print(f"Raspuns: {res.text}")