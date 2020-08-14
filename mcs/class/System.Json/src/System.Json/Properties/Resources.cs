namespace System.Json.Properties {
    using System;
    
    
    internal class Resources {
        internal Resources() {
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The argument &apos;{0}&apos; must be greater than or equal to {1}..
        /// </summary>
        internal static string ArgumentMustBeGreaterThanOrEqualTo {
            get {
                return "The argument '{0}' must be greater than or equal to {1}.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to cast object of type &apos;{0}&apos; to type &apos;{1}&apos;..
        /// </summary>
        internal static string CannotCastJsonValue {
            get {
                return "Unable to cast object of type '{0}' to type '{1}'.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to CannotReadAsType=Cannot read &apos;{0}&apos; as &apos;{1}&apos; type..
        /// </summary>
        internal static string CannotReadAsType {
            get {
                return "CannotReadAsType=Cannot read '{0}' as '{1}' type.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot read JsonPrimitive value &apos;{0}&apos; as &apos;{1}&apos;..
        /// </summary>
        internal static string CannotReadPrimitiveAsType {
            get {
                return "Cannot read JsonPrimitive value '{0}' as '{1}'.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; does not contain a definition for property &apos;{1}&apos;..
        /// </summary>
        internal static string DynamicPropertyNotDefined {
            get {
                return "'{0}' does not contain a definition for property '{1}'.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The input source is not correctly formatted..
        /// </summary>
        internal static string IncorrectJsonFormat {
            get {
                return "The input source is not correctly formatted.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; type indexer is not supported on JsonValue of &apos;JsonType.{1}&apos; type..
        /// </summary>
        internal static string IndexerNotSupportedOnJsonType {
            get {
                return "'{0}' type indexer is not supported on JsonValue of 'JsonType.{1}' type.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot convert null to &apos;{0}&apos; because it is a non-nullable value type..
        /// </summary>
        internal static string InvalidCastNonNullable {
            get {
                return "Cannot convert null to '{0}' because it is a non-nullable value type.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot cast JsonPrimitive value &apos;{0}&apos; as &apos;{1}&apos;. It is not in a valid date format..
        /// </summary>
        internal static string InvalidDateFormat {
            get {
                return "Cannot cast JsonPrimitive value '{0}' as '{1}'. It is not in a valid date format.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid &apos;{0}&apos; index type; only &apos;System.String&apos; and non-negative &apos;System.Int32&apos; types are supported..
        /// </summary>
        internal static string InvalidIndexType {
            get {
                return "Invalid '{0}' index type; only 'System.String' and non-negative 'System.Int32' types are supported.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid JSON primitive: {0}..
        /// </summary>
        internal static string InvalidJsonPrimitive {
            get {
                return "Invalid JSON primitive: {0}.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot cast &apos;{0}&apos; value &apos;{1}.{2}&apos; as a type of &apos;{3}&apos;. The provided string is not a valid relative or absolute &apos;{3}&apos;..
        /// </summary>
        internal static string InvalidUriFormat {
            get {
                return "Cannot cast '{0}' value '{1}.{2}' as a type of '{3}'. The provided string is not a valid relative or absolute '{3}'.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An empty string cannot be parsed as JSON..
        /// </summary>
        internal static string JsonStringCannotBeEmpty {
            get {
                return "An empty string cannot be parsed as JSON.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Null index or multidimensional indexing is not supported by this indexer; use &apos;System.Int32&apos; or &apos;System.String&apos; for array and object indexing respectively..
        /// </summary>
        internal static string NonSingleNonNullIndexNotSupported {
            get {
                return "Null index or multidimensional indexing is not supported by this indexer; use 'System.Int32' or 'System.String' for array and object indexing respectively.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot cast JsonPrimitive value &apos;{0}&apos; as &apos;{1}&apos;. The value is either too large or too small for the specified CLR type..
        /// </summary>
        internal static string OverflowReadAs {
            get {
                return ">Cannot cast JsonPrimitive value '{0}' as '{1}'. The value is either too large or too small for the specified CLR type.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Object type not supported..
        /// </summary>
        internal static string TypeNotSupported {
            get {
                return "Object type not supported.";
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Operation not supported on JsonValue instances of &apos;JsonType.Default&apos; type..
        /// </summary>
        internal static string UseOfDefaultNotAllowed {
            get {
                return "Operation not supported on JsonValue instances of 'JsonType.Default' type.";
            }
        }
    }
}
