import requests
import threading
import time

def get_zip(files: list[str], label: str):
    url = f"http://localhost:5050/{('&').join(files)}"
    start = time.time()
    r = requests.get(url)
    elapsed = round((time.time() - start) * 1000)
    
    if r.status_code == 200:
        print(f"[{label}] STATUS: {r.status_code} | {elapsed}ms | {len(r.content)} bytes")
    else:
        print(f"[{label}] STATUS: {r.status_code} | {elapsed}ms | {r.text.strip()}")

FILES = ["test1.txt", "test2.txt", "asdf.txt"]

print("\n1. Cache miss")
get_zip(FILES, "zahtev 1")

print("\n2. Cache hit")
get_zip(FILES, "zahtev 2")

print("\n3. Stampede test - jedan miss, ostalo hit")
FILES_2 = ["test2.txt", "asdf.txt"]
threads = [threading.Thread(target=get_zip, args=(FILES_2, f"zahtev {i+3}")) for i in range(5)]
for t in threads: t.start()
for t in threads: t.join()

print("\n4. Fajlovi koji ne postoje")
get_zip(["1.txt", "2.txt"], "zahtev 8")

print("\n5. Neki fajlovi su nepostojeci")
get_zip(["test1.txt", "1.txt"], "zahtev 9")

print("\n6. Prazan zahtev")
get_zip([], "zahtev 10")