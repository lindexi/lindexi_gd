namespace KaldaygeduWalaineejaw
{
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/office/2006/encryption")]
    public partial class encryptionDataIntegrity
    {

        private string encryptedHmacValueField;

        private string encryptedHmacKeyField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string encryptedHmacValue
        {
            get
            {
                return this.encryptedHmacValueField;
            }
            set
            {
                this.encryptedHmacValueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string encryptedHmacKey
        {
            get
            {
                return this.encryptedHmacKeyField;
            }
            set
            {
                this.encryptedHmacKeyField = value;
            }
        }
    }
}