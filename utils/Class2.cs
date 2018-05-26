using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
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
 * SmartCardManager handles user smart card operations.
 *
 */

namespace utils
{
    public class SmartCardManagerSelectFromIdentity
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //private static Object lockObject = new Object();

        private static SmartCardManager mSCManager;

        private int mSlotCount = 0;

        private readonly String mSerialNumber;

        private ECertificate mSignatureCert;

        private ECertificate mEncryptionCert;

        protected IBaseSmartCard bsc;

        protected BaseSigner mSigner;

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
         * @return SmartCardManager instance
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
        public static SmartCardManager getInstance()
        {
            if (mSCManager == null)
            {
                mSCManager = new SmartCardManager();
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
                        return getInstance();
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
                        return getInstance();
                    }
                    if (!mSCManager.getSelectedSerialNumber().Equals(availableSerial))
                    {
                        LOGGER.Debug("Serial number changed. New card is placed to system");
                        mSCManager = null;
                        return getInstance();
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

        public SmartCardManager()
        {
            try
            {
                LOGGER.Debug("New SmartCardManager will be created");
                String[] terminals = SmartOp.getCardTerminals();
                String terminal;


                if (terminals == null || terminals.Length == 0)
                {
                    MesajiIsle("Kart takılı kart okuyucu bulunamadı-SmartCardManager", 1);
                    Program.KartOkuyucuYok = 1;
                    return;
                    // throw new SmartCardException("Kart takılı kart okuyucu bulunamadı");
                }

                LOGGER.Debug("Kart okuyucu sayısı : " + terminals.Length);
                if (terminals.Length != Program.TerminalSayisi && Program.TerminalSayisi != 0)
                {
                    MesajiIsle("Kart seçildikten sonra imzalama aşamasında yeni kart okuyucu takıldı.", 1);
                    Program.KartOkuyucuYok = 1;
                    return;
                }
                Program.TerminalSayisi = terminals.Length;
                int index = 0;
                if (terminals.Length == 1)
                    terminal = terminals[index];
                else
                {
                    index = askOption(null, null, terminals, "Okuyucu Listesi", new String[] { "Tamam" });
                    terminal = terminals[index];
                }
                Pair<long, CardType> slotAndCardType = SmartOp.getSlotAndCardType(terminal);
                bsc = new P11SmartCard(slotAndCardType.getmObj2());
                bsc.openSession(slotAndCardType.getmObj1());

                mSerialNumber = StringUtil.ToString(bsc.getSerial());
                mSlotCount = terminals.Length;
            }
            catch (SmartCardException e)
            {
                throw e;
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public BaseSigner getSigner(String aCardPIN, ECertificate aCert)
        {
            try
            {
                if (mSigner == null)
                {
                    bsc.login(aCardPIN);
                    mSigner = bsc.getSigner(aCert, Algorithms.SIGNATURE_RSA_SHA256);
                }
            }
            catch (PKCS11Exception e)
            {
                throw new SmartCardException("Pkcs11 exception - Detay bilgilere bakınız", e);
            }
            catch (Exception exc)
            {
                // probably couldn't write to the file
                MesajiIsle("Hata Oluştu." + exc.Message, 1);
            }
            return mSigner;

        }


        /**
         * Logouts from smart card. 
         * @throws SmartCardException
         */

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void logout()
        {
            mSigner = null;
            bsc.logout();
        }

        /**
         * Returns for the signature certificate. If there are more than one certificates in the card in requested
         * attributes, it wants user to select the certificate. It caches the selected certificate, to reset cache,
         * call reset function.
         * 
         * @param checkIsQualified Only selects the qualified certificates if it is true.
         * @param checkBeingNonQualified Only selects the non-qualified certificates if it is true. 
         * if the two parameters are false, it selects all certificates.
         * if the two parameters are true, it throws ESYAException. A certificate can not be qualified and non qualified at
         * the same time.
         * 
         * @return certificate
         * @throws SmartCardException
         * @throws ESYAException
         */

        [MethodImpl(MethodImplOptions.Synchronized)]
        public ECertificate getSignatureCertificate(bool checkIsQualified, bool checkBeingNonQualified)
        {
            if (mSignatureCert == null)
            {
                List<byte[]> allCerts = bsc.getSignatureCertificates();
                mSignatureCert = selectCertificate(checkIsQualified, checkBeingNonQualified, allCerts);
            }

            return mSignatureCert;
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
                List<byte[]> allCerts = bsc.getEncryptionCertificates();
                mEncryptionCert = selectCertificate(checkIsQualified, checkBeingNonQualified, allCerts);
            }

            return mEncryptionCert;
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
            return mSerialNumber;
        }

        private int getSlotCount()
        {
            return mSlotCount;
        }

        public IBaseSmartCard getBasicSmartCard()
        {
            return bsc;
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