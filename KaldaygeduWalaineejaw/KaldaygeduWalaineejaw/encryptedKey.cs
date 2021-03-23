namespace KaldaygeduWalaineejaw
{
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/office/2006/keyEncryptor/password")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://schemas.microsoft.com/office/2006/keyEncryptor/password", IsNullable = false)]
    public partial class encryptedKey
    {

        private string saltValueField;

        private string hashAlgorithmField;

        private string cipherChainingField;

        private string cipherAlgorithmField;

        private byte hashSizeField;

        private ushort keyBitsField;

        private byte blockSizeField;

        private byte saltSizeField;

        private string encryptedKeyValueField;

        private string encryptedVerifierHashValueField;

        private string encryptedVerifierHashInputField;

        private uint spinCountField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string saltValue
        {
            get
            {
                return this.saltValueField;
            }
            set
            {
                this.saltValueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string hashAlgorithm
        {
            get
            {
                return this.hashAlgorithmField;
            }
            set
            {
                this.hashAlgorithmField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string cipherChaining
        {
            get
            {
                return this.cipherChainingField;
            }
            set
            {
                this.cipherChainingField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string cipherAlgorithm
        {
            get
            {
                return this.cipherAlgorithmField;
            }
            set
            {
                this.cipherAlgorithmField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte hashSize
        {
            get
            {
                return this.hashSizeField;
            }
            set
            {
                this.hashSizeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort keyBits
        {
            get
            {
                return this.keyBitsField;
            }
            set
            {
                this.keyBitsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte blockSize
        {
            get
            {
                return this.blockSizeField;
            }
            set
            {
                this.blockSizeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte saltSize
        {
            get
            {
                return this.saltSizeField;
            }
            set
            {
                this.saltSizeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string encryptedKeyValue
        {
            get
            {
                return this.encryptedKeyValueField;
            }
            set
            {
                this.encryptedKeyValueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string encryptedVerifierHashValue
        {
            get
            {
                return this.encryptedVerifierHashValueField;
            }
            set
            {
                this.encryptedVerifierHashValueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string encryptedVerifierHashInput
        {
            get
            {
                return this.encryptedVerifierHashInputField;
            }
            set
            {
                this.encryptedVerifierHashInputField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint spinCount
        {
            get
            {
                return this.spinCountField;
            }
            set
            {
                this.spinCountField = value;
            }
        }
    }
}