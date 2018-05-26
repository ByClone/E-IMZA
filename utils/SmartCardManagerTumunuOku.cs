using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Threading;
using System.Timers;
using iaik.pkcs.pkcs11.wrapper;
using log4net;
using ProgramNameSpace;
using SignatureHelper;
using tr.gov.tubitak.uekae.esya.api.asn.x509;
using tr.gov.tubitak.uekae.esya.api.common;
using tr.gov.tubitak.uekae.esya.api.common.crypto;
using tr.gov.tubitak.uekae.esya.api.common.util;
using tr.gov.tubitak.uekae.esya.api.common.util.bag;
using tr.gov.tubitak.uekae.esya.api.smartcard.gui;
using tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11;

/**
 * SmartCardManagerToplu handles user smart card operations.
 *
 */

namespace utils
{
    public class SmartCardManagerTumunuOku
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //private static Object lockObject = new Object();

        private static SmartCardManagerTumunuOku mSCManager;

        public int mSlotCount = 0;

        private int aktifkart = 0;
        private int SessionOpened = 0;

        public readonly List<String> mSerialNumber = new List<string>();

        public List<String> Terminaller = new List<string>();

        public List<ECertificate> mSignatureCert;

        public List<ECertificate> mEncryptionCert;

        public List<IBaseSmartCard> bsc;

        //protected IBaseSmartCard[] bsc;

        protected BaseSigner[] mSigner;

        /**
         * Singleton is used for this class. If many card placed, it wants to user to select one of cards.
         * If there is a influential change in the smart card environment, it  repeat the selection process.
         * The influential change can be: 
         * 		If there is a new smart card connected to system.
         * 		The cached card is removed from system.
         * These situations are checked in getInstance() function. So for your non-squential SmartCard Operation,
         * call getInstance() function to check any change in the system.
         *
         * In order to reset thse selections, call reset function.
         * 
         * @return SmartCardManagerToplu instance
         * @throws SmartCardException
         */
        public void MesajiIsle(string Mesaj, byte Fatal)
        {
            if (Program.ParamOto == "0")
            {
                if (Fatal == 1) { Program.Hata = 1; }
                MessageBox.Show(Mesaj);
            }
            else
            {
                System.Console.WriteLine(Mesaj);
                if (Fatal == 1)
                {
                    Program.Hata = 1;
                    Environment.Exit(1);
                }
            }
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SmartCardManagerTumunuOku getInstanceTumunuOku()
        {
            if (mSCManager == null)
            {
                mSCManager = new SmartCardManagerTumunuOku();
                return mSCManager;
            }
            else
            {
                //Check is there any change
                try
                {
                    //If there is a new card in the system, user will select a smartcard. 
                    //Create new SmartCard.
                    if (mSCManager.getSlotCount() < SmartOp.getCardTerminals().Length)
                    {
                        LOGGER.Debug("New card pluged in to system");
                        mSCManager = null;
                        return getInstanceTumunuOku();
                    }

                    //If used card is removed, select new card.
                    String availableSerial = null;
                    try
                    {
                        availableSerial = StringUtil.ToString(mSCManager.getBasicSmartCard().getSerial());
                    }
                    catch (SmartCardException ex)
                    {
                        LOGGER.Debug("Card removed");
                        mSCManager = null;
                        return getInstanceTumunuOku();
                    }
                    if (!mSCManager.getSelectedSerialNumber().Equals(availableSerial))
                    {
                        LOGGER.Debug("Serial number changed. New card is placed to system");
                        mSCManager = null;
                        return getInstanceTumunuOku();
                    }

                    return mSCManager;
                }
                catch (SmartCardException e)
                {
                    mSCManager = null;
                    throw;
                }
            }
        }


        /*
         * 
         * @throws SmartCardException
         */

        /**
         * BaseSigner interface for the requested certificate. Do not forget to logout after your crypto 
         * operation finished
         * @param aCardPIN
         * @param aCert
         * @return
         * @throws SmartCardException
         */

              //  public delegate void tm();
        public SmartCardManagerTumunuOku()
        {
            try
            {
                LOGGER.Debug("New SmartCardManagerToplu will be created");
                String[] terminals = SmartOp.getCardTerminals();

                if (terminals == null || terminals.Length == 0)
                {
                    MesajiIsle("Kart takılı kart okuyucu bulunamadı-SmartCardManagerTumunuOku", 1);
                    Program.KartOkuyucuYok = 1;
                    return;
                    // throw new SmartCardException("Kart takılı kart okuyucu bulunamadı");
                }

                mSlotCount = terminals.Length;
                int index = 0;
                bsc = new List<IBaseSmartCard>();
                Program.ltrm.Clear();
                long[] PresentSlots;
                int m;
                //string SlotID;
                SmartCard sc;
                // kartlari turlerine gore denetleyip getSignatureCertificateTek tek bsc listesine ekleyip
                // TC, AdSoyad, SlotID ve CardType bilgilerini topla. terminal iptal...

                // yeni sekil tum kartlari bu sekilde okumayi dene
                //if (Program.ParamCardType != "")
                {   // meltemde o oldugu icin sadece safesign gonderip onu toplamak yerine kart tiplerinin tümünü ardarda da deneyebiliriz...
                    // bu zaten tum kart tipşerini ve slotlarini ciftler halinde getiriyor. guzel... 26.12.2015 ikazanci
                    //List <Pair<long, CardType>> slotAndCardType = SmartOp.findCardTypesAndSlots(CardTypeConverter.AsCardType(Program.ParamCardType).getApplication());
                    //for (m = 0; m < slotAndCardType.Count; m++)
                    //{
                    //    SlotID = slotAndCardType[m].getmObj1().ToString();
                    //    bsc.Add(new P11SmartCard(slotAndCardType[m].getmObj2()));
                    //    bsc[index].openSession(slotAndCardType[m].getmObj1()); // etugra icin 52481
                    //    mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial(Convert.ToInt64(SlotID))));
                    //    // sertifika getir
                    //    getSignatureCertificateTek(true, false, index);
                    //    terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", slotAndCardType[m].getmObj2().ToString(), slotAndCardType[m].getmObj1().ToString());
                    //    bsc[index].closeSession();
                    //    index = index + 1;
                    //}
                    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.SAFESIGN);
                    PresentSlots = sc.getTokenPresentSlotList(); // getTokenPresentSlotList(); // tokenli slot listesini al
                    for (m = 0; m < PresentSlots.Length; m++)
                    {   
                        bsc.Add(new P11SmartCard(sc.getCardType()));
                        bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                        mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                        // sertifika getir
                        getSignatureCertificateTek(true, false, index);
                        terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                        index = index + 1;
                    }
                }

                // ...
                // ...
                // yeni sekil sonu

                if (1 == 0)
                {
                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.ALADDIN);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //        mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //        // sertifika getir
                    //        getSignatureCertificateTek(true, false, index);
                    //        terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //        index = index + 1;
                    //    }
                    //}
                    //catch
                    //{ }
                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.AKIS);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        //MessageBox.Show(sc.getCardType().ToString() + " " + PresentSlots[m].ToString());
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            // bsc[index].openSession(PresentSlots[m]);
                    //            var t = new tm(() => KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 500;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            DelayAction(1000, timer.Stop);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }

                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.SAFESIGN);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        //MessageBox.Show(sc.getCardType().ToString() + " " + PresentSlots[m].ToString());
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            // bsc[index].openSession(PresentSlots[m]);
                    //            var t = new tm(() => KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 500;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            DelayAction(1000, timer.Stop);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }
                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.GEMPLUS);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            var t = new tm(() => SessionOpened = KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 5000;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }
                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.TKART);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            var t = new tm(() => SessionOpened = KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 5000;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }
                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.SEFIROT);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            var t = new tm(() => SessionOpened = KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 5000;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }
                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.AEPKEYPER);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            var t = new tm(() => SessionOpened = KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 5000;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }

                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.AKIS_KK);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            var t = new tm(() => SessionOpened = KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 5000;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }

                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.CARDOS);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            var t = new tm(() => SessionOpened = KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 5000;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }
                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.DATAKEY);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            var t = new tm(() => SessionOpened = KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 5000;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }
                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.KEYCORP);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            var t = new tm(() => SessionOpened = KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 5000;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }

                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.NCIPHER);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            var t = new tm(() => SessionOpened = KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 5000;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }

                    //try
                    //{
                    //    sc = new SmartCard(tr.gov.tubitak.uekae.esya.api.smartcard.pkcs11.CardType.UTIMACO);
                    //    PresentSlots = sc.getTokenPresentSlotList(); // tokenli slot listesini al
                    //    for (m = 0; m < PresentSlots.Length; m++)
                    //    {
                    //        bsc.Add(new P11SmartCard(sc.getCardType()));
                    //        try
                    //        {
                    //            SessionOpened = 0;
                    //            var t = new tm(() => SessionOpened = KartAc(index, PresentSlots[m]));
                    //            var timer = new System.Timers.Timer();
                    //            timer.Interval = 5000;
                    //            timer.AutoReset = false;
                    //            timer.Start();
                    //            timer.Elapsed += (sender, e) => timer_Elapsed(t);
                    //            t.BeginInvoke(null, null);
                    //            // bsc[index].openSession(PresentSlots[m]); // etugra icin 52481
                    //            if (SessionOpened == 1)
                    //            {
                    //                mSerialNumber.Add(StringUtil.ToString(bsc[index].getSerial()));
                    //                // sertifika getirme islemi
                    //                getSignatureCertificateTek(true, false, index);
                    //                terminalekle(GetTcNo(mSignatureCert[index].ToString()), GetAdSoyad(mSignatureCert[index].ToString()), "", sc.getCardType().ToString(), PresentSlots[m].ToString());
                    //                index = index + 1;
                    //            }
                    //            else
                    //            {
                    //                bsc.RemoveAt(index);
                    //            }
                    //        }
                    //        catch { bsc.RemoveAt(index); }
                    //    }
                    //}
                    //catch
                    //{ }
                } 
                // iptal edilen kisim sonu
            }

       catch (SmartCardException e)
            {
                MesajiIsle("Takılı bütün kartların listelemesi yapılamadı, muhtemelen sistemde farklı türde bir kart takılı: "+ e.Message, 1); 
                // throw e;
            }
            catch (PKCS11Exception e)
            {
                throw new SmartCardException("Pkcs11 exception", e);
            }
            catch (IOException e)
            {
                throw new SmartCardException("Smart Card IO exception - Detay bilgilerine bakınız", e);
            }
        }

        private void Start()
        {
            throw new NotImplementedException();
        }


        public static void DelayAction(int millisecond, Action action)
        {
            var timer = new DispatcherTimer();
            timer.Tick += delegate
            {
                action.Invoke();
                timer.Stop();
            };

            timer.Interval = TimeSpan.FromMilliseconds(millisecond);
            timer.Start();
        }
        
        //static void timer_Elapsed(tm p)
        //{
        //    try
        //    { if (p != null) p.EndInvoke(null); }
        //    catch { }
        //   // throw new TimeoutException();
        //}

        private int KartAc(int i, long p)
        {
            try
        {
            bsc[i].openSession(p);
            SessionOpened = 1;
            return 1;
        }catch
            {
            return 0;
        }
        }
     
        private void terminalekle(string TCNo, string AdSoyad, string TerminalAdi, string KartTipi, string SlotID)
        {
            Program.ltrm.Add(new terminaller
            {
                TCKimlikNo = TCNo,
                AdiSoyadi = AdSoyad,
                TerminalAdi = TerminalAdi,
                KartTipi = KartTipi,
                SlotID = SlotID
            });
        }

        private string GetTcNo(string Cert)
        {
            // sertifika icindeki tcno bilgisini al
            int startIndex1 = Cert.IndexOf("SERIALNUMBER=");
            return Cert.Substring(startIndex1 + 13, 11);
        }

        private string GetAdSoyad(string Cert)
        {
            // sertifika icindeki ad soyad bilgisini al
            int startIndex2 = Cert.IndexOf("CN=");
            int endIndex = Cert.IndexOf(",", startIndex2);
            return Cert.Substring(startIndex2 + 3, endIndex - (startIndex2 + 3));
        }


        [MethodImpl(MethodImplOptions.Synchronized)]


        public ECertificate getSignatureCertificateTek(bool checkIsQualified, bool checkBeingNonQualified, int index)
        {
            if (mSignatureCert == null) { mSignatureCert = new List<ECertificate>(); }
            List<byte[]> allCerts = bsc[index].getSignatureCertificates();
            mSignatureCert.Add(selectCertificate(checkIsQualified, checkBeingNonQualified, allCerts));
            return mSignatureCert[index];
        }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        public ECertificate getSignatureCertificateToplu(bool checkIsQualified, bool checkBeingNonQualified)
        {
            mSignatureCert = new List<ECertificate>();
            for (int index = 0; index <= mSlotCount - 1; index++)
            {
                //if (mSignatureCert[index] == null)
                //{
                List<byte[]> allCerts = bsc[index].getSignatureCertificates();
                mSignatureCert.Add(selectCertificate(checkIsQualified, checkBeingNonQualified, allCerts));
                //// tcno ve adsoyad alma denemesi
                //string AdiSoyadi, TCKimlikNo = mSignatureCert[aktifkart].ToString();
                //AdiSoyadi = TCKimlikNo;
                //int startIndex = TCKimlikNo.IndexOf("SERIALNUMBER=");
                //TCKimlikNo = TCKimlikNo.Substring(startIndex + 13, 11);

                //startIndex = AdiSoyadi.IndexOf("CN=");
                //int endIndex = AdiSoyadi.IndexOf(",", startIndex);
                //AdiSoyadi = AdiSoyadi.Substring(startIndex + 3, endIndex - (startIndex + 3));
                //// tcno ve adsoyad alma denemesi
                //}
            }
            return mSignatureCert[aktifkart];
        }

        /**
         * Returns for the encryption certificate. If there are more than one certificates in the card in requested
         * attributes, it wants user to select the certificate. It caches the selected certificate, to reset cache,
         * call reset function.
         * 
         * @param checkIsQualified
         * @param checkBeingNonQualified
         * @return
         * @throws SmartCardException
         * @throws ESYAException
         */

        [MethodImpl(MethodImplOptions.Synchronized)]
        public ECertificate getEncryptionCertificate(bool checkIsQualified, bool checkBeingNonQualified)
        {
            if (mEncryptionCert == null)
            {
                List<byte[]> allCerts = bsc[aktifkart].getEncryptionCertificates();
                mEncryptionCert[aktifkart] = selectCertificate(checkIsQualified, checkBeingNonQualified, allCerts);
            }

            return mEncryptionCert[aktifkart];
        }

        private ECertificate selectCertificate(bool checkIsQualified, bool checkBeingNonQualified, List<byte[]> aCerts)
        {
            if (aCerts != null && aCerts.Count == 0)
                throw new ESYAException("Kartta sertifika bulunmuyor");

            if (checkIsQualified && checkBeingNonQualified)
                throw new ESYAException(
                    "Bir sertifika ya nitelikli sertifikadir, ya niteliksiz sertifikadir. Hem nitelikli hem niteliksiz olamaz");

            List<ECertificate> certs = new List<ECertificate>();

            foreach (byte[] bs in aCerts)
            {
                ECertificate cert = new ECertificate(bs);

                if (checkIsQualified)
                {
                    if (cert.isQualifiedCertificate())
                        certs.Add(cert);
                }
                else if (checkBeingNonQualified)
                {
                    if (!cert.isQualifiedCertificate())
                        certs.Add(cert);
                }
                else
                {
                    certs.Add(cert);
                }
            }

            ECertificate selectedCert = null;

            if (certs.Count == 0)
            {
                if (checkIsQualified)
                    throw new ESYAException("Kartta nitelikli sertifika bulunmuyor");
                else if (checkBeingNonQualified)
                    throw new ESYAException("Kartta niteliksiz sertifika bulunmuyor");
            }
            else if (certs.Count == 1)
            {
                selectedCert = certs[0];
            }
            else
            {
                String[] optionList = new String[certs.Count];
                for (int i = 0; i < certs.Count; i++)
                {
                    optionList[i] = certs[i].getSubject().getCommonNameAttribute();
                }

                int result = askOption(null, null, optionList, "Sertifika Listesi", new[] { "Tamam" });

                if (result < 0)
                    selectedCert = null;
                else
                    selectedCert = certs[result];
            }
            return selectedCert;
        }


        private String getSelectedSerialNumber()
        {
            return mSerialNumber[aktifkart];
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int getSlotCount()
        {
            return mSlotCount;
        }

        public IBaseSmartCard getBasicSmartCard()
        {
            return bsc[aktifkart];
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void reset()
        {
            mSCManager = null;
        }


        public static int askOption(Control aParent, Icon aIcon, String[] aSecenekList, String aBaslik,
                                     String[] aOptions)
        {
            SlotList sl = new SlotList(null, aIcon, aSecenekList, aBaslik);
            DialogResult result = sl.ShowDialog();
            if (result != DialogResult.OK)
                return -1;
            return sl.getSelectedIndex();
        }

    }
}