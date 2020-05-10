using System;
using System.IO;
using System.IO.Packaging;

namespace DocumentFormat.OpenXml.Packaging
{
    internal class PackageAdapt : IPackage
    {
        public PackageAdapt(Package package)
        {
            Package = package;
        }

        private System.IO.Packaging.Package Package { get; }

        /// <inheritdoc />
        public FileAccess FileOpenAccess => Package.FileOpenAccess;

        /// <inheritdoc />
        public PackageProperties PackageProperties => Package.PackageProperties;

        /// <inheritdoc />
        public PackagePart CreatePart(Uri partUri, string contentType)
        {
            return Package.CreatePart(partUri, contentType);
        }

        /// <inheritdoc />
        public PackagePart CreatePart(Uri partUri, string contentType, CompressionOption compressionOption)
        {
            return Package.CreatePart(partUri, contentType, compressionOption);
        }

        /// <inheritdoc />
        public PackagePart GetPart(Uri partUri)
        {
            return Package.GetPart(partUri);
        }

        /// <inheritdoc />
        public bool PartExists(Uri partUri)
        {
            return Package.PartExists(partUri);
        }

        /// <inheritdoc />
        public void DeletePart(Uri partUri)
        {
            Package.DeletePart(partUri);
        }

        /// <inheritdoc />
        public PackagePartCollection GetParts()
        {
            return Package.GetParts();
        }

        /// <inheritdoc />
        public void Close()
        {
            Package.Close();
        }

        /// <inheritdoc />
        public void Flush()
        {
            Package.Flush();
        }

        /// <inheritdoc />
        public PackageRelationship CreateRelationship(Uri targetUri, TargetMode targetMode, string relationshipType)
        {
            return Package.CreateRelationship(targetUri, targetMode, relationshipType);
        }

        /// <inheritdoc />
        public PackageRelationship CreateRelationship(Uri targetUri, TargetMode targetMode, string relationshipType, string id)
        {
            return Package.CreateRelationship(targetUri, targetMode, relationshipType, id);
        }

        /// <inheritdoc />
        public void DeleteRelationship(string id)
        {
            Package.DeleteRelationship(id);
        }

        /// <inheritdoc />
        public PackageRelationshipCollection GetRelationships()
        {
            return Package.GetRelationships();
        }

        /// <inheritdoc />
        public PackageRelationshipCollection GetRelationshipsByType(string relationshipType)
        {
            return Package.GetRelationshipsByType(relationshipType);
        }

        /// <inheritdoc />
        public PackageRelationship GetRelationship(string id)
        {
            return Package.GetRelationship(id);
        }

        /// <inheritdoc />
        public bool RelationshipExists(string id)
        {
            return Package.RelationshipExists(id);
        }
    }
}