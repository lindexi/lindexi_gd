namespace KaldaygeduWalaineejaw
{
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/office/2006/encryption")]
    public partial class encryptionKeyEncryptors
    {

        private encryptionKeyEncryptorsKeyEncryptor keyEncryptorField;

        /// <remarks/>
        public encryptionKeyEncryptorsKeyEncryptor keyEncryptor
        {
            get
            {
                return this.keyEncryptorField;
            }
            set
            {
                this.keyEncryptorField = value;
            }
        }
    }
}