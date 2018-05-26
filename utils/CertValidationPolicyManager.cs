using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using tr.gov.tubitak.uekae.esya.api.certificate.validation.policy;

namespace utils
{
    public class CertValidationPolicyManager
    {
        private ValidationPolicy validationPolicy=null;

        public ValidationPolicy getValidationPolicy()
        {
            return validationPolicy;
        }

        private static CertValidationPolicyManager instance = null;
        public static CertValidationPolicyManager getInstance()
        {
            if (instance == null)
            {
                instance = new CertValidationPolicyManager();
            }
            return instance;
        }

        private CertValidationPolicyManager()
        {
            init();
        }

        public void reset()
        {
            validationPolicy = null;
        }

        private void init()
        {
            if (validationPolicy == null)
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string policyFilePath = currentDirectory + "\\certval-policy.xml";
                validationPolicy = PolicyReader.readValidationPolicy(policyFilePath);
            }
        }
    }
}
