eimzaOS-2.0.2.zip: 			xades 2.0.2 dll'leri ile hazırlanmış exe
EImzaOS-Project-Complete-2.0.2.7z: 	xades 2.0.2 dll'leri ile hazırlanmış kaynak dosyası

eimza.zip:				xades 1.4.15 dll'leri ile hazırlanmış exe
EImzaOS-Project-Complete.7z: 		xades 1.4.15 dll'leri ile hazırlanmış kaynak dosyası


Dll'lerin hedef bilgisayarlarda register edilmesi vs. gerekmiyor. Kendi bilgisayarimdan başka yeni kurulmuş iki bilgisayarda (XP Pro 32 bit ve Win 7 64 bit) denedim çalıştı, ekteki eimza.zip içindeki dosyalar aynen korundu ise ve net framework 3.5 kurulu ise sorun yok. Gerekirse ileride gerekli tüm dosyalar exe içerisine gömülerek programın dağıtım sorunu aşılabilir.

Tabii Akis yazılımının zaten yüklü olması gerekli. Önce akis ile akıllı karta ulaşabildiğinizi deneyin. Sonra program çalışacaktır.
İmza işlemini gerçekleştirebilmek için elinizdeki karta ait sertifikayı EImzaOS.exe'nin bulundugu klasörün altındaki trusted klasörüne import etmeniz gerekli.

Programın çalışması için gerekli dosyalar altta gereklilikler kısmında verildi.

Programın Çalışma şekli şöyle:
eImzaOS.exe iki parametre alıyor. EXE'ye ilk parametre olarak imzalanacak kaynak XML verisi string formatta gönderiliyor. EXE'nin kaynak XML olarak dosya kullanması isteniyorsa ilk parametre boş olarak ( "" şeklinde) geçilip ikinci parametre olarak kaynak dosyanın yolu veriliyor. Eğer iki parametre de gönderilmişse program ilk parametreyi (string xml'i) dikkate alıyor.

Komut satırı:
cmd /C eImzaOS.exe "<ereceteBilgisi>..........</ereceteBilgisi>" "kaynak xml dosyasi yolu"

İmzalama işlemi bittiğinde program kapanıyor ve imzalanmış veriyi yine string olarak döndürüyor. Aşağıda delphi üzerinden çalıştırma ve dönen veriyi alma örneği var:


Programı Delphi'den şöyle çalıştırıyorum:
var
  XML: WideString;
begin
  ...
  ...

  XML := XML + '<ereceteBilgisi>' +
  '  <tesisKodu>'+inttostr(frmMain.GSSTesisKodu)+'</tesisKodu>' +
  ...
  ...

    if Dosyadan = 0 then
    begin
      // hazirlanan XML textini gonder ve imzalanmis olarak geri al
      XML := frmMain.GetDosOutput(frmMain.LocalPath+'EImzaOS.exe '+'"'+XML+'"'+' "'+frmMain.LocalPath+'\"', frmMain.LocalPath);
    end
    else
    begin
      // XML'i dosyaya kaydet (dosya uzerinden islem yapmak isteniyorsa kullan)
      AssignFile(Logo,frmMain.LocalPath+'erecete.xml');
      Rewrite(Logo);
      Writeln(Logo,XML);
      CloseFile(Logo);
      XML := frmMain.GetDosOutput('EImzaOS.exe ""'+' "'+frmMain.LocalPath+'\"', frmMain.LocalPath);
    end;

    if XML = '' then
    begin
      frmRMemo.Memo1.Lines.Add('İmzalanmış reçete oluşmadı. Gönderim Yapılmadı');
      Exit;
    end;
    vImzaliEreceteGirisIstekDVO := ImzaliEreceteGirisIstekDVO.Create;
    //  imzaliErecete parametresi TByteDynArray iken string yaptim...
    vImzaliEreceteGirisIstekDVO.imzaliErecete := XML;
    vImzaliEreceteGirisIstekDVO.tesisKodu := frmMain.GSSTesisKodu;
  ...
  ...

Kullanmak isteyen olursa buyursun denesin...

eimza.zip yazılımın çalışır halidir. Kalan diğer tüm dosyalar ise kaynak kodlardır.
Visual C# 2010 Express ile hazırlanmıştır.

İbrahim KAZANCI
Healthy HBYS
www.hbys.web.tr

Gereklilikler:

EIMZAOS.exe'nin bulunduğu dizinde
  xmlsignature-config.xml
  EImzaOS.exe.config
  EImzaOS.vshost.exe.config
  certval-policy.xml
  asn1rt.dll
  log4net.dll
  ma3api-asn.dll
  ma3api-certstore.dll
  ma3api-certvalidation.dll
  ma3api-cmssignature.dll
  ma3api-common.dll
  ma3api-crypto-bouncyprovider.dll
  ma3api-crypto.dll
  ma3api-iaik_wrapper.dll
  ma3api-infra.dll
  ma3api-managedPkcs11.dll
  ma3api-mssclient-aveaprovider.dll
  ma3api-mssclient-turkcellprovider.dll
  ma3api-mssclient.dll
  ma3api-pkcs11net.dll
  ma3api-signature.dll
  ma3api-smartcard.dll
  ma3api-xmlsignature.dll
  nunit.framework.dll
  System.Data.SQLite.dll
dosyaları olmalıdır.(exe ile aynı klasörde)

Ayrıca exe'nin bulunduğu klasörün altında 4 klasör olmalıdır. Bunlar;
  trusted klasörü: akıllı karta ait .cer uzantılı sertifika import edilip kaydedilmelidir.
  lisans klasörü: lisansFree.xml adıyla lisans dosyası kayıtlı olmalıdır (path bilgisi lisanshelper.cs içinde yer alıyor. Dosya kamusm'den geliyor)
  en-US klasörü: (dll dosyaları)
  tr-TR klasörü: (dll dosyaları)


