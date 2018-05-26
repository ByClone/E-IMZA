using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using SignatureHelper;
using tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11;

namespace ProgramNameSpace
{
    static class Program
    {
        public static string ParamXML = "";
        public static string ParamPath = "";
        public static string ParamOto = "0";
        public static string ParamPin = "";
        public static string ParamTCKimlikNo = "";
        public static string ParamSlotID = "";
        public static string ParamCardType = "";

        public static string ParamAdiSoyadi;
        public static string ParamSQLServer = "192.168.0.250"; // "127.0.0.1";
                          // ParamSQLServer = "192.168.0.250";

        public static string ParamSQLUser;
        public static string ParamSQLPassword;
        public static string ParamOtoExit = "0";

        public static byte KartOkuyucuYok = 0;
        public static int TerminalSayisi = 0;
        public static string PinKodu;
        public static string HataMesaji = "";
        public static int Hata = 0;

        public static int himm = 1;

        public static string SertifikaBilgisi = "";

        public static string CardType = "";
        public static string Terminal = "";

        public static List<terminaller> ltrm = new List<terminaller>();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {   // dll dosyalari 

            if (args.Length == 4)
            {
                if (args.Length != 0) { ParamXML = args[0]; }
                if (args.Length > 1) { ParamPath = args[1]; }
                if (args.Length > 2) { ParamCardType = args[2]; }
                if (args.Length > 3) { ParamPin = args[3]; }
            }
            else
            {
                if (args.Length != 0) { ParamXML = args[0]; }
                if (args.Length > 1) { ParamXML += args[1]; }
                if (args.Length > 2) { ParamXML += args[2]; }
                if (args.Length > 3) { ParamXML += args[3]; }
                if (args.Length > 4) { ParamXML += args[4]; }
                if (args.Length > 5) { ParamXML += args[5]; }
                if (args.Length > 6) { ParamXML += args[6]; }
                if (args.Length > 7) { ParamXML += args[7]; }
                if (args.Length > 8) { ParamXML += args[8]; }
                if (args.Length > 9) { ParamXML += args[9]; }
                if (args.Length > 10) { ParamCardType = args[10]; }
                if (args.Length > 11) { ParamPath = args[11]; }
                if (args.Length > 12) { ParamOto = args[12]; }
                if (args.Length > 13) { ParamPin = args[13]; }
                if (args.Length > 14) { ParamTCKimlikNo = args[14]; }
                if (args.Length > 15) { ParamSlotID = args[15]; }
                if (args.Length > 16) { ParamAdiSoyadi = args[16]; }
                if (args.Length > 17) { ParamSQLServer = args[17]; }
                if (args.Length > 18) { ParamSQLUser = args[18]; }
                if (args.Length > 19) { ParamSQLPassword = args[19]; }
            }

            ParamXML = ParamXML.Replace("^", ""); // varsa ^ karakterini temizle
            ParamPath = ParamPath.Replace("\"", ""); // varsa " karakterini temizle 

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
    public static class WinFormsExtensions
    {
        public static void AppendLine(this TextBox source, string value)
        {
            if (source.Text.Length == 0)
                source.Text = value;
            else
                source.AppendText("\r\n" + value);
        }
    }

    public static class CardTypeConverter
    {
        public static CardType AsCardType(string cardtype)
        {
              switch(cardtype)
              {
                    case "AEPKEYPER": return CardType.AEPKEYPER;
                    case "AKIS": return CardType.AKIS;
                    case "AKIS_KK": return CardType.AKIS_KK;
                    case "ALADDIN": return CardType.ALADDIN;
                    case "CARDOS": return CardType.CARDOS;
                    case "DATAKEY": return CardType.DATAKEY;
                    case "GEMPLUS": return CardType.GEMPLUS;
                    case "KEYCORP": return CardType.KEYCORP;
                    case "NCIPHER": return CardType.NCIPHER;
                    case "SAFESIGN": return CardType.SAFESIGN;
                    case "SEFIROT": return CardType.SEFIROT;
                    case "TKART": return CardType.TKART;
                    case "UNKNOWN": return CardType.UNKNOWN;
                    case "UTIMACO": return CardType.UTIMACO;
                  default: return CardType.UNKNOWN;
              }
        }
    }

}
