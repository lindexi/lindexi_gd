using System.Xml.Serialization;

namespace KaldaygeduWalaineejaw
{
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/office/2006/encryption")]
    [System.Xml.Serialization.XmlRootAttribute("encryption",Namespace = "http://schemas.microsoft.com/office/2006/encryption", IsNullable = false)]
    public partial class Encryption
    {

        private encryptionKeyData keyDataField;

        private encryptionDataIntegrity dataIntegrityField;

        private encryptionKeyEncryptors keyEncryptorsField;

        [XmlElement("keyData")]
        public encryptionKeyData KeyData
        {
            get
            {
                return this.keyDataField;
            }
            set
            {
                this.keyDataField = value;
            }
        }

        /// <remarks/>
        [XmlElement("dataIntegrity")]
        public encryptionDataIntegrity DataIntegrity
        {
            get
            {
                return this.dataIntegrityField;
            }
            set
            {
                this.dataIntegrityField = value;
            }
        }

        /// <remarks/>
        [XmlElement("keyEncryptors")]
        public encryptionKeyEncryptors KeyEncryptors
        {
            get
            {
                return this.keyEncryptorsField;
            }
            set
            {
                this.keyEncryptorsField = value;
            }
        }
    }
}