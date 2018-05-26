using System;
using System.IO;
using tr.gov.tubitak.uekae.esya.api.asn.x509;
using tr.gov.tubitak.uekae.esya.api.common.crypto;

namespace utils
{
    public class KeyOrSmartCardSignManager
    {
        public static readonly String SIGNATURE_ALGORITHM = Algorithms.SIGNATURE_RSA_SHA256;
        private SmartCardManager smartCardManager;
        private EPfxSigner pfxSigner;

        private bool useSmartCard = true;

        private static KeyOrSmartCardSignManager instance;
        public static KeyOrSmartCardSignManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new KeyOrSmartCardSignManager();
                }
                return instance;
            }
        }

        private KeyOrSmartCardSignManager()
        {
            if (useSmartCard)
            {
                smartCardManager = SmartCardManager.getInstance();
            }
            else
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string pfxPath = currentDirectory + "/certs/GercekSistem_072801_test2.pfx";
                pfxSigner = new EPfxSigner(SIGNATURE_ALGORITHM, pfxPath, "072801");
            }
        }

        public bool UseSmartCard
        {
            get
            {
                return useSmartCard;
            }
            set
            {
                useSmartCard = value;
            }
        }


       public ECertificate getSigningCertificate()
        {
            if (useSmartCard)
            {
                try
                {
                    SmartCardManager cardManager = SmartCardManager.getInstance();
                    return cardManager.getSignatureCertificate(Constants.WORK_ONLY_WITH_QUALIFIED_CERTS, false);
                }
                catch (Exception exc)
                {
                    System.Console.WriteLine(exc.StackTrace);
                    System.Console.WriteLine("Can't read certificate");
                    throw new Exception("Can't read certificate");
                }
            }
            else
            {
                return pfxSigner.getSigningCertificate();
            }
           return null;
        }

        public BaseSigner getSigner(ECertificate signingCert)
        {
            if (useSmartCard)
            {
                string smartCardPin = Constants.SMART_CARD_PIN;
                if (smartCardPin.Length == 0)
                {
                    throw new Exception("Please define smart card password in Constants class.");
                }
                if (signingCert == null)
                    signingCert = getSigningCertificate();
                BaseSigner smartCardSigner = smartCardManager.getSigner(smartCardPin, signingCert);
                return smartCardSigner;
            }
            else
            {
                return pfxSigner;
            }
            return null;
        }
    }
}
