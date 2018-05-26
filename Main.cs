using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using tr.gov.tubitak.uekae.esya.api.common.util;
using tr.gov.tubitak.uekae.esya.api.common.util.bag;
using tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11;
using tr.gov.tubitak.uekae.esya.asn.util;
using utils;
using tr.gov.tubitak.uekae.esya.api.asn.x509;
using ProgramNameSpace;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
namespace SignatureHelper
{

    public partial class Main : Form
    {
        Dictionary<string, Assembly> _libs = new Dictionary<string, Assembly>();
        public Main()
        {    // dll embed icin
             // AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;







            //if (GetNetworkTime() > new DateTime(2016, 01, 05, 0, 0, 0, DateTimeKind.Utc))
            //{
            //    // 1;
            //}
            //else if (Program.himm == 1)
            //{ Program.himm = 0; }
            if (Program.ParamXML != "")
            {
                rbString.Checked = true;
                try
                {
                    txtXML.Text = System.Xml.Linq.XDocument.Parse(Program.ParamXML).ToString(); // Program.ParamXML;
                }
                catch { txtXML.Text = txtXML.Text; }

                tBoxERecetePath.Visible = false;
                btnSelectFile.Visible = false;
                label1.Visible = false;
            }
            else if (Program.ParamPath != "")
            {
                rbFile.Checked = true;
                try
                {
                    if (File.Exists(tBoxERecetePath.Text))
                    {
                        txtXML.Text = System.Xml.Linq.XDocument.Parse(DosyaRead(Program.ParamPath)).ToString(); // DosyaRead(Program.ParamPath);
                    }
                    else
                    {
                        txtXML.Text = Program.ParamPath + " İmza atılacak dosya bulunamadı, dosyayı seçiniz.";
                    }
                }
                catch { txtXML.Text = Program.ParamPath + " İmza atılacak dosya bulunamadı, dosyayı seçiniz."; }
                tBoxERecetePath.Visible = true;
                btnSelectFile.Visible = true;
                label1.Visible = true;
            }
            else if (tBoxERecetePath.Text != "")
            {
                rbFile.Checked = true;
                try
                {
                    if (File.Exists(tBoxERecetePath.Text))
                    {
                        txtXML.Text = System.Xml.Linq.XDocument.Parse(DosyaRead(tBoxERecetePath.Text)).ToString(); // DosyaRead(tBoxERecetePath.Text);
                    }
                    else
                    {
                        txtXML.Text = tBoxERecetePath.Text + " İmza atılacak dosya bulunamadı, dosyayı seçiniz.";
                    }
                }
                catch
                {
                    txtXML.Text = tBoxERecetePath.Text + " İmza atılacak dosya bulunamadı, dosyayı seçiniz.";
                }

                tBoxERecetePath.Visible = true;
                btnSelectFile.Visible = true;
                label1.Visible = true;
            }
            tmrGiris.Enabled = true;
            //LisansHelper.loadFreeLicense();
            //KartveOkuyucuKontrol();
            if (Program.ParamPin != "")
            {
                if ((Program.ParamPin.Length > 3) && (Program.ParamPin.Length < 10))
                {
                    if ((Program.ParamPin.Length != 4) && (Program.ParamCardType == "SAFESIGN"))
                    {
                        MesajiIsle("SAFESIGN için Pin kodu sorunlu: " + Program.ParamPin, 0);
                    }
                    else
                    {
                        if (Program.ParamPin.All(char.IsDigit))
                        {
                            tbPinKodu.Text = Program.ParamPin;
                            tBoxERecetePath.Visible = false;
                            btnSelectFile.Visible = false;
                            label1.Visible = false;
                        }
                        else
                        {
                            MesajiIsle("Pin kodu sorunlu: " + Program.ParamPin, 0);
                        }
                    }
                }
                else
                {
                    MesajiIsle("Pin kodu fazla uzun veya fazla kısa: " + Program.ParamPin, 0);
                }
            }
            else
            {
                if (Program.ParamOto == "1")
                {
                    MesajiIsle("Pin kodu boş: " + Program.ParamPin, 0);
                }
            }

        }
        public static DateTime GetNetworkTime()
        {
            try
            {
                //default Windows time server
                const string ntpServer = "time.windows.com";

                // NTP message size - 16 bytes of the digest (RFC 2030)
                var ntpData = new byte[48];

                //Setting the Leap Indicator, Version Number and Mode values
                ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

                var addresses = Dns.GetHostEntry(ntpServer).AddressList;

                //The UDP port number assigned to NTP is 123
                var ipEndPoint = new IPEndPoint(addresses[0], 123);
                //NTP uses UDP
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                socket.Connect(ipEndPoint);

                socket.SendTimeout = 3000;
                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();

                //Offset to get to the "Transmit Timestamp" field (time at which the reply 
                //departed the server for the client, in 64-bit timestamp format."
                const byte serverReplyTime = 40;

                //Get the seconds part
                ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

                //Get the seconds fraction
                ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

                //Convert From big-endian to little-endian
                intPart = SwapEndianness(intPart);
                fractPart = SwapEndianness(fractPart);

                var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

                //**UTC** time
                var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

                return networkDateTime.ToLocalTime();
            }

            catch (Exception e)
            {
                MesajiIsle("Gerçek Tarih Saat alınamadı: " + e.Message, 1);
                return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }

        }
        static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }
        public static void MesajiIsle(string Mesaj, byte Fatal)
        {
            if (Program.ParamOto == "0")
            {
                if (Fatal == 1) { Program.Hata = 1; }
                if (Mesaj != "") { MessageBox.Show(Mesaj); }
            }
            else
            {
                if (Mesaj != "") { System.Console.WriteLine(Mesaj); }
                if (Fatal == 1)
                {
                    Program.Hata = 1;
                    Environment.Exit(1);
                }
            }
        }


        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x0219:


                    try
                    {
                        backgroundWorker1.RunWorkerAsync();
                    }
                    catch (Exception)
                    {

                        
                    }


                    break;
            }
            base.WndProc(ref m);

        }

        public bool KartveOkuyucuKontrol()
        {
            String[] terminals = SmartOp.getCardTerminals();
            Program.TerminalSayisi = terminals.Length;

            if (terminals == null || terminals.Length == 0)
            {
                return false;
            }
            SmartCardManager scmgr = SmartCardManager.getInstance();
            {
                ECertificate signingCert = scmgr.getSignatureCertificate(true, false);
                LBLTCKIMLIK.Text= Bul(signingCert.ToString(),"SERIALNUMBER=",",");
                LBLADISOYADI.Text= Bul(signingCert.ToString(), "CN=", ",");
                LBLSERIAL.Text = Bul(signingCert.ToString(), "Serial:", "Valid from").TrimStart(' ');
                LBLBASTARIH.Text= Bul(signingCert.ToString(), "Valid from", "to").TrimStart(' ').TrimEnd(' ');
                LBLBITTARIH.Text = Bul(signingCert.ToString(), " to", "Ca:").TrimStart(' ').TrimEnd(' ');
                
                DateTime bitis_tarihi = DateTime.Parse(LBLBITTARIH.Text);
                DateTime baslangic_tarihi = DateTime.Parse(LBLBASTARIH.Text);
                DateTime simdi = DateTime.Now;
                int kalangun = GunFarkikBul(simdi,bitis_tarihi);

                if (bitis_tarihi<simdi)
                {
                    LBLKALANGUN.Text ="- "+kalangun.ToString();
                    LBLKALANGUN.ForeColor = Color.Red;
                }
                else
                {
                    LBLKALANGUN.Text = kalangun.ToString();
                }

                Program.SertifikaBilgisi = "Sertifika ve Sahiplik Bilgisi: " + signingCert.ToString();
            }

            return true;
        }


        #region gün farkı bul
        public int GunFarkikBul(DateTime dt1, DateTime dt2)

        {

            TimeSpan zaman = new TimeSpan(); // zaman farkını bulmak adına kullanılacak olan nesne

            zaman = dt1 - dt2;//metoda gelen 2 tarih arasındaki fark

            return Math.Abs(zaman.Days); // 2 tarih arasındaki farkın kaç gün olduğu döndürülüyor.

        }
        #endregion



        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            LBLTCKIMLIK.Text = null;
            LBLADISOYADI.Text = null;
            LBLSERIAL.Text = null;
            LBLBASTARIH.Text = null;
            LBLBITTARIH.Text = null;
            LBLKALANGUN.Text = null;
            KartveOkuyucuKontrol();

        }


        #region İKİ STRİNG ARASINI BUL
        public string Bul(string metin, string basla, string bitir)
        {
            string sonuc;
            try
            {
                int IcerikBaslangicIndex = metin.IndexOf(basla) + basla.Length;
                int IcerikBitisIndex = metin.Substring(IcerikBaslangicIndex).IndexOf(bitir);
                sonuc = metin.Substring(IcerikBaslangicIndex, IcerikBitisIndex);
            }
            catch (Exception)
            {
                sonuc = null;
            }

            return sonuc;
        }
        #endregion

        public string DosyaRead(string Path)
        {

            if (Path == "") return "";
            try
            {
                StreamReader streamRead = new StreamReader(Path);
                string text = streamRead.ReadToEnd();
                streamRead.Close();
                return text;
            }
            catch
            {
                MesajiIsle("Dosya Okunamadı: " + Path, 0);
                return "";
            }
        }
        // dll embed icin
        //// dll handler
        //System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        //{
        //    string keyName = new AssemblyName(args.Name).Name;

        //    // If DLL is loaded then don't load it again just return
        //    if (_libs.ContainsKey(keyName)) return _libs[keyName];

        //    // using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DllResourceName("itextsharp.dll")))  // <-- To find out the Namespace name go to Your Project >> Properties >> Application >> Default namespace
        //    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EsyaXMLSignatureExample." + keyName + ".dll"))  // <-- To find out the Namespace name go to Your Project >> Properties >> Application >> Default namespace
        //    {
        //        byte[] buffer = new BinaryReader(stream).ReadBytes((int)stream.Length);
        //        Assembly assembly = Assembly.Load(buffer);
        //        _libs[keyName] = assembly;
        //        return assembly;
        //    }
        //}

        // dll embed icin
        //private string DllResourceName(string ddlName)
        //{
        //    string resourceName = string.Empty;
        //    foreach (string name in Assembly.GetExecutingAssembly().GetManifestResourceNames())
        //    {
        //        if (name.EndsWith(ddlName))
        //        {
        //            resourceName = name;
        //            break;
        //        }
        //    }
        //    return resourceName;
        //}

        private void btnEReceteImzala_Click(object sender, EventArgs e)
        {
            SignHelper signHelper = new SignHelper();
            // string signedFilePath = signHelper.eReceteImzala(tBoxERecetePath.Text, tBoxSignedERecetePath.Text);
            // dosyayiBase64Yaz(signedFilePath);
            if (Program.ParamXML == "" && Program.ParamPath == "" && tBoxERecetePath.Text == "")
            {
                MesajiIsle("EReçete verisi string olarak da path olarak da gelmedi, kaynak dosyayı seçiniz!", 0);
            }
            if (rbString.Checked && Program.ParamXML == "")
            {
                MesajiIsle("EReçete verisi string olarak gelmedi, kaynak dosya üzerinden işlem yapınız!", 0);
                rbFile.Checked = true;
                if (Program.ParamOto == "0") { return; }
                //return;
            }
            if (Program.Hata == 1) { Environment.Exit(1); }
            if (tbPinKodu.Text == "")
            {
                MesajiIsle("Pin kodunu giriniz!", 0);
                if (Program.ParamOto == "0") { return; }
                if (Program.ParamOto == "1") { Environment.Exit(1); }
                //return;
            }
            if (Program.Hata == 1) { Environment.Exit(1); }
            // dosya yapisi kontrolu
            if (Program.ParamOto != "1")
            {
                if (txtXML.Text.Contains("<ereceteBilgisi>") && txtXML.Text.Contains("</ereceteBilgisi>") && txtXML.Text.Contains("<takipNo>") && txtXML.Text.Contains("<doktorTcKimlikNo>"))
                {
                    //    if (MessageBox.Show("E-Reçete bilgisini 'Güvenli Elektronik İmza' ile imzalamak istediğinizden emin misiniz?", "Dikkat", MessageBoxButtons.OKCancel) == DialogResult.OK)
                    //    { }
                    //    else MesajiIsle("", 1);
                }
                else
                {
                    if (txtXML.Text.Contains("<") && txtXML.Text.Contains("</") && txtXML.Text.Contains(">"))
                    {
                        //if (MessageBox.Show("İmzalanacak bilgi, bir XML ve e-Reçete değil. 'Güvenli Elektronik İmza' ile imzalamak istediğinizden emin misiniz?", "Dikkat", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        //{ }
                        //else MesajiIsle("", 1);
                    }
                    else
                    {
                        if (MessageBox.Show("İmzalanacak XML, geçerli bir e-Reçete değil. Yine de 'Güvenli Elektronik İmza' ile imzalamak istediğinizden emin misiniz?", "Dikkat", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        { }
                        else MesajiIsle("", 1);
                    }

                }
            }
            if (Program.Hata == 1) { Environment.Exit(1); }
            Program.PinKodu = tbPinKodu.Text;
            string XML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + signHelper.eReceteImzala(tBoxERecetePath.Text, tBoxSignedERecetePath.Text, Program.ParamXML, cbDosyayaYaz.Checked);
            if (Program.Hata == 1) { Environment.Exit(1); }
            // dosyaya yazilacaksa eReceteImzala icinde imzalanmis xsig dosyasi olusuyor 
            // sonra bu dosya alttaki prosedurde okunup base64'e cevriliyor ve sonu _64.txt olacak sekilde kaydediliyor.
            if (cbDosyayaYaz.Checked) dosyayiBase64Yaz(tBoxSignedERecetePath.Text.Replace(".xml", ".xsig"));
            // ONEMLI, base64 olarak console ciktisi veriyorum. ikazanci
            byte[] signStream = System.Text.Encoding.UTF8.GetBytes(XML);
            // hata varsa parametreyi geri dondurmeden evvel cik
            if (Program.Hata == 1) { Environment.Exit(1); }
            System.Console.WriteLine("esignerbase64:" + System.Convert.ToBase64String(signStream) + ":esignerbase64");
            if (XML != "") Application.Exit();
            //TextWriter tw = new StreamWriter(tBoxSignedERecetePath.Text.Replace(".xml", ".xsig")+"-String_64.txt");
            //// write a line of text to the file
            //tw.WriteLine(System.Convert.ToBase64String(signStream));
            //// close the stream
            //tw.Close();

            //Akıllı kartla doğrudan imzala
            // string signedFilePath = signHelper.eReceteImzala_SmartCardSigner(tBoxERecetePath.Text);

            // path donusunu kaldirip mesaji ereceteImzala'mnin icine aldim... ikazanci 20.6.2014
            //if (signedFilePath != null)
            //{
            //    tBoxSignedERecetePath.Text = signedFilePath;
            //    MessageBox.Show(signedFilePath + " konumunda imzalı E-Reçete oluşturuldu.");
            //}


            // string eReceteImzalaBase64 = signHelper.eReceteImzalaBase64(tBoxERecetePath.Text);
            //MessageBox.Show(eReceteImzalaBase64);
        }

        private void Main_Load(object sender, EventArgs e)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            //tBoxERecetePath.Text = currentDirectory + "\\eRecete.xml";
            //tBoxSignedERecetePath.Text = currentDirectory + "\\eReceteBES.xml";
            if (Program.ParamPath != "")
            { tBoxERecetePath.Text = Program.ParamPath; }
            else tBoxERecetePath.Text = "eRecete.xml";
            //tBoxERecetePath.Text = Program.ParamPath +"eRecete.xml";
            tBoxSignedERecetePath.Text = currentDirectory + "\\eReceteImzali.xml";
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            FileDialog fd = new OpenFileDialog();
            fd.InitialDirectory = Directory.GetCurrentDirectory();
            DialogResult result = fd.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                tBoxERecetePath.Text = fd.FileName;
                txtXML.Text = DosyaRead(tBoxERecetePath.Text);
            }
        }

        private void btnVerify_Click(object sender, EventArgs e)
        {
            SignHelper signHelper = new SignHelper();
            string signedERecetePath = tBoxSignedERecetePath.Text;
            string verifyResultStr = signHelper.verifySignature(signedERecetePath);
            MesajiIsle(verifyResultStr, 0);
        }

        private void btnAddTimeStamp_Click(object sender, EventArgs e)
        {
            SignHelper signHelper = new SignHelper();
            string signedERecetePath = tBoxSignedERecetePath.Text;
            string estFilePath = signHelper.upgradeToEST(signedERecetePath);
            if (estFilePath != null)
            {
                tBoxSignedERecetePath.Text = estFilePath;
                MesajiIsle(estFilePath + " konumunda zaman damgalı imzalı E-Reçete oluşturuldu.", 0);
            }
        }

        private void btnAddSerialSignature_Click(object sender, EventArgs e)
        {
            SignHelper signHelper = new SignHelper();
            string signedERecetePath = tBoxSignedERecetePath.Text;
            string estFilePath = signHelper.addSerialSignature(signedERecetePath);
            if (estFilePath != null)
            {
                tBoxSignedERecetePath.Text = estFilePath;
                MesajiIsle(estFilePath + " konumunda seri imzalı E-Reçete oluşturuldu.", 0);
            }
        }

        private void btnAddParalelSignature_Click(object sender, EventArgs e)
        {
            SignHelper signHelper = new SignHelper();
            string eRecetePath = tBoxERecetePath.Text;
            string estFilePath = signHelper.createParalelSignature(eRecetePath);
            if (estFilePath != null)
            {
                tBoxSignedERecetePath.Text = estFilePath;
                MesajiIsle(estFilePath + " konumunda paralel imzalı E-Reçete oluşturuldu.", 0);
            }
        }

        void dosyayiBase64Yaz(string filePath)
        {
            string base64 = dosyadanBase64Oku(filePath);
            // create a writer and open the file
            TextWriter tw = new StreamWriter(filePath + "_64.txt");

            // write a line of text to the file
            tw.WriteLine(base64);

            // close the stream
            tw.Close();
            // ONEMLI, base64 olarak output veriyorum. ikazanci
            //System.Console.WriteLine(base64);
        }

        private string dosyadanBase64Oku(string filePath)
        {
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            byte[] signStream = AsnIO.streamOku(fileStream);
            string base64Signature = System.Convert.ToBase64String(signStream);
            return base64Signature;
        }

        // http://stackoverflow.com/questions/472906/converting-a-string-to-byte-array adresindeki kod
        static byte[] GetBytesx(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void rbString_CheckedChanged(object sender, EventArgs e)
        {
            if (rbString.Checked)
            {
                tBoxERecetePath.Visible = false;
                btnSelectFile.Visible = false;
                label1.Visible = false;
                txtXML.Text = Program.ParamXML;
            }
            else
            {
                tBoxERecetePath.Visible = true;
                btnSelectFile.Visible = true;
                label1.Visible = true;
                if (tBoxERecetePath.Text != "") { txtXML.Text = DosyaRead(tBoxERecetePath.Text); }
            }

        }

        private void rbFile_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Program.ParamOto == "0")
            {
                // manuel ise belli sure sonra pinkodunu sifirlayabilirsin
                tbPinKodu.Text = "";
                tmrPinKoduTemizle.Enabled = false;
            }
        }

        private void tbPinKodu_TextChanged(object sender, EventArgs e)
        {
            tmrPinKoduTemizle.Enabled = true;
        }

        private void tBoxERecetePath_TextChanged(object sender, EventArgs e)
        {
            //  txtXML.Text = DosyaRead(tBoxERecetePath.Text);
        }

        private void tbPinKodu_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnEReceteImzala_Click(this, new EventArgs());
            }
        }

        private void tmrOtoImzala_Tick(object sender, EventArgs e)
        {
        }

        private void tmrGiris_Tick(object sender, EventArgs e)
        {
            //if (Program.himm == 1)
            //{
            //    Application.Exit();
            //}
            tmrGiris.Enabled = false;
            //            LisansHelper.loadFreeLicense();
            LisansHelper.loadLicense();
            if (Program.ParamSlotID == "") if (KartveOkuyucuKontrol() == false) ;
            if (Program.ParamOto == "1") { btnEReceteImzala_Click(btnEReceteImzala, new EventArgs()); };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String[] terminals = SmartOp.getCardTerminals();
            if (terminals == null || terminals.Length == 0)
            {
                MesajiIsle("Kart takılı bir kart okuyucu bulunamadı-Kart Kontrol İşlemi", 1);
                return;
            }
            // eger tum kartlari kendisi kontrol edip oto. imzalayacaksa askoption calistirma,
            // for dongusunde tum kartlari ve kartlardaki tum sertifikalari gezip bunlari arraya, sonrasinda da tabloya at
            // imzalama asamasinda tckimlikno uyusmamasi gibi bir durum olursa ancak o zaman tum kartlari bir kez daha
            // dolasarak db tablodaki kart ve sertifikalarin slot nolarini yenile

            // ***********************
            // alt satirda hem kartlar hem sertifikalar hem ltrm class dolduruluyor 
            SmartCardManagerTumunuOku scmgr = SmartCardManagerTumunuOku.getInstanceTumunuOku();
            // ***********************
            {
                // sql olustur 
                String SqlCumlesi = "";
                SqlCumlesi = "Delete AkilliKartlar ";

                for (int i = 0; i < Program.ltrm.Count; i++)
                {
                    SqlCumlesi += " insert into AkilliKartlar (FisNo, TerminalAdi,TCKimlikNo,AdiSoyadi,InsertDate, KartTipi, SlotID) values ('" +
                       Convert.ToString(i + 1) + "','" + Program.ltrm[i].TerminalAdi + "','" + Program.ltrm[i].TCKimlikNo + "','" + Program.ltrm[i].AdiSoyadi + "', getdate(), '" + Program.ltrm[i].KartTipi + "', '" + Convert.ToInt64(Program.ltrm[i].SlotID) + "' )";
                }
                if (Program.ltrm.Count > 0)
                {
                    if (Program.ParamSQLServer == "")
                    {
                        MesajiIsle("SQL Server bağlantı bilgileri eksik. Kayıt işlemi yapılamadı.", 0);
                        //return;
                    }
                    if (Program.ParamSQLUser == "")
                    {
                        MesajiIsle("SQL Server bağlantı bilgileri eksik. Kayıt işlemi yapılamadı.", 0);
                        return;
                    }
                    if (Program.ParamSQLPassword == "")
                    {
                        MesajiIsle("SQL Server bağlantı bilgileri eksik. Kayıt işlemi yapılamadı.", 0);
                        return;
                    }
                    // dbye kaydet
                    SqlConnection SQLFormVeriBaglantisi = new SqlConnection();
                    SQLFormVeriBaglantisi.ConnectionString = "server=" + Program.ParamSQLServer + ";user=" + Program.ParamSQLUser + ";pwd=" + Program.ParamSQLPassword + ";database=konur;";
                    SQLFormVeriBaglantisi.Open();
                    SqlCommand qryVeriKaydet = new SqlCommand(SqlCumlesi, SQLFormVeriBaglantisi);
                    qryVeriKaydet.ExecuteNonQuery();
                    SQLFormVeriBaglantisi.Close();
                    MessageBox.Show("Kayıt işlemi Tamamlandı", "Kayıt Bilgisi");
                }
            }

            return;
        }

        private void cbDosyayaYaz_CheckedChanged(object sender, EventArgs e)
        {
            if (cbDosyayaYaz.Checked)
            {
                tBoxSignedERecetePath.Visible = true;
                lblHedef.Visible = true;
            }
            else
            {
                tBoxSignedERecetePath.Visible = false;
                lblHedef.Visible = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            String[] terminals = SmartOp.getCardTerminals();
            if (terminals == null || terminals.Length == 0)
            {
                MesajiIsle("Kart takılı bir kart okuyucu bulunamadı-Kart Kontrol İşlemi", 1);
                return;
            }
            // eger tum kartlari kendisi kontrol edip oto. imzalayacaksa askoption calistirma,
            // for dongusunde tum kartlari ve kartlardaki tum sertifikalari gezip bunlari arraya, sonrasinda da tabloya at
            // imzalama asamasinda tckimlikno uyusmamasi gibi bir durum olursa ancak o zaman tum kartlari bir kez daha
            // dolasarak db tablodaki kart ve sertifikalarin slot nolarini yenile

            // ***********************
            // alt satirda hem kartlar hem sertifikalar hem ltrm classi dolduruluyor 
            SmartCardManagerTumunuOku scmgr = SmartCardManagerTumunuOku.getInstanceTumunuOku();
            // ***********************
            {
                // olustur 
                string Mesaj = "";
                for (int i = 0; i < Program.ltrm.Count; i++)
                {
                    Mesaj += "\r\nKart No: " + Convert.ToString(i + 1) + ", TerminalAdı: " + Program.ltrm[i].TerminalAdi + ", TCKimlikNo: " + Program.ltrm[i].TCKimlikNo + ", AdSoyad: " + Program.ltrm[i].AdiSoyadi + ", KartTipi: " + Program.ltrm[i].KartTipi + ", SlotID: " + Program.ltrm[i].SlotID;
                }
                if (Program.ltrm.Count > 0)
                {

                    MessageBox.Show("Kartlar okundu: " + Mesaj, "Kayıt Bilgisi");
                }
            }

            return;


        }

        private void button3_Click_1(object sender, EventArgs e)
        {
        }

        private void btnKapat_Click(object sender, EventArgs e)
        {
        }


    }

    class terminaller
    {
        public string TerminalAdi { set; get; }
        public string AdiSoyadi { set; get; }
        public string TCKimlikNo { set; get; }
        public string KartTipi { set; get; }
        public string SlotID { set; get; }
    }
}
