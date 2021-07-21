using System;
using System.Collections.Generic;
using System.Text;
using System.Xaml.Resources;


   public static class SRID
    {
            private static global::System.Resources.ResourceManager resourceMan;

            private static global::System.Globalization.CultureInfo resourceCulture;

            /// <summary>
            ///   返回此类使用的缓存的 ResourceManager 实例。
            /// </summary>
            [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
            internal static global::System.Resources.ResourceManager ResourceManager
            {
                get
                {
                    if (object.ReferenceEquals(resourceMan, null))
                    {
                        global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("System.Xaml.Resources.Strings", typeof(Strings).Assembly);
                        resourceMan = temp;
                    }
                    return resourceMan;
                }
            }

            /// <summary>
            ///   重写当前线程的 CurrentUICulture 属性
            ///   重写当前线程的 CurrentUICulture 属性。
            /// </summary>
            [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
            internal static global::System.Globalization.CultureInfo Culture
            {
                get
                {
                    return resourceCulture;
                }
                set
                {
                    resourceCulture = value;
                }
            }

            /// <summary>
            ///   查找类似 Add value to collection of type &apos;{0}&apos; threw an exception. 的本地化字符串。
            /// </summary>
            internal static string AddCollection
            {
                get
                {
                    return ResourceManager.GetString("AddCollection", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Add value to dictionary of type &apos;{0}&apos; threw an exception. 的本地化字符串。
            /// </summary>
            internal static string AddDictionary
            {
                get
                {
                    return ResourceManager.GetString("AddDictionary", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot determine the item type of collection type &apos;{0}&apos; because it has more than one Add method or ICollection&lt;T&gt; implementation. To make this collection type usable in XAML, add a public Add(object) method, implement System.Collections.IList or a single System.Collections.Generic.ICollection&lt;T&gt;. 的本地化字符串。
            /// </summary>
            internal static string AmbiguousCollectionItemType
            {
                get
                {
                    return ResourceManager.GetString("AmbiguousCollectionItemType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot determine the item type of dictionary type &apos;{0}&apos; because it has more than one Add method or IDictionary&lt;K,V&gt; implementation. To make this dictionary type usable in XAML, add a public Add(object,object) method, implement System.Collections.IDictionary or a single System.Collections.Generic.IDictionary&lt;K,V&gt;. 的本地化字符串。
            /// </summary>
            internal static string AmbiguousDictionaryItemType
            {
                get
                {
                    return ResourceManager.GetString("AmbiguousDictionaryItemType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 A child of KeyFrameAnimation in XAML must be a KeyFrame of a compatible type. 的本地化字符串。
            /// </summary>
            internal static string Animation_ChildMustBeKeyFrame
            {
                get
                {
                    return ResourceManager.GetString("Animation_ChildMustBeKeyFrame", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; cannot use default {1} value of &apos;{2}&apos;. 的本地化字符串。
            /// </summary>
            internal static string Animation_Invalid_DefaultValue
            {
                get
                {
                    return ResourceManager.GetString("Animation_Invalid_DefaultValue", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; must have either a TimeSpan for its Duration or a TimeSpan for the KeyTime of its last KeyFrame. This &apos;{0}&apos; has a Duration of &apos;{1}&apos; and a KeyTime of &apos;{2}&apos; for its last KeyFrame, so the KeyTimes cannot be resolved. 的本地化字符串。
            /// </summary>
            internal static string Animation_InvalidAnimationUsingKeyFramesDuration
            {
                get
                {
                    return ResourceManager.GetString("Animation_InvalidAnimationUsingKeyFramesDuration", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; is not a valid &apos;{1}&apos; value for class &apos;{2}&apos;. This value might have been supplied by the base value of the property being animated or the output value of another animation applied to the same property. 的本地化字符串。
            /// </summary>
            internal static string Animation_InvalidBaseValue
            {
                get
                {
                    return ResourceManager.GetString("Animation_InvalidBaseValue", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Resolved KeyTime for key frame at index {1} cannot be greater than resolved KeyTime for key frame at index {4}. KeyFrames[{1}]5D; has specified KeyTime &apos;{2}&apos;, which resolves to time {3}; KeyFrames[{4}]5D; has specified KeyTime &apos;{5}&apos;, which resolves to time {6}. Some KeyTimes are resolved relative to Begin time of &apos;{0}&apos; and others relative to its Duration, so some sets of KeyTimes are not valid for all Durations. 的本地化字符串。
            /// </summary>
            internal static string Animation_InvalidResolvedKeyTimes
            {
                get
                {
                    return ResourceManager.GetString("Animation_InvalidResolvedKeyTimes", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{2}&apos; KeyTime value is not valid for key frame at index {1} of this &apos;{0}&apos; because it is greater than animation&apos;s Duration value &apos;{3}&apos;. 的本地化字符串。
            /// </summary>
            internal static string Animation_InvalidTimeKeyTime
            {
                get
                {
                    return ResourceManager.GetString("Animation_InvalidTimeKeyTime", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 KeyFrameAnimation objects cannot have text objects as children. 的本地化字符串。
            /// </summary>
            internal static string Animation_NoTextChildren
            {
                get
                {
                    return ResourceManager.GetString("Animation_NoTextChildren", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Enumerating attached properties on object &apos;{0}&apos; threw an exception. 的本地化字符串。
            /// </summary>
            internal static string APSException
            {
                get
                {
                    return ResourceManager.GetString("APSException", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 One of the following arguments must be non-null: &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string ArgumentRequired
            {
                get
                {
                    return ResourceManager.GetString("ArgumentRequired", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Array Add method is not implemented. 的本地化字符串。
            /// </summary>
            internal static string ArrayAddNotImplemented
            {
                get
                {
                    return ResourceManager.GetString("ArrayAddNotImplemented", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Part between semicolon &apos;;&apos; and equals sign &apos;=&apos; is not &apos;{0}&apos; URI &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string AssemblyTagMissing
            {
                get
                {
                    return ResourceManager.GetString("AssemblyTagMissing", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Attachable events are not implemented. 的本地化字符串。
            /// </summary>
            internal static string AttachableEventNotImplemented
            {
                get
                {
                    return ResourceManager.GetString("AttachableEventNotImplemented", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Attachable member &apos;{0}&apos; was not found. 的本地化字符串。
            /// </summary>
            internal static string AttachableMemberNotFound
            {
                get
                {
                    return ResourceManager.GetString("AttachableMemberNotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 An attachable property named &apos;{1}&apos; is attached on a dictionary key &apos;{0}&apos; that is either a string or can be type-converted to string, which is not supported. 的本地化字符串。
            /// </summary>
            internal static string AttachedPropertyOnDictionaryKey
            {
                get
                {
                    return ResourceManager.GetString("AttachedPropertyOnDictionaryKey", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 An attachable property named &apos;{2}&apos; is attached to a property named &apos;{1}&apos;.  The property named &apos;{1}&apos; is either a string or can be type-converted to string; attaching on such properties are not supported.  For debugging, the property &apos;{1}&apos; contains an object &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string AttachedPropertyOnTypeConvertedOrStringProperty
            {
                get
                {
                    return ResourceManager.GetString("AttachedPropertyOnTypeConvertedOrStringProperty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot set property &apos;{0}&apos; on object &apos;{1}&apos; because the object is a forward or incompletely initialized reference. The unresolved name(s) are: &apos;{2}&apos;. 的本地化字符串。
            /// </summary>
            internal static string AttachedPropOnFwdRefTC
            {
                get
                {
                    return ResourceManager.GetString("AttachedPropOnFwdRefTC", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 An unhandled scanner attribute was encountered. 的本地化字符串。
            /// </summary>
            internal static string AttributeUnhandledKind
            {
                get
                {
                    return ResourceManager.GetString("AttributeUnhandledKind", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 One of the InternalsVisibleToAttribute values in assembly &apos;{0}&apos; is not a valid assembly name. Use the format &apos;AssemblyShortName&apos; or &apos;AssemblyShortName, PublicKey=...&apos;. 的本地化字符串。
            /// </summary>
            internal static string BadInternalsVisibleTo1
            {
                get
                {
                    return ResourceManager.GetString("BadInternalsVisibleTo1", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 InternalsVisibleToAttribute value &apos;{0}&apos; in assembly &apos;{1}&apos; is not a valid assembly name. Use the format &apos;AssemblyShortName&apos; or &apos;AssemblyShortName, PublicKey=...&apos;. 的本地化字符串。
            /// </summary>
            internal static string BadInternalsVisibleTo2
            {
                get
                {
                    return ResourceManager.GetString("BadInternalsVisibleTo2", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Bad method &apos;{0}&apos; on &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string BadMethod
            {
                get
                {
                    return ResourceManager.GetString("BadMethod", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Bad state in ObjectWriter. Non directive missing instance. 的本地化字符串。
            /// </summary>
            internal static string BadStateObjectWriter
            {
                get
                {
                    return ResourceManager.GetString("BadStateObjectWriter", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 An XmlnsCompatibleWithAttribute in assembly &apos;{0}&apos; is missing a required property. Set both the NewNamespace and OldNamespace properties, or remove the XmlnsCompatibleWithAttribute. 的本地化字符串。
            /// </summary>
            internal static string BadXmlnsCompat
            {
                get
                {
                    return ResourceManager.GetString("BadXmlnsCompat", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 An XmlnsDefinitionAttribute in assembly &apos;{0}&apos; is missing a required property. Set both the ClrNamespace and XmlNamespace properties, or remove the XmlnsDefinitionAttribute. 的本地化字符串。
            /// </summary>
            internal static string BadXmlnsDefinition
            {
                get
                {
                    return ResourceManager.GetString("BadXmlnsDefinition", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 An XmlnsPrefixAttribute in assembly &apos;{0}&apos; is missing a required property. Set both the Prefix and XmlNamespace properties, or remove the XmlnsPrefixAttribute. 的本地化字符串。
            /// </summary>
            internal static string BadXmlnsPrefix
            {
                get
                {
                    return ResourceManager.GetString("BadXmlnsPrefix", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Builder Stack is not empty when end of XamlNode stream was reached. 的本地化字符串。
            /// </summary>
            internal static string BuilderStackNotEmptyOnClose
            {
                get
                {
                    return ResourceManager.GetString("BuilderStackNotEmptyOnClose", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Failed to check convertibility from type &apos;{0}&apos; using &apos;{1}&apos;. This generally indicates an incorrectly implemented TypeConverter. 的本地化字符串。
            /// </summary>
            internal static string CanConvertFromFailed
            {
                get
                {
                    return ResourceManager.GetString("CanConvertFromFailed", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Failed to check convertibility to type &apos;{0}&apos; using &apos;{1}&apos;. This generally indicates an incorrectly implemented TypeConverter. 的本地化字符串。
            /// </summary>
            internal static string CanConvertToFailed
            {
                get
                {
                    return ResourceManager.GetString("CanConvertToFailed", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 In markup extensions, all constructor argument values should be atoms.  For the object of type &apos;{0}&apos;, one or more argument values are not atomic. 的本地化字符串。
            /// </summary>
            internal static string CannotAddPositionalParameters
            {
                get
                {
                    return ResourceManager.GetString("CannotAddPositionalParameters", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot convert string value &apos;{0}&apos; to type &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string CannotConvertStringToType
            {
                get
                {
                    return ResourceManager.GetString("CannotConvertStringToType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot convert type &apos;{0}&apos; to &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string CannotConvertType
            {
                get
                {
                    return ResourceManager.GetString("CannotConvertType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot create an instance of &apos;{0}&apos; because XamlType is not valid. 的本地化字符串。
            /// </summary>
            internal static string CannotCreateBadEventDelegate
            {
                get
                {
                    return ResourceManager.GetString("CannotCreateBadEventDelegate", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot create an instance of &apos;{0}&apos; because XamlType is not valid. 的本地化字符串。
            /// </summary>
            internal static string CannotCreateBadType
            {
                get
                {
                    return ResourceManager.GetString("CannotCreateBadType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot find Assembly &apos;{0}&apos; in URI &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string CannotFindAssembly
            {
                get
                {
                    return ResourceManager.GetString("CannotFindAssembly", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot modify a read-only container. 的本地化字符串。
            /// </summary>
            internal static string CannotModifyReadOnlyContainer
            {
                get
                {
                    return ResourceManager.GetString("CannotModifyReadOnlyContainer", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot reassign a previously set SchemaContext. 的本地化字符串。
            /// </summary>
            internal static string CannotReassignSchemaContext
            {
                get
                {
                    return ResourceManager.GetString("CannotReassignSchemaContext", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot resolve type &apos;{0}&apos; for method &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string CannotResolveTypeForFactoryMethod
            {
                get
                {
                    return ResourceManager.GetString("CannotResolveTypeForFactoryMethod", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot get part or part information from a write-only container. 的本地化字符串。
            /// </summary>
            internal static string CannotRetrievePartsOfWriteOnlyContainer
            {
                get
                {
                    return ResourceManager.GetString("CannotRetrievePartsOfWriteOnlyContainer", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 BaseUri can only be set once at the root node (XamlXmlReader may provide a default at the root node). 的本地化字符串。
            /// </summary>
            internal static string CannotSetBaseUri
            {
                get
                {
                    return ResourceManager.GetString("CannotSetBaseUri", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot set SchemaContext on ObjectWriter. 的本地化字符串。
            /// </summary>
            internal static string CannotSetSchemaContext
            {
                get
                {
                    return ResourceManager.GetString("CannotSetSchemaContext", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot set SchemaContext to null. 的本地化字符串。
            /// </summary>
            internal static string CannotSetSchemaContextNull
            {
                get
                {
                    return ResourceManager.GetString("CannotSetSchemaContextNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot write on a closed XamlWriter. 的本地化字符串。
            /// </summary>
            internal static string CannotWriteClosedWriter
            {
                get
                {
                    return ResourceManager.GetString("CannotWriteClosedWriter", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The value &apos;{1}&apos; contains significant white space(s) but &quot;xml:space = preserve&quot; cannot be written down on the member &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string CannotWriteXmlSpacePreserveOnMember
            {
                get
                {
                    return ResourceManager.GetString("CannotWriteXmlSpacePreserveOnMember", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot assign root instance of type &apos;{0}&apos; to type &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string CantAssignRootInstance
            {
                get
                {
                    return ResourceManager.GetString("CantAssignRootInstance", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot create unknown type &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string CantCreateUnknownType
            {
                get
                {
                    return ResourceManager.GetString("CantCreateUnknownType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot get write-only property &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string CantGetWriteonlyProperty
            {
                get
                {
                    return ResourceManager.GetString("CantGetWriteonlyProperty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot set read-only property &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string CantSetReadonlyProperty
            {
                get
                {
                    return ResourceManager.GetString("CantSetReadonlyProperty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot set unknown member &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string CantSetUnknownProperty
            {
                get
                {
                    return ResourceManager.GetString("CantSetUnknownProperty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Close called while inside a deferred load section. 的本地化字符串。
            /// </summary>
            internal static string CloseInsideTemplate
            {
                get
                {
                    return ResourceManager.GetString("CloseInsideTemplate", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Must close XamlWriter before reading from XamlNodeList. 的本地化字符串。
            /// </summary>
            internal static string CloseXamlWriterBeforeReading
            {
                get
                {
                    return ResourceManager.GetString("CloseXamlWriterBeforeReading", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot add instance of type &apos;{1}&apos; to a collection of type &apos;{0}&apos;. Only items of type &apos;{2}&apos; are allowed. 的本地化字符串。
            /// </summary>
            internal static string Collection_BadType
            {
                get
                {
                    return ResourceManager.GetString("Collection_BadType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot pass multidimensional array to the CopyTo method on a collection. 的本地化字符串。
            /// </summary>
            internal static string Collection_CopyTo_ArrayCannotBeMultidimensional
            {
                get
                {
                    return ResourceManager.GetString("Collection_CopyTo_ArrayCannotBeMultidimensional", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; parameter value is equal to or greater than the length of the &apos;{1}&apos; parameter value. 的本地化字符串。
            /// </summary>
            internal static string Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength
            {
                get
                {
                    return ResourceManager.GetString("Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The number of elements in this collection is greater than the available space from &apos;{0}&apos; to the end of destination &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string Collection_CopyTo_NumberOfElementsExceedsArrayLength
            {
                get
                {
                    return ResourceManager.GetString("Collection_CopyTo_NumberOfElementsExceedsArrayLength", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Collection &apos;{0}&apos; cannot contain null values. 的本地化字符串。
            /// </summary>
            internal static string CollectionCannotContainNulls
            {
                get
                {
                    return ResourceManager.GetString("CollectionCannotContainNulls", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The number of elements in this collection must be less than or equal to &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string CollectionNumberOfElementsMustBeLessOrEqualTo
            {
                get
                {
                    return ResourceManager.GetString("CollectionNumberOfElementsMustBeLessOrEqualTo", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Failed attempting to create an Implicit Type with a constructor. 的本地化字符串。
            /// </summary>
            internal static string ConstructImplicitType
            {
                get
                {
                    return ResourceManager.GetString("ConstructImplicitType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The invocation of the constructor on type &apos;{0}&apos; that matches the specified binding constraints threw an exception. 的本地化字符串。
            /// </summary>
            internal static string ConstructorInvocation
            {
                get
                {
                    return ResourceManager.GetString("ConstructorInvocation", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot write the given positional parameters because a matching constructor was not found. 的本地化字符串。
            /// </summary>
            internal static string ConstructorNotFoundForGivenPositionalParameters
            {
                get
                {
                    return ResourceManager.GetString("ConstructorNotFoundForGivenPositionalParameters", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Converter type &apos;{0}&apos; doesn&apos;t derive from expected base type &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string ConverterMustDeriveFromBase
            {
                get
                {
                    return ResourceManager.GetString("ConverterMustDeriveFromBase", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; ValueSerializer cannot convert from &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string ConvertFromException
            {
                get
                {
                    return ResourceManager.GetString("ConvertFromException", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; ValueSerializer cannot convert &apos;{1}&apos; to &apos;{2}&apos;. 的本地化字符串。
            /// </summary>
            internal static string ConvertToException
            {
                get
                {
                    return ResourceManager.GetString("ConvertToException", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Failed to add attached properties to item in ConditionalWeakTable. 的本地化字符串。
            /// </summary>
            internal static string DefaultAttachablePropertyStoreCannotAddInstance
            {
                get
                {
                    return ResourceManager.GetString("DefaultAttachablePropertyStoreCannotAddInstance", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Deferred load threw an exception. 的本地化字符串。
            /// </summary>
            internal static string DeferredLoad
            {
                get
                {
                    return ResourceManager.GetString("DeferredLoad", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Deferred member was not collected in &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string DeferredPropertyNotCollected
            {
                get
                {
                    return ResourceManager.GetString("DeferredPropertyNotCollected", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Save of deferred-load content threw an exception. 的本地化字符串。
            /// </summary>
            internal static string DeferredSave
            {
                get
                {
                    return ResourceManager.GetString("DeferredSave", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot get a XamlDeferringLoader from XamlValueConverter &apos;{0}&apos; because its ConverterInstance property is null. 的本地化字符串。
            /// </summary>
            internal static string DeferringLoaderInstanceNull
            {
                get
                {
                    return ResourceManager.GetString("DeferringLoaderInstanceNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos;.&apos;{1}&apos; Depends on &apos;{0}&apos;.{1}&apos;, which was not set. 的本地化字符串。
            /// </summary>
            internal static string DependsOnMissing
            {
                get
                {
                    return ResourceManager.GetString("DependsOnMissing", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Dictionary of type &apos;{0}&apos; cannot add key &apos;{1}&apos;. A TypeConverter will convert the key to type &apos;{2}&apos;. To avoid seeing this error, override System.Collections.IDictionary.Add and perform the conversion there. 的本地化字符串。
            /// </summary>
            internal static string DictionaryFirstChanceException
            {
                get
                {
                    return ResourceManager.GetString("DictionaryFirstChanceException", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Directive getter is not implemented. 的本地化字符串。
            /// </summary>
            internal static string DirectiveGetter
            {
                get
                {
                    return ResourceManager.GetString("DirectiveGetter", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Directive &apos;{0}&apos; must be a value of type string. Remove this directive or change it to a string value. 的本地化字符串。
            /// </summary>
            internal static string DirectiveMustBeString
            {
                get
                {
                    return ResourceManager.GetString("DirectiveMustBeString", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Directive &apos;{0}&apos; is only allowed on the root object. Remove this directive or move it to the root of the document. 的本地化字符串。
            /// </summary>
            internal static string DirectiveNotAtRoot
            {
                get
                {
                    return ResourceManager.GetString("DirectiveNotAtRoot", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Directive &apos;{0}&apos; was not found in TargetNamespace &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string DirectiveNotFound
            {
                get
                {
                    return ResourceManager.GetString("DirectiveNotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; property has already been set on &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string DuplicateMemberSet
            {
                get
                {
                    return ResourceManager.GetString("DuplicateMemberSet", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 There is more than one XmlnsCompatibleWithAttribute in assembly &apos;{0}&apos; for OldNamespace &apos;{1}&apos;. Remove the extra attribute(s). 的本地化字符串。
            /// </summary>
            internal static string DuplicateXmlnsCompat
            {
                get
                {
                    return ResourceManager.GetString("DuplicateXmlnsCompat", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 There are conflicting XmlnsCompatibleWithAttributes in assemblies &apos;{0}&apos; and &apos;{1}&apos; for OldNamespace &apos;{2}&apos;. Change the attributes to have the same NewNamespace, or pass a non-conflicting set of Reference Assemblies in the XamlSchemaContext constructor. 的本地化字符串。
            /// </summary>
            internal static string DuplicateXmlnsCompatAcrossAssemblies
            {
                get
                {
                    return ResourceManager.GetString("DuplicateXmlnsCompatAcrossAssemblies", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; enumeration value is not valid. 的本地化字符串。
            /// </summary>
            internal static string Enum_Invalid
            {
                get
                {
                    return ResourceManager.GetString("Enum_Invalid", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 No current object to return. 的本地化字符串。
            /// </summary>
            internal static string Enumerator_VerifyContext
            {
                get
                {
                    return ResourceManager.GetString("Enumerator_VerifyContext", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; event cannot be assigned a value that is not assignable to &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string EventCannotBeAssigned
            {
                get
                {
                    return ResourceManager.GetString("EventCannotBeAssigned", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot write positional parameters in the current state.  The writer cannot write the positional parameters in attribute form because the writer has started to write elements, nor can the writer expand the positional parameters due to the lack of a default constructor on the markup extension that contains the positional parameters.  Try moving the positional parameter member earlier in the node stream, to a place where XamlXmlWriter can still write attributes. 的本地化字符串。
            /// </summary>
            internal static string ExpandPositionalParametersinTypeWithNoDefaultConstructor
            {
                get
                {
                    return ResourceManager.GetString("ExpandPositionalParametersinTypeWithNoDefaultConstructor", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot write positional parameters in the current state.  The writer cannot write the positional parameters in attribute form because the writer has started to write elements, nor can the writer expand the positional parameters since UnderlyingType on type &apos;{0}&apos; is null.  Try moving the positional parameter member earlier in the node stream, to place where XamlXmlWriter can still write attributes. 的本地化字符串。
            /// </summary>
            internal static string ExpandPositionalParametersWithoutUnderlyingType
            {
                get
                {
                    return ResourceManager.GetString("ExpandPositionalParametersWithoutUnderlyingType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot write positional parameters in the current state.  The writer cannot write the positional parameters in attribute form because the writer has started to write elements, nor can the writer expand the positional parameters since not all properties are writable.  Try moving the positional parameter member earlier in the node stream, to a place where XamlXmlWriter can still write attributes. 的本地化字符串。
            /// </summary>
            internal static string ExpandPositionalParametersWithReadOnlyProperties
            {
                get
                {
                    return ResourceManager.GetString("ExpandPositionalParametersWithReadOnlyProperties", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Expected permission of type XamlLoadPermission. 的本地化字符串。
            /// </summary>
            internal static string ExpectedLoadPermission
            {
                get
                {
                    return ResourceManager.GetString("ExpectedLoadPermission", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Expected value of type ObjectMarkupInfo. 的本地化字符串。
            /// </summary>
            internal static string ExpectedObjectMarkupInfo
            {
                get
                {
                    return ResourceManager.GetString("ExpectedObjectMarkupInfo", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Assembly name &apos;{0}&apos; is not fully qualified. The Name, Version, Culture, and PublicKeyToken must all be provided. 的本地化字符串。
            /// </summary>
            internal static string ExpectedQualifiedAssemblyName
            {
                get
                {
                    return ResourceManager.GetString("ExpectedQualifiedAssemblyName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Type name &apos;{0}&apos; is not assembly-qualified. You can obtain this value from System.Type.AssemblyQualifiedName. 的本地化字符串。
            /// </summary>
            internal static string ExpectedQualifiedTypeName
            {
                get
                {
                    return ResourceManager.GetString("ExpectedQualifiedTypeName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The factory method &apos;{0}&apos; that matches the specified binding constraints returned null. 的本地化字符串。
            /// </summary>
            internal static string FactoryReturnedNull
            {
                get
                {
                    return ResourceManager.GetString("FactoryReturnedNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Input file or data stream does not conform to the expected file format specification. 的本地化字符串。
            /// </summary>
            internal static string FileFormatException
            {
                get
                {
                    return ResourceManager.GetString("FileFormatException", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; file does not conform to the expected file format specification. 的本地化字符串。
            /// </summary>
            internal static string FileFormatExceptionWithFileName
            {
                get
                {
                    return ResourceManager.GetString("FileFormatExceptionWithFileName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Could not load file or assembly &apos;{0}&apos; or one of its dependencies. The system cannot find the specified file. 的本地化字符串。
            /// </summary>
            internal static string FileNotFoundExceptionMessage
            {
                get
                {
                    return ResourceManager.GetString("FileNotFoundExceptionMessage", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Attempt to reference named object(s) &apos;{0}&apos; which have not yet been defined. Forward references, or references to objects that contain forward references, are not supported on directives other than Key. 的本地化字符串。
            /// </summary>
            internal static string ForwardRefDirectives
            {
                get
                {
                    return ResourceManager.GetString("ForwardRefDirectives", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Specified value of type &apos;{0}&apos; must have IsFrozen set to false to modify. 的本地化字符串。
            /// </summary>
            internal static string Freezable_CantBeFrozen
            {
                get
                {
                    return ResourceManager.GetString("Freezable_CantBeFrozen", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot promote from Array. 的本地化字符串。
            /// </summary>
            internal static string FrugalList_CannotPromoteBeyondArray
            {
                get
                {
                    return ResourceManager.GetString("FrugalList_CannotPromoteBeyondArray", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot promote from &apos;{0}&apos; to &apos;{1}&apos; because the target map is too small. 的本地化字符串。
            /// </summary>
            internal static string FrugalList_TargetMapCannotHoldAllData
            {
                get
                {
                    return ResourceManager.GetString("FrugalList_TargetMapCannotHoldAllData", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Getting instance of &apos;{0}&apos; threw an exception. 的本地化字符串。
            /// </summary>
            internal static string GetConverterInstance
            {
                get
                {
                    return ResourceManager.GetString("GetConverterInstance", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Retrieving items in collection or dictionary of type &apos;{0}&apos; threw an exception. 的本地化字符串。
            /// </summary>
            internal static string GetItemsException
            {
                get
                {
                    return ResourceManager.GetString("GetItemsException", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XamlTypeInvoker.GetItems returned null for type &apos;{0}&apos;. This generally indicates an incorrectly implemented collection type. 的本地化字符串。
            /// </summary>
            internal static string GetItemsReturnedNull
            {
                get
                {
                    return ResourceManager.GetString("GetItemsReturnedNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Collection property &apos;{0}&apos;.&apos;{1}&apos; is null. 的本地化字符串。
            /// </summary>
            internal static string GetObjectNull
            {
                get
                {
                    return ResourceManager.GetString("GetObjectNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot get TargetType on a non-attachable Member. 的本地化字符串。
            /// </summary>
            internal static string GetTargetTypeOnNonAttachableMember
            {
                get
                {
                    return ResourceManager.GetString("GetTargetTypeOnNonAttachableMember", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Either getter or setter must be non-null. 的本地化字符串。
            /// </summary>
            internal static string GetterOrSetterRequired
            {
                get
                {
                    return ResourceManager.GetString("GetterOrSetterRequired", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Get property &apos;{0}&apos; threw an exception. 的本地化字符串。
            /// </summary>
            internal static string GetValue
            {
                get
                {
                    return ResourceManager.GetString("GetValue", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Attached property getter methods must have one parameter and a non-void return type. 的本地化字符串。
            /// </summary>
            internal static string IncorrectGetterParamNum
            {
                get
                {
                    return ResourceManager.GetString("IncorrectGetterParamNum", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Attached property setter and attached event adder methods must have two parameters. 的本地化字符串。
            /// </summary>
            internal static string IncorrectSetterParamNum
            {
                get
                {
                    return ResourceManager.GetString("IncorrectSetterParamNum", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Initialization of &apos;{0}&apos; threw an exception. 的本地化字符串。
            /// </summary>
            internal static string InitializationGuard
            {
                get
                {
                    return ResourceManager.GetString("InitializationGuard", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Type &apos;{0}&apos; cannot be initialized from text (XamlLanguage.Initialization).  Add a TypeConverter to this type or change the XAML to use a constructor or factory method. 的本地化字符串。
            /// </summary>
            internal static string InitializationSyntaxWithoutTypeConverter
            {
                get
                {
                    return ResourceManager.GetString("InitializationSyntaxWithoutTypeConverter", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Character &apos;{0}&apos; was unexpected in string &apos;{1}&apos;.  Invalid XAML type name. 的本地化字符串。
            /// </summary>
            internal static string InvalidCharInTypeName
            {
                get
                {
                    return ResourceManager.GetString("InvalidCharInTypeName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Encountered a closing BracketCharacter &apos;{0}&apos; without a corresponding opening BracketCharacter. 的本地化字符串。
            /// </summary>
            internal static string InvalidClosingBracketCharacers
            {
                get
                {
                    return ResourceManager.GetString("InvalidClosingBracketCharacers", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Event argument is invalid. 的本地化字符串。
            /// </summary>
            internal static string InvalidEvent
            {
                get
                {
                    return ResourceManager.GetString("InvalidEvent", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Invalid expression: &apos;{0}&apos; 的本地化字符串。
            /// </summary>
            internal static string InvalidExpression
            {
                get
                {
                    return ResourceManager.GetString("InvalidExpression", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 PermissionState value &apos;{0}&apos; is not valid for this Permission. 的本地化字符串。
            /// </summary>
            internal static string InvalidPermissionStateValue
            {
                get
                {
                    return ResourceManager.GetString("InvalidPermissionStateValue", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Permission type is not valid. Expected &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string InvalidPermissionType
            {
                get
                {
                    return ResourceManager.GetString("InvalidPermissionType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Type argument &apos;{0}&apos; is not a valid type. 的本地化字符串。
            /// </summary>
            internal static string InvalidTypeArgument
            {
                get
                {
                    return ResourceManager.GetString("InvalidTypeArgument", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The string &apos;{0}&apos; is not a valid XAML type name list. Type name lists are comma-delimited lists of types; such as &apos;x:String, x:Int32&apos;. 的本地化字符串。
            /// </summary>
            internal static string InvalidTypeListString
            {
                get
                {
                    return ResourceManager.GetString("InvalidTypeListString", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The string &apos;{0}&apos; is not a valid XAML type name. Type names contain an optional prefix, a name, and optional type arguments; such as &apos;String&apos;, &apos;x:Int32&apos;, &apos;g:Dictionary(x:String,x:Int32)&apos;. 的本地化字符串。
            /// </summary>
            internal static string InvalidTypeString
            {
                get
                {
                    return ResourceManager.GetString("InvalidTypeString", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; is not a valid XAML member name. 的本地化字符串。
            /// </summary>
            internal static string InvalidXamlMemberName
            {
                get
                {
                    return ResourceManager.GetString("InvalidXamlMemberName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Construction directive &apos;{0}&apos; must be an attribute or the first property element. 的本地化字符串。
            /// </summary>
            internal static string LateConstructionDirective
            {
                get
                {
                    return ResourceManager.GetString("LateConstructionDirective", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; Line number &apos;{1}&apos; and line position &apos;{2}&apos;. 的本地化字符串。
            /// </summary>
            internal static string LineNumberAndPosition
            {
                get
                {
                    return ResourceManager.GetString("LineNumberAndPosition", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; Line number &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string LineNumberOnly
            {
                get
                {
                    return ResourceManager.GetString("LineNumberOnly", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 List collection is not an IList. 的本地化字符串。
            /// </summary>
            internal static string ListNotIList
            {
                get
                {
                    return ResourceManager.GetString("ListNotIList", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 BracketCharacter &apos;{0}&apos; does not have a corresponding opening/closing BracketCharacter. 的本地化字符串。
            /// </summary>
            internal static string MalformedBracketCharacters
            {
                get
                {
                    return ResourceManager.GetString("MalformedBracketCharacters", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot parse the malformed property name &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string MalformedPropertyName
            {
                get
                {
                    return ResourceManager.GetString("MalformedPropertyName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Items in the array must be type &apos;{0}&apos;. One or more items cannot be cast to this type. 的本地化字符串。
            /// </summary>
            internal static string MarkupExtensionArrayBadType
            {
                get
                {
                    return ResourceManager.GetString("MarkupExtensionArrayBadType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Must set Type before calling ProvideValue on ArrayExtension. 的本地化字符串。
            /// </summary>
            internal static string MarkupExtensionArrayType
            {
                get
                {
                    return ResourceManager.GetString("MarkupExtensionArrayType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; StaticExtension value cannot be resolved to an enumeration, static field, or static property. 的本地化字符串。
            /// </summary>
            internal static string MarkupExtensionBadStatic
            {
                get
                {
                    return ResourceManager.GetString("MarkupExtensionBadStatic", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Markup extension &apos;{0}&apos; requires &apos;{1}&apos; be implemented in the IServiceProvider for ProvideValue. 的本地化字符串。
            /// </summary>
            internal static string MarkupExtensionNoContext
            {
                get
                {
                    return ResourceManager.GetString("MarkupExtensionNoContext", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 StaticExtension must have Member property set before ProvideValue can be called. 的本地化字符串。
            /// </summary>
            internal static string MarkupExtensionStaticMember
            {
                get
                {
                    return ResourceManager.GetString("MarkupExtensionStaticMember", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 TypeExtension must have TypeName property set before ProvideValue can be called. 的本地化字符串。
            /// </summary>
            internal static string MarkupExtensionTypeName
            {
                get
                {
                    return ResourceManager.GetString("MarkupExtensionTypeName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; string is not valid for type. 的本地化字符串。
            /// </summary>
            internal static string MarkupExtensionTypeNameBad
            {
                get
                {
                    return ResourceManager.GetString("MarkupExtensionTypeNameBad", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot determine the positional parameters for type &apos;{0}&apos; because it has more than one constructor overload with &apos;{1}&apos; parameters. To make this markup extension usable in XAML, remove the duplicate constructor overload(s) or set XamlSchemaContextSettings.SupportMarkupExtensionsWithDuplicateArity to true. 的本地化字符串。
            /// </summary>
            internal static string MarkupExtensionWithDuplicateArity
            {
                get
                {
                    return ResourceManager.GetString("MarkupExtensionWithDuplicateArity", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The name of the member &apos;{0}&apos; contains characters that are invalid in XAML. 的本地化字符串。
            /// </summary>
            internal static string MemberHasInvalidXamlName
            {
                get
                {
                    return ResourceManager.GetString("MemberHasInvalidXamlName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Member &apos;{0}&apos; on type &apos;{1}&apos; is internal. 的本地化字符串。
            /// </summary>
            internal static string MemberIsInternal
            {
                get
                {
                    return ResourceManager.GetString("MemberIsInternal", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The invocation of a method &apos;{0}&apos; that matches the specified binding constraints threw an exception. 的本地化字符串。
            /// </summary>
            internal static string MethodInvocation
            {
                get
                {
                    return ResourceManager.GetString("MethodInvocation", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 No local assembly provided to complete URI=&apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string MissingAssemblyName
            {
                get
                {
                    return ResourceManager.GetString("MissingAssemblyName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Missing case &apos;{0}&apos; in DeferringWriter&apos;{1}&apos; method. 的本地化字符串。
            /// </summary>
            internal static string MissingCase
            {
                get
                {
                    return ResourceManager.GetString("MissingCase", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Missing case in Default processing of XamlNodes. 的本地化字符串。
            /// </summary>
            internal static string MissingCaseXamlNodes
            {
                get
                {
                    return ResourceManager.GetString("MissingCaseXamlNodes", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unexpected equals sign &apos;=&apos; following &apos;{0}&apos;. Check for a missing comma separator. 的本地化字符串。
            /// </summary>
            internal static string MissingComma1
            {
                get
                {
                    return ResourceManager.GetString("MissingComma1", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unexpected equals sign &apos;=&apos; following &apos;{0}&apos;=&apos;{1}&apos;. Check for a missing comma separator. 的本地化字符串。
            /// </summary>
            internal static string MissingComma2
            {
                get
                {
                    return ResourceManager.GetString("MissingComma2", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Missing implicit property case. 的本地化字符串。
            /// </summary>
            internal static string MissingImplicitProperty
            {
                get
                {
                    return ResourceManager.GetString("MissingImplicitProperty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Missing case for ImplicitPropertyType. 的本地化字符串。
            /// </summary>
            internal static string MissingImplicitPropertyTypeCase
            {
                get
                {
                    return ResourceManager.GetString("MissingImplicitPropertyTypeCase", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Missing key value on &apos;{0}&apos; object. 的本地化字符串。
            /// </summary>
            internal static string MissingKey
            {
                get
                {
                    return ResourceManager.GetString("MissingKey", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Missing case handler in LookupPropertyBit. 的本地化字符串。
            /// </summary>
            internal static string MissingLookPropertyBit
            {
                get
                {
                    return ResourceManager.GetString("MissingLookPropertyBit", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Service provider is missing the IXamlNameProvider service. 的本地化字符串。
            /// </summary>
            internal static string MissingNameProvider
            {
                get
                {
                    return ResourceManager.GetString("MissingNameProvider", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Service provider is missing the INameResolver service. 的本地化字符串。
            /// </summary>
            internal static string MissingNameResolver
            {
                get
                {
                    return ResourceManager.GetString("MissingNameResolver", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Missing case in ClrType &apos;Member&apos; lookup. 的本地化字符串。
            /// </summary>
            internal static string MissingPropertyCaseClrType
            {
                get
                {
                    return ResourceManager.GetString("MissingPropertyCaseClrType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Missing &apos;{0}&apos; in URI &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string MissingTagInNamespace
            {
                get
                {
                    return ResourceManager.GetString("MissingTagInNamespace", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Creating from text without a TypeConverter is not allowed. 的本地化字符串。
            /// </summary>
            internal static string MissingTypeConverter
            {
                get
                {
                    return ResourceManager.GetString("MissingTypeConverter", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; must be of type &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string MustBeOfType
            {
                get
                {
                    return ResourceManager.GetString("MustBeOfType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Reference must have a Name to resolve. 的本地化字符串。
            /// </summary>
            internal static string MustHaveName
            {
                get
                {
                    return ResourceManager.GetString("MustHaveName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 This setter is not intended to be used directly from your code. Do not call this setter. 的本地化字符串。
            /// </summary>
            internal static string MustNotCallSetter
            {
                get
                {
                    return ResourceManager.GetString("MustNotCallSetter", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Name resolution failure. &apos;{0}&apos; was not found. 的本地化字符串。
            /// </summary>
            internal static string NameNotFound
            {
                get
                {
                    return ResourceManager.GetString("NameNotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot register duplicate name &apos;{0}&apos; in this scope. 的本地化字符串。
            /// </summary>
            internal static string NameScopeDuplicateNamesNotAllowed
            {
                get
                {
                    return ResourceManager.GetString("NameScopeDuplicateNamesNotAllowed", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Could not register named object. {0} 的本地化字符串。
            /// </summary>
            internal static string NameScopeException
            {
                get
                {
                    return ResourceManager.GetString("NameScopeException", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; name is not valid for identifier. 的本地化字符串。
            /// </summary>
            internal static string NameScopeInvalidIdentifierName
            {
                get
                {
                    return ResourceManager.GetString("NameScopeInvalidIdentifierName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Name cannot be an empty string. 的本地化字符串。
            /// </summary>
            internal static string NameScopeNameNotEmptyString
            {
                get
                {
                    return ResourceManager.GetString("NameScopeNameNotEmptyString", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Name &apos;{0}&apos; was not found. 的本地化字符串。
            /// </summary>
            internal static string NameScopeNameNotFound
            {
                get
                {
                    return ResourceManager.GetString("NameScopeNameNotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot attach NameScope to null root instance. 的本地化字符串。
            /// </summary>
            internal static string NameScopeOnRootInstance
            {
                get
                {
                    return ResourceManager.GetString("NameScopeOnRootInstance", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The prefix &apos;xml&apos; is reserved. 的本地化字符串。
            /// </summary>
            internal static string NamespaceDeclarationCannotBeXml
            {
                get
                {
                    return ResourceManager.GetString("NamespaceDeclarationCannotBeXml", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 NamespaceDeclaration.Namespace cannot be null.  Provide a value for this property. 的本地化字符串。
            /// </summary>
            internal static string NamespaceDeclarationNamespaceCannotBeNull
            {
                get
                {
                    return ResourceManager.GetString("NamespaceDeclarationNamespaceCannotBeNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 NamespaceDeclaration.Prefix cannot be null.  Provide a value for this property. 的本地化字符串。
            /// </summary>
            internal static string NamespaceDeclarationPrefixCannotBeNull
            {
                get
                {
                    return ResourceManager.GetString("NamespaceDeclarationPrefixCannotBeNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Namespace &apos;{0}&apos; was not found in scope. 的本地化字符串。
            /// </summary>
            internal static string NamespaceNotFound
            {
                get
                {
                    return ResourceManager.GetString("NamespaceNotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 No Add methods found on type &apos;{0}&apos; for a value of type &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string NoAddMethodFound
            {
                get
                {
                    return ResourceManager.GetString("NoAddMethodFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; is not allowed in attribute usage. 的本地化字符串。
            /// </summary>
            internal static string NoAttributeUsage
            {
                get
                {
                    return ResourceManager.GetString("NoAttributeUsage", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 No matching constructor found on type &apos;{0}&apos;. You can use the Arguments or FactoryMethod directives to construct this type. 的本地化字符串。
            /// </summary>
            internal static string NoConstructor
            {
                get
                {
                    return ResourceManager.GetString("NoConstructor", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 A Constructor for &apos;{0}&apos; with &apos;{1}&apos; arguments was not found. 的本地化字符串。
            /// </summary>
            internal static string NoConstructorWithNArugments
            {
                get
                {
                    return ResourceManager.GetString("NoConstructorWithNArugments", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 No default constructor found for type &apos;{0}&apos;. You can use the Arguments or FactoryMethod directives to construct this type. 的本地化字符串。
            /// </summary>
            internal static string NoDefaultConstructor
            {
                get
                {
                    return ResourceManager.GetString("NoDefaultConstructor", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; is not allowed in element usage. 的本地化字符串。
            /// </summary>
            internal static string NoElementUsage
            {
                get
                {
                    return ResourceManager.GetString("NoElementUsage", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Type with positional parameters is not a markup extension. 的本地化字符串。
            /// </summary>
            internal static string NonMEWithPositionalParameters
            {
                get
                {
                    return ResourceManager.GetString("NonMEWithPositionalParameters", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: Missing StartMember on Type &apos;{0}&apos; before EndMember. 的本地化字符串。
            /// </summary>
            internal static string NoPropertyInCurrentFrame_EM
            {
                get
                {
                    return ResourceManager.GetString("NoPropertyInCurrentFrame_EM", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: EndMember must follow StartObject and StartMember. 的本地化字符串。
            /// </summary>
            internal static string NoPropertyInCurrentFrame_EM_noType
            {
                get
                {
                    return ResourceManager.GetString("NoPropertyInCurrentFrame_EM_noType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: GetObject requires a StartMember after StartObject &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string NoPropertyInCurrentFrame_GO
            {
                get
                {
                    return ResourceManager.GetString("NoPropertyInCurrentFrame_GO", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: GetObject must follow a StartObject and StartMember. 的本地化字符串。
            /// </summary>
            internal static string NoPropertyInCurrentFrame_GO_noType
            {
                get
                {
                    return ResourceManager.GetString("NoPropertyInCurrentFrame_GO_noType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: &apos;{0}&apos;=&apos;{1}&apos; Namespace Declaration requires a StartMember after StartObject &apos;{2}&apos;. 的本地化字符串。
            /// </summary>
            internal static string NoPropertyInCurrentFrame_NS
            {
                get
                {
                    return ResourceManager.GetString("NoPropertyInCurrentFrame_NS", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: StartObject &apos;{0}&apos; requires a StartMember after StartObject &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string NoPropertyInCurrentFrame_SO
            {
                get
                {
                    return ResourceManager.GetString("NoPropertyInCurrentFrame_SO", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: Value of &apos;{0}&apos; requires a StartMember after StartObject &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string NoPropertyInCurrentFrame_V
            {
                get
                {
                    return ResourceManager.GetString("NoPropertyInCurrentFrame_V", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: Value of &apos;{0}&apos; must follow a StartObject and StartMember. 的本地化字符串。
            /// </summary>
            internal static string NoPropertyInCurrentFrame_V_noType
            {
                get
                {
                    return ResourceManager.GetString("NoPropertyInCurrentFrame_V_noType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 No constructor with &apos;{0}&apos; arguments for &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string NoSuchConstructor
            {
                get
                {
                    return ResourceManager.GetString("NoSuchConstructor", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos;.&apos;{1}&apos; is not an ambient property. 的本地化字符串。
            /// </summary>
            internal static string NotAmbientProperty
            {
                get
                {
                    return ResourceManager.GetString("NotAmbientProperty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; is not an ambient type. 的本地化字符串。
            /// </summary>
            internal static string NotAmbientType
            {
                get
                {
                    return ResourceManager.GetString("NotAmbientType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The type &apos;{0}&apos; is not assignable from the type &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string NotAssignableFrom
            {
                get
                {
                    return ResourceManager.GetString("NotAssignableFrom", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 [&apos;{0}&apos;(&apos;{1}&apos;)]5D; on &apos;{2}&apos; is not a property declared on this type. 的本地化字符串。
            /// </summary>
            internal static string NotDeclaringTypeAttributeProperty
            {
                get
                {
                    return ResourceManager.GetString("NotDeclaringTypeAttributeProperty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 This operation is not supported on directive members. 的本地化字符串。
            /// </summary>
            internal static string NotSupportedOnDirective
            {
                get
                {
                    return ResourceManager.GetString("NotSupportedOnDirective", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 This operation is not supported on unknown members. 的本地化字符串。
            /// </summary>
            internal static string NotSupportedOnUnknownMember
            {
                get
                {
                    return ResourceManager.GetString("NotSupportedOnUnknownMember", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 This operation is not supported on unknown types. 的本地化字符串。
            /// </summary>
            internal static string NotSupportedOnUnknownType
            {
                get
                {
                    return ResourceManager.GetString("NotSupportedOnUnknownType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: Missing CurrentObject before EndObject. 的本地化字符串。
            /// </summary>
            internal static string NoTypeInCurrentFrame_EO
            {
                get
                {
                    return ResourceManager.GetString("NoTypeInCurrentFrame_EO", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: Missing StartObject before StartMember &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string NoTypeInCurrentFrame_SM
            {
                get
                {
                    return ResourceManager.GetString("NoTypeInCurrentFrame_SM", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Argument should be a Type Converter, Markup Extension or Null. 的本地化字符串。
            /// </summary>
            internal static string ObjectNotTcOrMe
            {
                get
                {
                    return ResourceManager.GetString("ObjectNotTcOrMe", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Type &apos;{0}&apos; not visible. If the type is local, please set the LocalAssembly field in XamlReaderSettings. 的本地化字符串。
            /// </summary>
            internal static string ObjectReader_TypeNotVisible
            {
                get
                {
                    return ResourceManager.GetString("ObjectReader_TypeNotVisible", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unable to find an attachable property named &apos;{0}&apos; on type &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderAttachedPropertyNotFound
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderAttachedPropertyNotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unable to locate MemberMarkupInfo.DictionaryEntriesFromGeneric method. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderDictionaryMethod1NotFound
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderDictionaryMethod1NotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 InstanceDescriptor did not provide the correct number of arguments. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderInstanceDescriptorIncompatibleArguments
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderInstanceDescriptorIncompatibleArguments", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 InstanceDescriptor provided an argument of type &apos;{0}&apos; where a parameter of type &apos;{1}&apos; was expected. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderInstanceDescriptorIncompatibleArgumentTypes
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderInstanceDescriptorIncompatibleArgumentTypes", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 InstanceDescriptor did not provide a valid constructor or method. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderInstanceDescriptorInvalidMethod
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderInstanceDescriptorInvalidMethod", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Multidimensional arrays not supported. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderMultidimensionalArrayNotSupported
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderMultidimensionalArrayNotSupported", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unable to serialize type &apos;{0}&apos;.  Verify that the type is public and either has a default constructor or an instance descriptor. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderNoDefaultConstructor
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderNoDefaultConstructor", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unable to find a suitable constructor for the specified constructor arguments on type &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderNoMatchingConstructor
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderNoMatchingConstructor", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unable to read objects of the type �{0}� because there are no accessible constructors. To allow this type to be used in XAML, add a default constructor, use ConstructorArgumentAttribute, or provide an InstanceDescriptor. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderTypeCannotRoundtrip
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderTypeCannotRoundtrip", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unable to read objects of the type &apos;{0}&apos;.  Nested types are not supported. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderTypeIsNested
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderTypeIsNested", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; blocked the use of type &apos;{1}&apos; in XAML. If you want to serialize this type, change &apos;{0}&apos;.GetXamlType to return a non-null value for this type, or pass a different value in the schemaContext parameter of the XamlObjectReader constructor. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderTypeNotAllowed
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderTypeNotAllowed", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 An element with the name &apos;{0}&apos; has already been registered in this scope. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderXamlNamedElementAlreadyRegistered
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderXamlNamedElementAlreadyRegistered", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The name property &apos;{0}&apos; on type &apos;{1}&apos; must be of type System.String. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderXamlNamePropertyMustBeString
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderXamlNamePropertyMustBeString", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The object graph contains multiple references to an instance of type &apos;{0}&apos; and the serializer cannot find a commonly visible location to write the instance. You should examine your use of name scopes. 的本地化字符串。
            /// </summary>
            internal static string ObjectReaderXamlNameScopeResultsInClonedObject
            {
                get
                {
                    return ResourceManager.GetString("ObjectReaderXamlNameScopeResultsInClonedObject", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; blocked the use of type &apos;{1}&apos; in XAML. If you want to load this type, change &apos;{0}&apos;.GetXamlType to return a non-null value for this type, or pass a different value in the schemaContext parameter of the XamlObjectWriter constructor. 的本地化字符串。
            /// </summary>
            internal static string ObjectWriterTypeNotAllowed
            {
                get
                {
                    return ResourceManager.GetString("ObjectWriterTypeNotAllowed", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 This operation is only supported on collection types. 的本地化字符串。
            /// </summary>
            internal static string OnlySupportedOnCollections
            {
                get
                {
                    return ResourceManager.GetString("OnlySupportedOnCollections", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 This operation is only supported on collection and dictionary types. 的本地化字符串。
            /// </summary>
            internal static string OnlySupportedOnCollectionsAndDictionaries
            {
                get
                {
                    return ResourceManager.GetString("OnlySupportedOnCollectionsAndDictionaries", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 This operation is only supported on dictionary types. 的本地化字符串。
            /// </summary>
            internal static string OnlySupportedOnDictionaries
            {
                get
                {
                    return ResourceManager.GetString("OnlySupportedOnDictionaries", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: Missing EndMember for &apos;{0}.{1}&apos; before EndObject. 的本地化字符串。
            /// </summary>
            internal static string OpenPropertyInCurrentFrame_EO
            {
                get
                {
                    return ResourceManager.GetString("OpenPropertyInCurrentFrame_EO", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: Missing EndMember for &apos;{0}.{1}&apos; before StartMember &apos;{2}&apos;. 的本地化字符串。
            /// </summary>
            internal static string OpenPropertyInCurrentFrame_SM
            {
                get
                {
                    return ResourceManager.GetString("OpenPropertyInCurrentFrame_SM", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Parameter must be greater than or equal to zero. 的本地化字符串。
            /// </summary>
            internal static string ParameterCannotBeNegative
            {
                get
                {
                    return ResourceManager.GetString("ParameterCannotBeNegative", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The property element &apos;{0}&apos; is not contained by an object element. 的本地化字符串。
            /// </summary>
            internal static string ParentlessPropertyElement
            {
                get
                {
                    return ResourceManager.GetString("ParentlessPropertyElement", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot load assembly &apos;{0}&apos; because a different version of that same assembly is loaded &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string ParserAssemblyLoadVersionMismatch
            {
                get
                {
                    return ResourceManager.GetString("ParserAssemblyLoadVersionMismatch", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Too many attributes are specified for &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string ParserAttributeArgsHigh
            {
                get
                {
                    return ResourceManager.GetString("ParserAttributeArgsHigh", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; requires more attributes. 的本地化字符串。
            /// </summary>
            internal static string ParserAttributeArgsLow
            {
                get
                {
                    return ResourceManager.GetString("ParserAttributeArgsLow", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 GetPositionalParameters returned the wrong length vector. 的本地化字符串。
            /// </summary>
            internal static string PositionalParamsWrongLength
            {
                get
                {
                    return ResourceManager.GetString("PositionalParamsWrongLength", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Prefix &apos;{0}&apos; does not map to a namespace. 的本地化字符串。
            /// </summary>
            internal static string PrefixNotFound
            {
                get
                {
                    return ResourceManager.GetString("PrefixNotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The prefix &apos;{0}&apos; could not be found. 的本地化字符串。
            /// </summary>
            internal static string PrefixNotInFrames
            {
                get
                {
                    return ResourceManager.GetString("PrefixNotInFrames", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; property on &apos;{1}&apos; does not allow you to specify text. 的本地化字符串。
            /// </summary>
            internal static string PropertyDoesNotTakeText
            {
                get
                {
                    return ResourceManager.GetString("PropertyDoesNotTakeText", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; is not implemented. 的本地化字符串。
            /// </summary>
            internal static string PropertyNotImplemented
            {
                get
                {
                    return ResourceManager.GetString("PropertyNotImplemented", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Provide value on &apos;{0}&apos; threw an exception. 的本地化字符串。
            /// </summary>
            internal static string ProvideValue
            {
                get
                {
                    return ResourceManager.GetString("ProvideValue", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot call MarkupExtension.ProvideValue because of a cyclical dependency. Properties inside a MarkupExtension cannot reference objects that reference the result of the MarkupExtension. The affected MarkupExtensions are: 的本地化字符串。
            /// </summary>
            internal static string ProvideValueCycle
            {
                get
                {
                    return ResourceManager.GetString("ProvideValueCycle", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; type name does not have the expected format &apos;className, assembly&apos;. 的本地化字符串。
            /// </summary>
            internal static string QualifiedNameHasWrongFormat
            {
                get
                {
                    return ResourceManager.GetString("QualifiedNameHasWrongFormat", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Quote characters &apos; or &quot; are only allowed at the start of values. 的本地化字符串。
            /// </summary>
            internal static string QuoteCharactersOutOfPlace
            {
                get
                {
                    return ResourceManager.GetString("QuoteCharactersOutOfPlace", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Value cannot be null. Object reference: &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string ReferenceIsNull
            {
                get
                {
                    return ResourceManager.GetString("ReferenceIsNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 schemaContext parameter cannot be different from savedContext.SchemaContext 的本地化字符串。
            /// </summary>
            internal static string SavedContextSchemaContextMismatch
            {
                get
                {
                    return ResourceManager.GetString("SavedContextSchemaContextMismatch", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 savedContext.SchemaContext cannot be null 的本地化字符串。
            /// </summary>
            internal static string SavedContextSchemaContextNull
            {
                get
                {
                    return ResourceManager.GetString("SavedContextSchemaContextNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 SchemaContext on writer must be initialized before accessing the reader. 的本地化字符串。
            /// </summary>
            internal static string SchemaContextNotInitialized
            {
                get
                {
                    return ResourceManager.GetString("SchemaContextNotInitialized", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 SchemaContext cannot be null. 的本地化字符串。
            /// </summary>
            internal static string SchemaContextNull
            {
                get
                {
                    return ResourceManager.GetString("SchemaContextNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot set SandboxExternalContent to true in partial trust. 的本地化字符串。
            /// </summary>
            internal static string SecurityExceptionForSettingSandboxExternalToTrue
            {
                get
                {
                    return ResourceManager.GetString("SecurityExceptionForSettingSandboxExternalToTrue", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Invalid security XML. Missing expected attribute &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string SecurityXmlMissingAttribute
            {
                get
                {
                    return ResourceManager.GetString("SecurityXmlMissingAttribute", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Invalid security XML. Unexpected tag &apos;{0}&apos;, expected &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string SecurityXmlUnexpectedTag
            {
                get
                {
                    return ResourceManager.GetString("SecurityXmlUnexpectedTag", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Invalid security XML. Unexpected value &apos;{0}&apos; in attribute &apos;{1}&apos;, expected &apos;{2}&apos;. 的本地化字符串。
            /// </summary>
            internal static string SecurityXmlUnexpectedValue
            {
                get
                {
                    return ResourceManager.GetString("SecurityXmlUnexpectedValue", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 This serviceType is already registered to another service. 的本地化字符串。
            /// </summary>
            internal static string ServiceTypeAlreadyAdded
            {
                get
                {
                    return ResourceManager.GetString("ServiceTypeAlreadyAdded", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Set connectionId threw an exception. 的本地化字符串。
            /// </summary>
            internal static string SetConnectionId
            {
                get
                {
                    return ResourceManager.GetString("SetConnectionId", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos;.&apos;{1}&apos; is a property without a getter and is not a valid XAML property. 的本地化字符串。
            /// </summary>
            internal static string SetOnlyProperty
            {
                get
                {
                    return ResourceManager.GetString("SetOnlyProperty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot set TargetType on a non-attachable Member. 的本地化字符串。
            /// </summary>
            internal static string SetTargetTypeOnNonAttachableMember
            {
                get
                {
                    return ResourceManager.GetString("SetTargetTypeOnNonAttachableMember", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Setting properties is not allowed on a type converted instance. Property = &apos;{0}&apos; 的本地化字符串。
            /// </summary>
            internal static string SettingPropertiesIsNotAllowed
            {
                get
                {
                    return ResourceManager.GetString("SettingPropertiesIsNotAllowed", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Setting xml:base on &apos;{0}&apos; threw an exception. 的本地化字符串。
            /// </summary>
            internal static string SetUriBase
            {
                get
                {
                    return ResourceManager.GetString("SetUriBase", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Set property &apos;{0}&apos; threw an exception. 的本地化字符串。
            /// </summary>
            internal static string SetValue
            {
                get
                {
                    return ResourceManager.GetString("SetValue", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Setting xml instance on &apos;{0}&apos; threw an exception. 的本地化字符串。
            /// </summary>
            internal static string SetXmlInstance
            {
                get
                {
                    return ResourceManager.GetString("SetXmlInstance", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Method &apos;{0}&apos; is not supported by default. It can be implemented in derived classes. 的本地化字符串。
            /// </summary>
            internal static string ShouldOverrideMethod
            {
                get
                {
                    return ResourceManager.GetString("ShouldOverrideMethod", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 ShouldSerialize check failed for member &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string ShouldSerializeFailed
            {
                get
                {
                    return ResourceManager.GetString("ShouldSerializeFailed", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Directly Assignable Fixups must only have one name. 的本地化字符串。
            /// </summary>
            internal static string SimpleFixupsMustHaveOneName
            {
                get
                {
                    return ResourceManager.GetString("SimpleFixupsMustHaveOneName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Parameter cannot be a zero-length string. 的本地化字符串。
            /// </summary>
            internal static string StringEmpty
            {
                get
                {
                    return ResourceManager.GetString("StringEmpty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The string is null or empty. 的本地化字符串。
            /// </summary>
            internal static string StringIsNullOrEmpty
            {
                get
                {
                    return ResourceManager.GetString("StringIsNullOrEmpty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Deferred load section was not collected in &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string TemplateNotCollected
            {
                get
                {
                    return ResourceManager.GetString("TemplateNotCollected", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Thread is already started. 的本地化字符串。
            /// </summary>
            internal static string ThreadAlreadyStarted
            {
                get
                {
                    return ResourceManager.GetString("ThreadAlreadyStarted", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Empty token encountered at position {0} while parsing &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string TokenizerHelperEmptyToken
            {
                get
                {
                    return ResourceManager.GetString("TokenizerHelperEmptyToken", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Extra data encountered at position {0} while parsing &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string TokenizerHelperExtraDataEncountered
            {
                get
                {
                    return ResourceManager.GetString("TokenizerHelperExtraDataEncountered", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Missing end quote encountered while parsing &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string TokenizerHelperMissingEndQuote
            {
                get
                {
                    return ResourceManager.GetString("TokenizerHelperMissingEndQuote", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Premature string termination encountered while parsing &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string TokenizerHelperPrematureStringTermination
            {
                get
                {
                    return ResourceManager.GetString("TokenizerHelperPrematureStringTermination", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Error with member &apos;{0}&apos;.&apos;{1}&apos;.  It has more than one &apos;{2}&apos;. 的本地化字符串。
            /// </summary>
            internal static string TooManyAttributes
            {
                get
                {
                    return ResourceManager.GetString("TooManyAttributes", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Error with type &apos;{0}&apos;.  It has more than one &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string TooManyAttributesOnType
            {
                get
                {
                    return ResourceManager.GetString("TooManyAttributesOnType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Only one TypeConverter attribute is allowed on a type. 的本地化字符串。
            /// </summary>
            internal static string TooManyTypeConverterAttributes
            {
                get
                {
                    return ResourceManager.GetString("TooManyTypeConverterAttributes", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 (null) 的本地化字符串。
            /// </summary>
            internal static string ToStringNull
            {
                get
                {
                    return ResourceManager.GetString("ToStringNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Object &apos;{0}&apos; assigned to directive &apos;{1}&apos; has properties which are references to named object(s) &apos;{2}&apos; which have not yet been defined. Forward references, or references to objects that contain forward references, are not supported inside object construction directives. 的本地化字符串。
            /// </summary>
            internal static string TransitiveForwardRefDirectives
            {
                get
                {
                    return ResourceManager.GetString("TransitiveForwardRefDirectives", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Failed to create a &apos;{0}&apos; from the text &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string TypeConverterFailed
            {
                get
                {
                    return ResourceManager.GetString("TypeConverterFailed", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Failed to convert &apos;{0}&apos; to type &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string TypeConverterFailed2
            {
                get
                {
                    return ResourceManager.GetString("TypeConverterFailed2", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The name of the type &apos;{0}&apos; contains characters that are invalid in XAML. 的本地化字符串。
            /// </summary>
            internal static string TypeHasInvalidXamlName
            {
                get
                {
                    return ResourceManager.GetString("TypeHasInvalidXamlName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Type &apos;{0}&apos; does not have a content property. Specify the name of the property to set, or add a ContentPropertyAttribute or TypeConverterAttribute on the type. 的本地化字符串。
            /// </summary>
            internal static string TypeHasNoContentProperty
            {
                get
                {
                    return ResourceManager.GetString("TypeHasNoContentProperty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot change property metadata after it has been associated with a property. 的本地化字符串。
            /// </summary>
            internal static string TypeMetadataCannotChangeAfterUse
            {
                get
                {
                    return ResourceManager.GetString("TypeMetadataCannotChangeAfterUse", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Type name &apos;{0}&apos; cannot have a dot &apos;.&apos;. 的本地化字符串。
            /// </summary>
            internal static string TypeNameCannotHavePeriod
            {
                get
                {
                    return ResourceManager.GetString("TypeNameCannotHavePeriod", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Type reference cannot find type named &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string TypeNotFound
            {
                get
                {
                    return ResourceManager.GetString("TypeNotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Type &apos;{0}&apos; is not public. 的本地化字符串。
            /// </summary>
            internal static string TypeNotPublic
            {
                get
                {
                    return ResourceManager.GetString("TypeNotPublic", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unclosed quoted value. 的本地化字符串。
            /// </summary>
            internal static string UnclosedQuote
            {
                get
                {
                    return ResourceManager.GetString("UnclosedQuote", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unexpected close of XAML Node Stream. 的本地化字符串。
            /// </summary>
            internal static string UnexpectedClose
            {
                get
                {
                    return ResourceManager.GetString("UnexpectedClose", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Invalid metadata for attribute &apos;{0}&apos; on &apos;{1}&apos;. Expected &apos;{2}&apos; argument(s) of type &apos;{3}&apos;. 的本地化字符串。
            /// </summary>
            internal static string UnexpectedConstructorArg
            {
                get
                {
                    return ResourceManager.GetString("UnexpectedConstructorArg", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unexpected &apos;{0}&apos; in parse rule &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string UnexpectedNodeType
            {
                get
                {
                    return ResourceManager.GetString("UnexpectedNodeType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Parameter is unexpected type &apos;{0}&apos;. Expected type is &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string UnexpectedParameterType
            {
                get
                {
                    return ResourceManager.GetString("UnexpectedParameterType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unexpected token &apos;{0}&apos; in rule: &apos;{1}&apos;, in &apos;{2}&apos;. 的本地化字符串。
            /// </summary>
            internal static string UnexpectedToken
            {
                get
                {
                    return ResourceManager.GetString("UnexpectedToken", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unexpected token after end of markup extension. 的本地化字符串。
            /// </summary>
            internal static string UnexpectedTokenAfterME
            {
                get
                {
                    return ResourceManager.GetString("UnexpectedTokenAfterME", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unhandled BoolTypeBit. 的本地化字符串。
            /// </summary>
            internal static string UnhandledBoolTypeBit
            {
                get
                {
                    return ResourceManager.GetString("UnhandledBoolTypeBit", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 [&apos;{0}&apos;(&apos;{1}&apos;)]5D; on &apos;{2}&apos; is not a known property. 的本地化字符串。
            /// </summary>
            internal static string UnknownAttributeProperty
            {
                get
                {
                    return ResourceManager.GetString("UnknownAttributeProperty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unknown member &apos;{0}&apos; on &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string UnknownMember
            {
                get
                {
                    return ResourceManager.GetString("UnknownMember", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unknown member &apos;{0}&apos; on unknown type &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string UnknownMemberOnUnknownType
            {
                get
                {
                    return ResourceManager.GetString("UnknownMemberOnUnknownType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unknown member &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string UnknownMemberSimple
            {
                get
                {
                    return ResourceManager.GetString("UnknownMemberSimple", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unknown type &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string UnknownType
            {
                get
                {
                    return ResourceManager.GetString("UnknownType", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unresolved reference &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string UnresolvedForwardReferences
            {
                get
                {
                    return ResourceManager.GetString("UnresolvedForwardReferences", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML namespace &apos;{0}&apos; is not resolved. 的本地化字符串。
            /// </summary>
            internal static string UnresolvedNamespace
            {
                get
                {
                    return ResourceManager.GetString("UnresolvedNamespace", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Uri &apos;{0}&apos; was not found. 的本地化字符串。
            /// </summary>
            internal static string UriNotFound
            {
                get
                {
                    return ResourceManager.GetString("UriNotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Error with type &apos;{0}&apos;. MarkupExtensions cannot use the &apos;UsableDuringInitialization&apos; attribute. 的本地化字符串。
            /// </summary>
            internal static string UsableDuringInitializationOnME
            {
                get
                {
                    return ResourceManager.GetString("UsableDuringInitializationOnME", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 A value in the &apos;{0}&apos; array is null. 的本地化字符串。
            /// </summary>
            internal static string ValueInArrayIsNull
            {
                get
                {
                    return ResourceManager.GetString("ValueInArrayIsNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XAML Node Stream: Value nodes must be followed by EndMember. 的本地化字符串。
            /// </summary>
            internal static string ValueMustBeFollowedByEndMember
            {
                get
                {
                    return ResourceManager.GetString("ValueMustBeFollowedByEndMember", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Specified index is out of range or child at index is null. Do not call this method if VisualChildrenCount returns zero, indicating that the Visual has no children. 的本地化字符串。
            /// </summary>
            internal static string Visual_ArgumentOutOfRange
            {
                get
                {
                    return ResourceManager.GetString("Visual_ArgumentOutOfRange", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 White space is not allowed after end of markup extension. 的本地化字符串。
            /// </summary>
            internal static string WhitespaceAfterME
            {
                get
                {
                    return ResourceManager.GetString("WhitespaceAfterME", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 XamlXmlWriter cannot write value &apos;{0}&apos; which contains significant white space in collection &apos;{1}&apos;. 的本地化字符串。
            /// </summary>
            internal static string WhiteSpaceInCollection
            {
                get
                {
                    return ResourceManager.GetString("WhiteSpaceInCollection", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 An attempt was made to write to a XamlWriter that has had its Closed method called. 的本地化字符串。
            /// </summary>
            internal static string WriterIsClosed
            {
                get
                {
                    return ResourceManager.GetString("WriterIsClosed", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unexpected XAML node type &apos;{0}&apos; from XamlReader in XamlFactory. 的本地化字符串。
            /// </summary>
            internal static string XamlFactoryInvalidXamlNode
            {
                get
                {
                    return ResourceManager.GetString("XamlFactoryInvalidXamlNode", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot set SchemaContext on XamlMarkupExtensionWriter. 的本地化字符串。
            /// </summary>
            internal static string XamlMarkupExtensionWriterCannotSetSchemaContext
            {
                get
                {
                    return ResourceManager.GetString("XamlMarkupExtensionWriterCannotSetSchemaContext", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot write a value that is not a string. 的本地化字符串。
            /// </summary>
            internal static string XamlMarkupExtensionWriterCannotWriteNonstringValue
            {
                get
                {
                    return ResourceManager.GetString("XamlMarkupExtensionWriterCannotWriteNonstringValue", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The member &apos;{0}&apos; has already been written. 的本地化字符串。
            /// </summary>
            internal static string XamlMarkupExtensionWriterDuplicateMember
            {
                get
                {
                    return ResourceManager.GetString("XamlMarkupExtensionWriterDuplicateMember", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Errors detected in input. 的本地化字符串。
            /// </summary>
            internal static string XamlMarkupExtensionWriterInputInvalid
            {
                get
                {
                    return ResourceManager.GetString("XamlMarkupExtensionWriterInputInvalid", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot convert this XamlTypeName instance to a string because the provided INamespacePrefixLookup could not generate a prefix for the namespace &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string XamlTypeNameCannotGetPrefix
            {
                get
                {
                    return ResourceManager.GetString("XamlTypeNameCannotGetPrefix", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot convert this XamlTypeName instance to a string because the Name property is null or empty. Set the Name property before calling XamlTypeName.ToString. 的本地化字符串。
            /// </summary>
            internal static string XamlTypeNameNameIsNullOrEmpty
            {
                get
                {
                    return ResourceManager.GetString("XamlTypeNameNameIsNullOrEmpty", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot convert this XamlTypeName instance to a string because the Namespace property is null. Set the Namespace property before calling XamlTypeName.ToString. 的本地化字符串。
            /// </summary>
            internal static string XamlTypeNameNamespaceIsNull
            {
                get
                {
                    return ResourceManager.GetString("XamlTypeNameNamespaceIsNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot write a value that is not a string. 的本地化字符串。
            /// </summary>
            internal static string XamlXmlWriterCannotWriteNonstringValue
            {
                get
                {
                    return ResourceManager.GetString("XamlXmlWriterCannotWriteNonstringValue", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The member &apos;{0}&apos; has already been written. 的本地化字符串。
            /// </summary>
            internal static string XamlXmlWriterDuplicateMember
            {
                get
                {
                    return ResourceManager.GetString("XamlXmlWriterDuplicateMember", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The argument isObjectFromMember can only be set to true when the type is a collection. 的本地化字符串。
            /// </summary>
            internal static string XamlXmlWriterIsObjectFromMemberSetForArraysOrNonCollections
            {
                get
                {
                    return ResourceManager.GetString("XamlXmlWriterIsObjectFromMemberSetForArraysOrNonCollections", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Namespace &apos;{0}&apos; already has a prefix defined in current scope. 的本地化字符串。
            /// </summary>
            internal static string XamlXmlWriterNamespaceAlreadyHasPrefixInCurrentScope
            {
                get
                {
                    return ResourceManager.GetString("XamlXmlWriterNamespaceAlreadyHasPrefixInCurrentScope", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The prefix &apos;{0}&apos; is already defined in current scope. 的本地化字符串。
            /// </summary>
            internal static string XamlXmlWriterPrefixAlreadyDefinedInCurrentScope
            {
                get
                {
                    return ResourceManager.GetString("XamlXmlWriterPrefixAlreadyDefinedInCurrentScope", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unable to call &apos;{0}&apos; in current state. 的本地化字符串。
            /// </summary>
            internal static string XamlXmlWriterWriteNotSupportedInCurrentState
            {
                get
                {
                    return ResourceManager.GetString("XamlXmlWriterWriteNotSupportedInCurrentState", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unable to call WriteObject with isObjectFromMember set to true in current state. 的本地化字符串。
            /// </summary>
            internal static string XamlXmlWriterWriteObjectNotSupportedInCurrentState
            {
                get
                {
                    return ResourceManager.GetString("XamlXmlWriterWriteObjectNotSupportedInCurrentState", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Need to implement public/internal sorting. 的本地化字符串。
            /// </summary>
            internal static string XaslTypePropertiesNotImplemented
            {
                get
                {
                    return ResourceManager.GetString("XaslTypePropertiesNotImplemented", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Specified class name &apos;{0}&apos; doesn&apos;t match actual root instance type &apos;{1}&apos;. Remove the Class directive or provide an instance via XamlObjectWriterSettings.RootObjectInstance. 的本地化字符串。
            /// </summary>
            internal static string XClassMustMatchRootInstance
            {
                get
                {
                    return ResourceManager.GetString("XClassMustMatchRootInstance", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Choice cannot follow a Fallback. 的本地化字符串。
            /// </summary>
            internal static string XCRChoiceAfterFallback
            {
                get
                {
                    return ResourceManager.GetString("XCRChoiceAfterFallback", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 AlternateContent must contain one or more Choice elements. 的本地化字符串。
            /// </summary>
            internal static string XCRChoiceNotFound
            {
                get
                {
                    return ResourceManager.GetString("XCRChoiceNotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Choice is valid only in AlternateContent. 的本地化字符串。
            /// </summary>
            internal static string XCRChoiceOnlyInAC
            {
                get
                {
                    return ResourceManager.GetString("XCRChoiceOnlyInAC", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 There is a cycle of XML compatibility definitions, such that namespace &apos;{0}&apos; overrides itself. This could be due to inconsistent XmlnsCompatibilityAttributes in different assemblies. Please change the definitions to eliminate this cycle, or pass a non-conflicting set of Reference Assemblies in the XamlSchemaContext constructor. 的本地化字符串。
            /// </summary>
            internal static string XCRCompatCycle
            {
                get
                {
                    return ResourceManager.GetString("XCRCompatCycle", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Duplicate Preserve declaration for element &apos;{1}&apos; in namespace &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string XCRDuplicatePreserve
            {
                get
                {
                    return ResourceManager.GetString("XCRDuplicatePreserve", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Duplicate ProcessContent declaration for element &apos;{1}&apos; in namespace &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string XCRDuplicateProcessContent
            {
                get
                {
                    return ResourceManager.GetString("XCRDuplicateProcessContent", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Duplicate wildcard Preserve declaration for namespace &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string XCRDuplicateWildcardPreserve
            {
                get
                {
                    return ResourceManager.GetString("XCRDuplicateWildcardPreserve", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Duplicate wildcard ProcessContent declaration for namespace &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string XCRDuplicateWildcardProcessContent
            {
                get
                {
                    return ResourceManager.GetString("XCRDuplicateWildcardProcessContent", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Fallback is valid only in AlternateContent. 的本地化字符串。
            /// </summary>
            internal static string XCRFallbackOnlyInAC
            {
                get
                {
                    return ResourceManager.GetString("XCRFallbackOnlyInAC", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; element is not a valid child of AlternateContent. Only Choice and Fallback elements are valid children of an AlternateContent element. 的本地化字符串。
            /// </summary>
            internal static string XCRInvalidACChild
            {
                get
                {
                    return ResourceManager.GetString("XCRInvalidACChild", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; attribute is not valid for &apos;{1}&apos; element. 的本地化字符串。
            /// </summary>
            internal static string XCRInvalidAttribInElement
            {
                get
                {
                    return ResourceManager.GetString("XCRInvalidAttribInElement", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; format is not valid. 的本地化字符串。
            /// </summary>
            internal static string XCRInvalidFormat
            {
                get
                {
                    return ResourceManager.GetString("XCRInvalidFormat", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot have both a specific and a wildcard Preserve declaration for namespace &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string XCRInvalidPreserve
            {
                get
                {
                    return ResourceManager.GetString("XCRInvalidPreserve", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Cannot have both a specific and a wildcard ProcessContent declaration for namespace &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string XCRInvalidProcessContent
            {
                get
                {
                    return ResourceManager.GetString("XCRInvalidProcessContent", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Requires attribute must contain a valid namespace prefix. 的本地化字符串。
            /// </summary>
            internal static string XCRInvalidRequiresAttribute
            {
                get
                {
                    return ResourceManager.GetString("XCRInvalidRequiresAttribute", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; attribute value is not a valid XML name. 的本地化字符串。
            /// </summary>
            internal static string XCRInvalidXMLName
            {
                get
                {
                    return ResourceManager.GetString("XCRInvalidXMLName", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 AlternateContent must contain only one Fallback element. 的本地化字符串。
            /// </summary>
            internal static string XCRMultipleFallbackFound
            {
                get
                {
                    return ResourceManager.GetString("XCRMultipleFallbackFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 MustUnderstand condition failed on namespace &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string XCRMustUnderstandFailed
            {
                get
                {
                    return ResourceManager.GetString("XCRMustUnderstandFailed", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; namespace cannot preserve items; it must be declared Ignorable first. 的本地化字符串。
            /// </summary>
            internal static string XCRNSPreserveNotIgnorable
            {
                get
                {
                    return ResourceManager.GetString("XCRNSPreserveNotIgnorable", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; namespace cannot process content; it must be declared Ignorable first. 的本地化字符串。
            /// </summary>
            internal static string XCRNSProcessContentNotIgnorable
            {
                get
                {
                    return ResourceManager.GetString("XCRNSProcessContentNotIgnorable", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Choice must contain a Requires attribute. 的本地化字符串。
            /// </summary>
            internal static string XCRRequiresAttribNotFound
            {
                get
                {
                    return ResourceManager.GetString("XCRRequiresAttribNotFound", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 &apos;{0}&apos; prefix is not defined. 的本地化字符串。
            /// </summary>
            internal static string XCRUndefinedPrefix
            {
                get
                {
                    return ResourceManager.GetString("XCRUndefinedPrefix", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unrecognized compatibility attribute &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string XCRUnknownCompatAttrib
            {
                get
                {
                    return ResourceManager.GetString("XCRUnknownCompatAttrib", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 Unrecognized Compatibility element &apos;{0}&apos;. 的本地化字符串。
            /// </summary>
            internal static string XCRUnknownCompatElement
            {
                get
                {
                    return ResourceManager.GetString("XCRUnknownCompatElement", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The value for XmlData property &apos;{0}&apos; is null or not IXmlSerializable. 的本地化字符串。
            /// </summary>
            internal static string XmlDataNull
            {
                get
                {
                    return ResourceManager.GetString("XmlDataNull", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 There is a cycle of XmlnsCompatibleWithAttribute definitions in assembly &apos;{0}&apos;, such that namespace &apos;{1}&apos; overrides itself. Change the definitions to eliminate this cycle. 的本地化字符串。
            /// </summary>
            internal static string XmlnsCompatCycle
            {
                get
                {
                    return ResourceManager.GetString("XmlnsCompatCycle", resourceCulture);
                }
            }

            /// <summary>
            ///   查找类似 The value for XmlData property &apos;{0}&apos; is not an XmlReader. 的本地化字符串。
            /// </summary>
            internal static string XmlValueNotReader
            {
                get
                {
                    return ResourceManager.GetString("XmlValueNotReader", resourceCulture);
                }
            }
    }

public class ReferenceEqualityComparer:IEqualityComparer<object>
{
    public static ReferenceEqualityComparer Instance{ get; }
    public bool Equals(object x, object y)
    {
        throw new NotImplementedException();
    }

    public int GetHashCode(object obj)
    {
        throw new NotImplementedException();
    }
}
