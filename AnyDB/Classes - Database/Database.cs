/********************************************************************************************************************
 * 
 * Database.cs
 * 
 * A starting point for the AnyDB.Database class. I've divided the real functionality into logical sections of this 
 * "partial" class. This file just has a couple of readonly fields for reference.
 */

using System.Data.Common;

namespace AnyDB
{
    /// <summary>
    /// The main AnyDB class.
    /// </summary>
    public partial class Database
    {
        /// <summary>
        /// Debugging flag.
        /// </summary>
        public static bool Trace = false;

        /// <summary>
        /// Gets this Database instance's provider invariant name, which is essentially a string that refers to the provider
        /// 's DLL (like a 'reference').
        /// </summary>
        public string ProviderInvariantName { get; }

        /// <summary>
        /// Gets this Database instance's provider specific connection string.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// Gets this Database instance's DbProviderFactory.
        /// </summary>
        public DbProviderFactory Factory { get; }

        /// <summary>
        /// How the database server identifies itself to you. Use this field for display or logging so you always
        /// know *which* database you are using. You can really tie yourself in knots if you forget that you are 
        /// looking at the wrong database.
        /// </summary>
        public string ProductName { get; private set; }

        /// <summary>
        /// The version number of the database, as reported by the server or the .NET data provider. Use this for
        /// display or logging if your servers are on different versions of the software. Sometimes you might have
        /// to use this to determine whether certain syntax is possible on that particular version, e.g. to use a
        /// a Common Table Expression on the newer version, or some convoluted and less efficient syntax as long as
        /// the older versions are still around.
        /// </summary>
        public string ProductVersion { get; private set; }
    }
}