namespace KaldaygeduWalaineejaw
{
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/office/2006/encryption")]
    public partial class encryptionKeyEncryptorsKeyEncryptor
    {

        private encryptedKey encryptedKeyField;

        private string uriField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://schemas.microsoft.com/office/2006/keyEncryptor/password")]
        public encryptedKey encryptedKey
        {
            get
            {
                return this.encryptedKeyField;
            }
            set
            {
                this.encryptedKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string uri
        {
            get
            {
                return this.uriField;
            }
            set
            {
                this.uriField = value;
            }
        }
    }
}