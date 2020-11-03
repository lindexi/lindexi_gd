using System;
using System.IO;
using System.IO.Packaging;

namespace DocumentFormat.OpenXml.Packaging
{
    public interface IPackage
    {
        /// <summary>
        /// Gets the FileAccess with which the package was opened. This is a read only property.
        /// This property gets set when the package is opened.
        /// </summary>
        /// <value>FileAccess</value>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        FileAccess FileOpenAccess { get; }

        /// <summary>
        /// The package properties are a subset of the standard OLE property sets
        /// SummaryInformation and DocumentSummaryInformation, and include such properties
        /// as Title and Subject.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        PackageProperties PackageProperties { get; }

        /// <summary>
        /// Creates a new part in the package. An empty stream corresponding to this part will be created in the
        /// package. If a part with the specified uri already exists then we throw an exception.
        /// This methods will call the CreatePartCore method which will create the actual PackagePart in the package.
        /// </summary>
        /// <param name="partUri">Uri of the PackagePart that is to be added</param>
        /// <param name="contentType">ContentType of the stream to be added</param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentNullException">If contentType parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        /// <exception cref="InvalidOperationException">If a PackagePart with the given partUri already exists in the Package</exception>
        PackagePart CreatePart(Uri partUri, string contentType);

        /// <summary>
        /// Creates a new part in the package. An empty stream corresponding to this part will be created in the
        /// package. If a part with the specified uri already exists then we throw an exception.
        /// This methods will call the CreatePartCore method which will create the actual PackagePart in the package.
        /// </summary>
        /// <param name="partUri">Uri of the PackagePart that is to be added</param>
        /// <param name="contentType">ContentType of the stream to be added</param>
        /// <param name="compressionOption">CompressionOption  describing compression configuration
        /// for the new part. This compression apply only to the part, it doesn't affect relationship parts or related parts.
        /// This parameter is optional. </param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentNullException">If contentType parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        /// <exception cref="ArgumentOutOfRangeException">If CompressionOption enumeration [compressionOption] does not have one of the valid values</exception>
        /// <exception cref="InvalidOperationException">If a PackagePart with the given partUri already exists in the Package</exception>
        PackagePart CreatePart(Uri partUri,
            string contentType,
            CompressionOption compressionOption);

        /// <summary>
        /// Returns a part that already exists in the package. If the part
        /// Corresponding to the URI does not exist in the package then an exception is
        /// thrown. The method calls the GetPartCore method which actually fetches the part.
        /// </summary>
        /// <param name="partUri"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, information cannot be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        /// <exception cref="InvalidOperationException">If the requested part does not exists in the Package</exception>
        PackagePart GetPart(Uri partUri);

        /// <summary>
        /// This is a convenient method to check whether a given part exists in the
        /// package. This will have a default implementation that will try to retrieve
        /// the part and then if successful, it will return true.
        /// If the custom file format has an easier way to do this, they can override this method
        /// to get this information in a more efficient way.
        /// </summary>
        /// <param name="partUri"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, information cannot be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        bool PartExists(Uri partUri);

        /// <summary>
        /// This method will do all the house keeping required when a part is deleted
        /// Then the DeletePartCore method will be called which will have the actual logic to
        /// do the work specific to the underlying file format and will actually delete the
        /// stream corresponding to this part. This method does not throw if the specified
        /// part does not exist. This is in conformance with the FileInfo.Delete call.
        /// </summary>
        /// <param name="partUri"></param>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        void DeletePart(Uri partUri);

        /// <summary>
        /// This returns a collection of all the Parts within the package.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is writeonly, no information can be retrieved from it</exception>
        PackagePartCollection GetParts();

        /// <summary>
        /// Closes the package and all the underlying parts and relationships.
        /// Calls the Dispose Method, since they have the same semantics
        /// </summary>
        void Close();

        /// <summary>
        /// Flushes the contents of the parts and the relationships to the package.
        /// This method will call the FlushCore method which will do the actual flushing of contents.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        void Flush();

        /// <summary>
        /// Creates a relationship at the Package level with the Target PackagePart specified as the Uri
        /// </summary>
        /// <param name="targetUri">Target's URI</param>
        /// <param name="targetMode">Enumeration indicating the base uri for the target uri</param>
        /// <param name="relationshipType">PackageRelationship type, having uri like syntax that is used to
        /// uniquely identify the role of the relationship</param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If parameter "targetUri" is null</exception>
        /// <exception cref="ArgumentNullException">If parameter "relationshipType" is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If parameter "targetMode" enumeration does not have a valid value</exception>
        /// <exception cref="ArgumentException">If TargetMode is TargetMode.Internal and the targetUri is an absolute Uri </exception>
        /// <exception cref="ArgumentException">If relationship is being targeted to a relationship part</exception>
        PackageRelationship CreateRelationship(Uri targetUri, TargetMode targetMode, string relationshipType);

        /// <summary>
        /// Creates a relationship at the Package level with the Target PackagePart specified as the Uri
        /// </summary>
        /// <param name="targetUri">Target's URI</param>
        /// <param name="targetMode">Enumeration indicating the base uri for the target uri</param>
        /// <param name="relationshipType">PackageRelationship type, having uri like syntax that is used to
        /// uniquely identify the role of the relationship</param>
        /// <param name="id">String that conforms to the xsd:ID datatype. Unique across the source's
        /// relationships. Null is OK (ID will be generated). An empty string is an invalid XML ID.</param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If parameter "targetUri" is null</exception>
        /// <exception cref="ArgumentNullException">If parameter "relationshipType" is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If parameter "targetMode" enumeration does not have a valid value</exception>
        /// <exception cref="ArgumentException">If TargetMode is TargetMode.Internal and the targetUri is an absolute Uri </exception>
        /// <exception cref="ArgumentException">If relationship is being targeted to a relationship part</exception>
        /// <exception cref="System.Xml.XmlException">If parameter "id" is not a valid Xsd Id</exception>
        /// <exception cref="System.Xml.XmlException">If an id is provided in the method, and its not unique</exception>
        PackageRelationship CreateRelationship(Uri targetUri, TargetMode targetMode, string relationshipType, string id);

        /// <summary>
        /// Deletes a relationship from the Package. This is done based on the
        /// relationship's ID. The target PackagePart is not affected by this operation.
        /// </summary>
        /// <param name="id">The ID of the relationship to delete. An invalid ID will not
        /// throw an exception, but nothing will be deleted.</param>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If parameter "id" is null</exception>
        /// <exception cref="System.Xml.XmlException">If parameter "id" is not a valid Xsd Id</exception>
        void DeleteRelationship(string id);

        /// <summary>
        /// Returns a collection of all the Relationships that are
        /// owned by the package
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, no information can be retrieved from it</exception>
        PackageRelationshipCollection GetRelationships();

        /// <summary>
        /// Returns a collection of filtered Relationships that are
        /// owned by the package
        /// The filter string is compared with the type of the relationships
        /// in a case sensitive and culture ignorant manner.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, no information can be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If parameter "relationshipType" is null</exception>
        /// <exception cref="ArgumentException">If parameter "relationshipType" is an empty string</exception>
        PackageRelationshipCollection GetRelationshipsByType(string relationshipType);

        /// <summary>
        /// Retrieve a relationship per ID.
        /// </summary>
        /// <param name="id">The relationship ID.</param>
        /// <returns>The relationship with ID 'id' or throw an exception if not found.</returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, no information can be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If parameter "id" is null</exception>
        /// <exception cref="System.Xml.XmlException">If parameter "id" is not a valid Xsd Id</exception>
        /// <exception cref="InvalidOperationException">If the requested relationship does not exist in the Package</exception>
        PackageRelationship GetRelationship(string id);

        /// <summary>
        /// Returns whether there is a relationship with the specified ID.
        /// </summary>
        /// <param name="id">The relationship ID.</param>
        /// <returns>true iff a relationship with ID 'id' is defined on this source.</returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, no information can be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If parameter "id" is null</exception>
        /// <exception cref="System.Xml.XmlException">If parameter "id" is not a valid Xsd Id</exception>
        bool RelationshipExists(string id);

    }
}