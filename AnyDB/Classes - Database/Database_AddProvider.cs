/********************************************************************************************************************
 * 
 * Database_AddProvider.cs
 * 
 * Credit where it's due: Google "sunspots western civilization duct tape programming."
 * 
 * But my method is slightly more cunning. I'm assuming here that every .NET provider just sticks the word "Factory" 
 * on its name and calls it a DbProviderFactory. It works for most the databases I've looked at, and web.config seems 
 * to imply just that. 
 * 
 * Another, more involved, method would be to load the DLL and use reflection to look for a class that is derived from 
 * DbProviderFactory. But that's too much hard work. And it might result in a very nasty coronal mass ejection from my 
 * arse...
 * 
 * "So cunning you could stick a tail on it and call it a weasel." (Edmund Blackadder).
 */

using System.Configuration;
using System.Data;

namespace AnyDB
{
    public partial class Database
    {
        /// <summary>
        /// First choice:
        /// Add a provider invariant name to the list of known .NET providers, assuming that the factory class can be 
        /// derived from the invariant name. Call this method on startup, before invoking the Database(string,string) 
        /// constructor, if the class is not registered through machine.config, app.config or web.config, but the DLL 
        /// can be found in the application directory. The config route is very much to be preferred even to this first 
        /// AddProvider() method.
        /// </summary>
        /// <param name="ProviderInvariantName">
        /// The provider's unambiguous reference. This is usually the name of the DLL without the .DLL extension. For 
        /// example, Microsoft SQL Server uses the provider invariant name "System.Data.SqlClient" (but that is, of 
        /// course, always installed by default).
        /// </param>
        
        public static void AddProvider(string ProviderInvariantName)
        {
            int dot = ProviderInvariantName.LastIndexOf(".");
            string clsnam = dot >= 0 ? ProviderInvariantName.Substring(dot+1) : ProviderInvariantName;
            AddProvider(ProviderInvariantName, ProviderInvariantName + "." + clsnam + "Factory");
        }

        /// <summary>
        /// Second choice:
        /// Use this method if the factory class has an unusual name that does not follow from the last part of the 
        /// provider invariant name.
        /// </summary>
        /// <param name="ProviderInvariantName">
        /// The provider's unambiguous reference. This is usually the same as the provider's namespace, and usually the 
        /// name of the DLL without the .DLL extension. For example, for SQL Server this is "System.Data.SqlClient".
        /// </param>
        /// <param name="FactoryClass">
        /// The fully qualified DbProviderFactory class. For SQL Server this is
        /// "System.Data.SqlClient.SqlClientFactory".
        /// </param>

        public static void AddProvider(string ProviderInvariantName, string FactoryClass)
        {
            AddProvider(ProviderInvariantName, FactoryClass, ProviderInvariantName);
        }

        /// <summary>
        /// Third choice:
        /// Use this method if the factory class and/or the provider's namespace follow some strange pattern, or no 
        /// discernible pattern at all. At this point it might be a good idea to familiarise yourself with the Notepad 
        /// application, because editing web.config will be much less hassle than cobbling up a fourth choice method. 
        /// You may also want to submit an SPR (Software Performance Report) to your database provider, and ask them if 
        /// their Evil Genius Hackrz Department want some Hot As Hades Hemlock Pizza delivering in their extended lunch 
        /// break. You'll find Conium maculatum growing around major roads and industrial sites throughout England and 
        /// Wales.
        /// </summary>
        /// <param name="ProviderInvariantName">
        /// The provider's unambiguous reference. This usually matches the provider's namespace, and usually the name 
        /// of the DLL without the .DLL extension. For example, for SQL Server this is "System.Data.SqlClient".
        /// </param>
        /// <param name="FactoryClass">
        /// The fully qualified DbProviderFactory class. For SQL Server this is "System.Data.SqlClient.SqlClientFactory".
        /// </param>
        /// <param name="ClassNamespace">
        /// The provider's namespace. For SQL Service this is "System.Data.SqlClient".
        /// </param>

        public static void AddProvider(string ProviderInvariantName, string FactoryClass, string ClassNamespace)
        {
            var cm = ConfigurationManager.GetSection("system.data") as DataSet;
            DataTable dt = cm.Tables[0];
            if (dt.Select("InvariantName='"+ProviderInvariantName+"'").Length == 0)
            {
                dt.Rows.Add("Short Description: " + ProviderInvariantName,
                            "Long Description: " + ProviderInvariantName,
                            ProviderInvariantName,
                            FactoryClass + ", " + ClassNamespace);
            }
        }
    }
}