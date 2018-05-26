
using System;
using System.Collections;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using tr.gov.tubitak.uekae.esya.api.asn.x509;
using tr.gov.tubitak.uekae.esya.api.common;
using tr.gov.tubitak.uekae.esya.api.common.crypto;
using tr.gov.tubitak.uekae.esya.api.crypto.alg;
using tr.gov.tubitak.uekae.esya.api.crypto.provider.bouncy;
using tr.gov.tubitak.uekae.esya.api.crypto.util;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace utils
{
    public class EPfxSigner:BaseSigner
    {
        private string signatureAlgorithmStr;

        private AsymmetricAlgorithm signingPrivateKey;

        private X509CertificateEntry signingBouncyCert;
        private AsymmetricKeyEntry signingBouncyKeyEntry;

        private ECertificate signingCertificate;

        private ECertificate signRequestCertificate=null;
 
        //private RSACryptoServiceProvider signingPrivateKey;

        public EPfxSigner(string aSignatureAlgorithmStr, string aPfxFilePath, string aPassword)
        {
            signatureAlgorithmStr = aSignatureAlgorithmStr;
            init(aPfxFilePath,aPassword);
        }

        private void init(string aPfxFilePath,string aPassword)
        {
            FileStream fin = new FileStream(aPfxFilePath, FileMode.Open, FileAccess.Read);
            Pkcs12StoreBuilder storeBuilder = new Pkcs12StoreBuilder();
            Pkcs12Store pkcs12Store = storeBuilder.Build();
            pkcs12Store.Load(fin, aPassword.ToCharArray());
            fin.Close();
            IEnumerable aliases = pkcs12Store.Aliases;
            IEnumerator aliasesEnumerator = aliases.GetEnumerator();
            while (aliasesEnumerator.MoveNext())
            {
                string alias = (string) aliasesEnumerator.Current;
                signingBouncyCert = pkcs12Store.GetCertificate(alias);
                X509Certificate x509Certificate = signingBouncyCert.Certificate;
                ECertificate cert = new ECertificate(x509Certificate.GetEncoded());
                EKeyUsage eKeyUsage = cert.getExtensions().getKeyUsage();
                bool isDigitalSignature = eKeyUsage.isDigitalSignature();
                if (isDigitalSignature)
                {
                    signingBouncyKeyEntry = pkcs12Store.GetKey(alias);
                    signingCertificate = cert;
                    break;
                }                
            }
        }

        private byte[] getPrefixForDigestAlg(String aDigestAlg)
        {
            if (aDigestAlg.Equals(Algorithms.DIGEST_SHA1))
                return sha1Prefix;
            else if (aDigestAlg.Equals(Algorithms.DIGEST_SHA256))
                return sha256Prefix;
            if (aDigestAlg.Equals(Algorithms.DIGEST_SHA384))
                return sha384Prefix;
            if (aDigestAlg.ToUpper().Equals(Algorithms.DIGEST_SHA512))
                return sha512Prefix;

            throw new ESYAException(aDigestAlg + " UnKnown digest algorithm");
        }

        private static readonly byte[] sha1Prefix = new[]
                                                        {
                                                            (byte) 0x30, (byte) 0x21, (byte) 0x30, (byte) 0x09,
                                                            (byte) 0x06, (byte) 0x05, (byte) 0x2B,
                                                            (byte) 0x0E, (byte) 0x03, (byte) 0x02, (byte) 0x1A,
                                                            (byte) 0x05, (byte) 0x00, (byte) 0x04, (byte) 0x14
                                                        };

        private static readonly byte[] sha256Prefix = new[]
                                                          {
                                                              (byte) 0x30, (byte) 0x31, (byte) 0x30, (byte) 0x0d,
                                                              (byte) 0x06, (byte) 0x09, (byte) 0x60,
                                                              (byte) 0x86, (byte) 0x48, (byte) 0x01, (byte) 0x65,
                                                              (byte) 0x03, (byte) 0x04, (byte) 0x02, (byte) 0x01,
                                                              (byte) 0x05, (byte) 0x00, (byte) 0x04, (byte) 0x20
                                                          };

        private static readonly byte[] sha384Prefix = new[]
                                                          {
                                                              (byte) 0x30, (byte) 0x41, (byte) 0x30, (byte) 0x0d,
                                                              (byte) 0x06, (byte) 0x09, (byte) 0x60,
                                                              (byte) 0x86, (byte) 0x48, (byte) 0x01, (byte) 0x65,
                                                              (byte) 0x03, (byte) 0x04, (byte) 0x02, (byte) 0x02,
                                                              (byte) 0x05, (byte) 0x00, (byte) 0x04, (byte) 0x30
                                                          };

        private static readonly byte[] sha512Prefix = new[]
                                                          {
                                                              (byte) 0x30, (byte) 0x51, (byte) 0x30, (byte) 0x0d,
                                                              (byte) 0x06, (byte) 0x09, (byte) 0x60,
                                                              (byte) 0x86, (byte) 0x48, (byte) 0x01, (byte) 0x65,
                                                              (byte) 0x03, (byte) 0x04, (byte) 0x02, (byte) 0x03,
                                                              (byte) 0x05, (byte) 0x00, (byte) 0x04, (byte) 0x40
                                                          };

        private byte[] getSignatureInput(byte[] aData)
        {
            DigestAlg digestAlg = null;
            String digestAlgStr = null;
            try
            {
                digestAlgStr = Algorithms.getDigestAlgOfSignatureAlg(signatureAlgorithmStr);
                digestAlg = DigestAlg.fromName(digestAlgStr);
            }

            catch (ESYAException e)
            {
                throw new ESYAException("UnKnown Digest Algorithm", e);
            }
            catch (Exception aEx)
            {
                throw new Exception(digestAlg + " algorithm is not supported",aEx);
            }

            byte[] messageHash = DigestUtil.digest(digestAlg, aData);
            byte[] hashPrefix = getPrefixForDigestAlg(digestAlgStr);

            byte[] realHashstruct = new byte[hashPrefix.Length + messageHash.Length];
            Array.Copy(hashPrefix, 0, realHashstruct, 0,
                       hashPrefix.Length);
            Array.Copy(messageHash, 0, realHashstruct, hashPrefix.Length,
                       messageHash.Length);
            return realHashstruct;
        }

        public void setSignRequestCertificate(ECertificate signRequestCertificate)
        {
            this.signRequestCertificate = signRequestCertificate;
        }

        private byte[] signData(byte[] aData)
        {
            SignatureAlg signatureAlg = SignatureAlg.fromName(signatureAlgorithmStr);
            BouncySigner bouncySigner = new BouncySigner(signatureAlg);
            AsymmetricKeyParameter asymmetricKeyParameter = signingBouncyKeyEntry.Key;
            bouncySigner.init(asymmetricKeyParameter);
            byte[] signature = bouncySigner.sign(aData);
            return signature;
        }
        public byte[] sign(byte[] aData)
        {
            try
            {
                if (signRequestCertificate != null)
                {
                    if (signingCertificate.getSerialNumberHex().CompareTo(signRequestCertificate.getSerialNumberHex()) != 0)
                    {
                        throw new Exception("Pfx içindeki sertifika ile imzalanmak istenen sertifika birbirinden farklı.");
                    }
                }
                return signData(aData);
            }
            catch (Exception exc)
            {
               System.Console.WriteLine(exc.Message);
                throw exc;
            }
        }

        public string getSignatureAlgorithmStr()
        {
            return signatureAlgorithmStr;
        }

        public ECertificate getSigningCertificate()
        {
            return signingCertificate;
        }
    }
}
