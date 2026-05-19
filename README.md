# sysprog-2
## Tema projekta - Zadatak 3:
Kreirati Web server koji klijentu za zahtevane fajlove kreira zip arhivu i vraća je klijentu. Svi
zahtevi serveru se šalju preko browser-a korišćenjem GET metode. U zahtevu se kao parametri
navode nazivi fajlova za koje treba da se kreira zip arhiva. Ukoliko neki od navedenih fajlova ne
postoji na serveru, ostali fajlovi se arhiviraju. Ukoliko ne postoji ni jedan zahtevani fajl na serveru,
klijentu se prikazuje poruka. Za kreiranje zip arhive koristiti biblioteku SharpZipLib.

<ins>Realizacija pomoću Task-ova.</ins>

Strategija upravljanja keš memorijom: vremensko isticanje.

Primer poziva serveru: http://localhost:5050/test1.txt&test2.txt&test3.txt
