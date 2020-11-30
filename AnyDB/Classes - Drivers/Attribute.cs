using System;

namespace AnyDB
{
    /// <summary>
    /// Place this attribute before the driver to list its provider invariant name, and AnyDB will make sure the .NET
    /// provider is registered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
    public class RegisterProvider : Attribute
    {
        /// <summary>
        /// Unambiguous reference to the .NET data provider, e.g. System.Data.SQLite.
        /// </summary>
        public string ProviderInvariantName;

        /// <summary>
        /// The name of the DLL to load, if the assembly cannot be loaded by DbProviderFactories.GetFactory().
        /// </summary>
        public string DLL;

        /// <summary>
        /// The namespace qualified name of the factory class, used with the DLL field.
        /// </summary>
        public string FactoryClass;

        /// <summary>
        /// The namespace used by the assembly. Usually the same as the factory class namespace.
        /// </summary>
        public string ClassNamespace;

        /// <summary>
        /// </summary>
        /// <param name="ProviderInvariantName">The provider invariant name, i.e. the driver's reference.</param>
        public RegisterProvider(string ProviderInvariantName)
        {
            this.ProviderInvariantName = ProviderInvariantName;
        }
    }

    /// <summary>
    /// Place this attribute before every constructor in your Driver class to specify the conditions for using
    /// the class, i.e. deciding whether or not the class is suitable for the querying that particular database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple=true)]
    public class Ident : Attribute
    {
        /// <summary>
        /// Something with which to identify your driver, e.g. the RDBMS name.
        /// </summary>
        public Providers Provider;

        /// <summary>
        /// Use this field if you can identify the database type by its ProductName having a certain value, e.g.
        /// "MySQL".
        /// </summary>
        public string ProductName;

        /// <summary>
        /// Use this field if you can identify the database type by its ProductName starting with a prefix, e.g.
        /// "DB2/" to match DB2/Linux, DB2/Windows or DB2/AS400.
        /// </summary>
        public string ProductNameStartsWith;

        /// <summary>
        /// Use this field if you can identify the database type by its ProductName ending with a particular suffix,
        /// e.g. "-Codasyl" for a hypothetical "BunnyVAX-3000-NanoVMS-Codasyl".
        /// </summary>
        public string ProductNameEndsWith;

        /// <summary>
        /// Use this field if you can identify the database type by its ProductName containing a substring, e.g.
        /// "MegaDB" for "HumungousVaporCloud Inc. MegaDB NoSQL Version 11h-2 Build 8250 Teatime".
        /// </summary>
        public string ProductNameContains;

        /// <summary>
        /// Use this field if you can identify the database type by its ProviderInvariantName having a fixed known
        /// string value, e.g. "System.Data.Odbc". This field is normally used together with the connection string.
        /// </summary>
        public string ProviderInvariantName;

        /// <summary>
        /// Use this field if the connection string contains useful information that identifies the database type, 
        /// e.g. "Text Driver" or "=Delimited" for comma delimited text.
        /// </summary>
        public string ConnectionStringContains;

        /// <summary>
        /// Use this field if you need to look for a more complex pattern in the connection string to identify the 
        /// database type, e.g. "Extended Properties='[^']*Text,?" is used to recognise that the OLEDB provider is
        /// being used for ASCII text as opposed to, say, Excel.
        /// </summary>
        public string ConnectionStringRegularExpression;

        /// <summary>
        /// </summary>
        /// <param name="Provider">For information only.</param>
        public Ident(Providers Provider)
        {
            this.Provider = Provider;
        }

        /// <summary>
        /// </summary>
        public Ident()
        {
        }
    }
}