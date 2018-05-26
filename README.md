# ESigner
First release of ESigner. A document signing application for smartcards.

E-Signer a electronic signer application for Smartcards (C#). It gets input data as string with parameters or a file and returns signed data as string.

ESigner.exe gets three parameters. First is the source XML data. If you want to use an XML file as the source then send the first parameter empty "" and send the path of the XML file as the second parameter. Third is CardType like "AKIS".

Returns signed XML as string. Usage: cmd /C ""ESigner.exe" "XML string" "Path of XML file (Optional)" "AKIS""

TÜrkçe: ESigner.exe üç parametre alır. İlki imzalanacak XML string. XML dosyadan okunacaksa ilk parametreyi boş ( "" ) geçip ikinci parametrede kaynak dosyanın yolunu verin. Eğer iki parametre de gönderilmişse ilki (string xml) dikkate alınır. Üçüncü parametre kart tipidir, "SAFESIGN", "AKIS" gibi.

İmzalama gerçekleşince program kapanır ve imzalanmış veriyi string olarak döndürür.

Komut satırı: cmd /C ""ESigner.exe" "....." "kaynak xml yolu" "AKIS""

ibrahim KAZANCI Healthy HBYS www.hbys.web.tr

https://plus.google.com/+ibrahimKazanci_1

