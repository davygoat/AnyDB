using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnyDB
{
    public partial class Database
    {
        static Regex reDriver = new Regex(@"(?<TAG>Driver)=(?<NAME>[^;]+)");

        static string RewriteDriver(string ConnectionString, params string[] driverNames)
        {
            /*
             * Extract the driver name from the connection string. If there isn't one, return the connection string as 
             * it is.
             */

            Match m = reDriver.Match(ConnectionString);
            if (m == null || m.Groups.Count != 3) return ConnectionString;

            string tag = m.Groups["TAG"].Value;
            string csdrv = m.Groups["NAME"].Value;

            /*
             * Query the registry to get a list of installed ODBC drivers. If the driver name already matches the 
             * installed name, return the unaltered connection string.
             */

            List<string> installed = GetSystemDriverList();
            if (installed.Find(d => d.ToLower() != csdrv.ToLower()) == null) return ConnectionString;

            /*
             * Find out which of the supplied driver names is in the list of installed drivers.
             */

            if (driverNames.Length > 0)
            {
                foreach (string cdrv in driverNames)
                {
                    if (installed.Find(d => d.ToLower() == cdrv.ToLower()) != null)
                    {
                        csdrv = cdrv;
                        break;
                    }
                }
            }
            else
            {
                foreach (string d in installed)
                {
                    if (d.ToLower().StartsWith(csdrv.ToLower()))
                    {
                        csdrv = d;
                        break;
                    }
                }
            }

            /*
             * If not found, return the query string unchanged.
             */

            if (installed.Find(d => d.ToLower() == csdrv.ToLower()) == null) return ConnectionString;

            /*
             * Driver found, so modify the query string to use the installed driver's full name.
             */

            csdrv = tag + "={" + csdrv + "}";
            return reDriver.Replace(ConnectionString, csdrv);
        }
    }
}