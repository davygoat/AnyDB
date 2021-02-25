/*===================================================================================================================
 *
 * This used to be a great big list of if statements. The current version uses reflection to find Driver classes 
 * that are 'decorated' with custom attributes telling us how and when they can be used. That has the advantage of 
 * not needing any code changes when you add support for a new database. All the vendor specific stuff should now be 
 * neatly confined to your class, unless your database happens to introduce a new quirk.
 */

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AnyDB
{
    public partial class Database
    {
        class DriverInfo
        {
            public string          Name;
            public Type            Type;
            public Ident           Ident;
            public ConstructorInfo Constructor;
        }

        static List<DriverInfo> DriverConstructors = null;
        static Dictionary<string, ConstructorInfo> ConstructorInfoCache = new Dictionary<string,ConstructorInfo>();
        static Dictionary<string, Type> AssemblyLoadedFactories = new Dictionary<string, Type>();

        /// <summary>
        /// Provides information about the database's capabilities and behaviours. Writing an AnyDB driver is mostly 
        /// about configuring this object. You should not need to modify this struct for normal operation. You can if
        /// you need to, but that is not recommended if you want to maintain portability.
        /// </summary>
        public DriverBase Driver { get; private set; }

        /*===========================================================================================================
         * 
         * On initialization, use reflection to :-
         * 
         * - find Driver classes
         * - that have constructors
         * - with one or more [Ident(...)] attributes.
         * 
         * I ought to use linq for this, but I don't like linq.
         * 
         * The result goes into a list called DriverConstructors.
         */

        static void FindDriverClasses()
        {
            List<string> tseList = new List<string>();
            List<string> limList = new List<string>();

            DriverConstructors = new List<DriverInfo>();

            foreach (Assembly asm in new [] { Assembly.GetExecutingAssembly(),
                                              Assembly.GetEntryAssembly() })
            {
                // GetEntryAssembly() returns null in Visual Studio ASP.NET
                if (asm == null) continue;

                foreach (Type type in asm.GetTypes())
                {
                    // we're only interested in classes derived from DriverBase
		            if (!type.IsClass) continue;
                    if (type.BaseType != typeof(DriverBase) && type.BaseType.BaseType != typeof(DriverBase)) continue;
                    // look for [RegisterProvider] attributes
                    {
                        var provider = Attribute.GetCustomAttribute(type, typeof(RegisterProvider)) as RegisterProvider;
                        if (provider != null)
                        {
                            if (provider.DLL != null && provider.FactoryClass != null)
                            {
                                try
                                {
                                    var dll = Assembly.LoadFrom(provider.DLL);
                                    if (dll == null)
                                    {
                                        Console.WriteLine("Cannot load '{0}' DLL", provider.DLL);
                                        continue;
                                    }
                                    Type typ = dll.GetType(provider.FactoryClass);
                                    AssemblyLoadedFactories[provider.ProviderInvariantName] = typ;
                                }
                                catch (FileNotFoundException)
                                {
                                    // TODO - poke this message into the type's DLLNotFound field and throw it only if used.
                                    Debug.WriteLine(string.Format("'{0}' DLL not found", provider.DLL));
                                    continue;
                                }
                            }
                            else if (provider.ClassNamespace != null)
                                AddProvider(provider.ProviderInvariantName, provider.FactoryClass, provider.ClassNamespace);
                            else
                                AddProvider(provider.ProviderInvariantName);
                        }
                    }

                    bool haveParameterLess = false;

                    // look for constructors with [Ident] attributes
                    foreach (ConstructorInfo constr in type.GetConstructors())
                    {
                        if (constr.GetParameters().Length == 0) haveParameterLess = true;
                        foreach (Attribute attr in constr.GetCustomAttributes(false))
                        {
                            if (attr is Ident)
                            {
                                DriverInfo info = new DriverInfo();
                                info.Name = type.Name;
                                info.Type = type;
                                info.Constructor = constr;
                                info.Ident = (Ident)attr;
                                DriverConstructors.Add(info);
                            }
                        }
                    }

                    if (!haveParameterLess)
                    {
                        Console.WriteLine("***********************************************************************");
                        Console.WriteLine("{0} does not have a parameterless constructor", type);
                        Console.WriteLine("***********************************************************************");
                        Console.ReadLine();
                        Environment.Exit(0);
                    }

                    // invoke parameterless constructor to get rewrite parameters
                    DriverBase drv = type.InvokeMember(null, BindingFlags.CreateInstance, null, null, null) as DriverBase;
                    foreach (Regex re in drv.TimespanExpressions)
                    {
                        if (!tseList.Contains(re.ToString()))
                        {
                            TimespanExpressions.Add(re);
                            tseList.Add(re.ToString());
                        }
                    }
                    foreach (Regex re in drv.LimitExpressions)
                    {
                        if (!limList.Contains(re.ToString()))
                        {
                            LimitExpressions.Add(re);
                            limList.Add(re.ToString());
                        }
                    }
                }
            }
        }

        /*===========================================================================================================
         *
         * Scan the DriverConstructors list, looking for a constructor that can handle the database we want to use.
         * 
         * The [Ident] attribute on each constructor has one or more of the following parameters:
         * 
         * ProductName                       - The exact product name as reported by the server.
         * ProductNameStartsWith             - Initial string to match the product name.
         * ProductNameEndsWith               - Similar to StartsWith, but using the end of the product name.
         * ProductNameContains               - A product name substring.
         * 
         * ProviderInvariantName             - The data provider's assembly name.
         * 
         * ConnectionStringContains          - A small portion of the connection string to use for identification.
         * ConnectionStringRegularExpression - A more complicated pattern to recognise in the connection string.
         * 
         * Most drivers will use either the product name, or a combination of provider invariant name and connection
         * string. The connection string is most often used for ODBC drivers.
         */

        private DriverBase MakeDriverObject(DbConnection connect)
        {
            /*
             * Scan the list, looking for a suitable match. We're only going to do this once for each connection string. 
             * Subsequent lookups with the same connection string will use the cache.
             * 
             * Yes, I do know about Edsger Dijkstra: I used to live within cycling distance across the border from him. 
             * If I can find a less 'harmful' way to do this, I will use it. But sometimes you have to respectfully 
             * disagree with received wisdom.
             */

            string key = ProviderInvariantName + "¬" + ConnectionString;
            Ident Ident = null;

            if (!ConstructorInfoCache.ContainsKey(key))
            {
                foreach (var info in DriverConstructors)
                {
                check_product_name:

                    if (info.Ident.ProductName != null)
                    {
                        if (ProductName == info.Ident.ProductName) goto check_provider_invariant;
                        else goto next_constructor;
                    }
                    if (info.Ident.ProductNameStartsWith != null)
                    {
                        if (ProductName.StartsWith(info.Ident.ProductNameStartsWith)) goto check_provider_invariant;
                        else goto next_constructor;
                    }
                    if (info.Ident.ProductNameEndsWith != null)
                    {
                        if (ProductName.EndsWith(info.Ident.ProductNameEndsWith)) goto check_provider_invariant;
                        else goto next_constructor;
                    }
                    if (info.Ident.ProductNameContains != null)
                    {
                        if (ProductName.Contains(info.Ident.ProductNameContains)) goto check_provider_invariant;
                        else goto next_constructor;
                    }

                check_provider_invariant:

                    if (info.Ident.ProviderInvariantName != null)
                    {
                        if (ProviderInvariantName == info.Ident.ProviderInvariantName) goto check_connection_string;
                        else goto next_constructor;
                    }

                check_connection_string:

                    if (info.Ident.ConnectionStringContains != null)
                    {
                        if (ConnectionString.Contains(info.Ident.ConnectionStringContains)) goto we_have_a_match;
                        else goto next_constructor;
                    }
                    if (info.Ident.ConnectionStringRegularExpression != null)
                    {
                        if (new Regex(info.Ident.ConnectionStringRegularExpression).IsMatch(ConnectionString)) goto we_have_a_match;
                        else goto next_constructor;
                    }

                we_have_a_match:

                    ConstructorInfoCache[key] = info.Constructor;
                    Ident = info.Ident;
                    break;

                next_constructor:

                    if (info.Constructor == null || info.Constructor != null) continue;
                    else goto check_product_name; // this two bit compiler thinks one plus one is three
                }
            }

            /*
             * If we found a matching constructor, invoke it.
             */

            if (ConstructorInfoCache.ContainsKey(key))
            {
                ConstructorInfo TheConstructor = ConstructorInfoCache[key];

                // Constructors can have parameters, so we have to build an array
                List<object> args = new List<object>();
                foreach (ParameterInfo prm in TheConstructor.GetParameters())
                {
                    if (prm.ParameterType == typeof(DbConnection)) args.Add(connect);
                    else if (prm.Name == "ProductVersion") args.Add(ProductVersion);
                    else if (prm.Name == "ConnectionString") args.Add(ConnectionString);
                    else if (prm.Name == "CurrentSchema") args.Add(CurrentSchema);
                    else if (prm.Name == "ProviderInvariantName") args.Add(ProviderInvariantName);
                    else if (prm.Name == "Factory") args.Add(Factory);
                    else
                    {
                        ConstructorInfoCache.Remove(key);
                        throw new Wobbler(string.Format("One of your '{0}' constructors has an unrecognised parameter '{1}' of type {2}",
                                                        Ident != null ? Ident.Provider.ToString() : "vendor specific",
                                                        prm.Name,
                                                        prm.ParameterType));
                    }
                }

                // Now invoke the constructor to instantiate the object
                return (DriverBase)TheConstructor.Invoke(args.ToArray());
            }

            /*
             * Failing that, return the base class, which may or may not work.
             */

            return new DriverBase();
        }
    }
}
