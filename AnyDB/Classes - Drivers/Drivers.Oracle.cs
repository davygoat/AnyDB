/*
 * Oracle and DB2 treat procedures and functions differently. A function MUST be called with a DbParameter of 
 * ReturnValue direction, a procedure MUST NOT be called with a ReturnValue. We'll use the Procedures collection to 
 * decide which method to use. Oracle has an additional quirk for setting the return type of a REFCURSOR.
 * 
 * Dependencies :-
 * 
 *    Providers.Oracle          - Oracle.ManagedDataAccess.dll (local).
 *    Providers.OracleUnmanaged - Oracle.DataAccess.dll (GAC) and various installed OCI DLLs.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    [RegisterProvider("Oracle.ManagedDataAccess.Client", FactoryClass="Oracle.ManagedDataAccess.Client.OracleClientFactory", ClassNamespace="Oracle.ManagedDataAccess")]
    class OracleManaged : OracleBase
    {
        public OracleManaged()
            : base()
        {
            OracleDbTypeName = "Oracle.ManagedDataAccess.Client.OracleDbType";
        }

        [Ident(Providers.Oracle, ProviderInvariantName = "Oracle.ManagedDataAccess.Client")]
        public OracleManaged(string ConnectionString, string CurrentSchema, DbProviderFactory Factory)
            : base(ConnectionString, CurrentSchema, Factory)
        {
            OracleDbTypeName = "Oracle.ManagedDataAccess.Client.OracleDbType";
        }
    }

    [RegisterProvider("Oracle.DataAccess.Client", FactoryClass="Oracle.DataAccess.Client.OracleClientFactory", ClassNamespace="Oracle.DataAccess")]
    class OracleUnmanaged : OracleBase
    {
        public OracleUnmanaged()
            : base()
        {
            OracleDbTypeName = "Oracle.DataAccess.Client.OracleDbType";
        }

        [Ident(Providers.OracleUnmanaged, ProviderInvariantName = "Oracle.DataAccess.Client")]
        public OracleUnmanaged(string ConnectionString, string CurrentSchema, DbProviderFactory Factory)
            : base(ConnectionString, CurrentSchema, Factory)
        {
            OracleDbTypeName = "Oracle.DataAccess.Client.OracleDbType";
        }
    }

    class OracleBase : DriverBase
    {
        public OracleBase()
        {
            TimespanFormat = "{0} {1} INTERVAL '{2}' {3}";
            TimespanExpressions.Add(new Regex("(?<B>"+TOK+")\\s*(?<S>[-+])\\s*INTERVAL\\s*(?<N>"+qNT+")\\s+(?<U>"+UNIT+")S?", OPT));

            LimitFormat = "SELECT * FROM (SELECT {0}) WHERE ROWNUM <= {1}";
            LimitExpressions.Add(new Regex(">>FIXME<<"));

            // problem: could be WHERE or AND, not necessarily <=, not necessarily last condition
            //LimitExpressions.Add(new Regex("SELECT\\s+(?<Q>.+?)WHERE\\s+ROWNUM\\s*<=\\s*(?<N>"+N+")", OPT)); 

            AllowSemicolon               = false; // no! not even to end your SQL statement
            HasPrimaryKeyException       = false;
            HasPermissionDeniedException = false; // sometimes (if you have *some* privileges)

            reNotUniqueException         = new Regex(@"UNIQUE constraint \((?<NAME>.+?)\) violated",    RegexOptions.IgnoreCase);
            reForeignKeyException        = new Regex(@"integrity constraint \((?<NAME>.+?)\) violated", RegexOptions.IgnoreCase);
            reCheckConstraintException   = new Regex(@"check constraint \((?<NAME>.+?)\) violated",     RegexOptions.IgnoreCase);
            reNotNullException           = new Regex(@"cannot update \((?<NAME>.+?)\) to NULL",         RegexOptions.IgnoreCase);
            reInvalidColumnException     = new Regex(@"""(?<NAME>.+?)"": invalid identifier",           RegexOptions.IgnoreCase);
            reInvalidTableException      = new Regex(@"table or view does not exist",                   RegexOptions.IgnoreCase);
            reInvalidProcedureException  = new Regex(@"identifier '(?<NAME>.+?)' must be declared",     RegexOptions.IgnoreCase);
            rePermissionDeniedException  = new Regex(@"insufficient privileges",                        RegexOptions.IgnoreCase);
            rePrimaryKeyException        = reNotUniqueException;

            QuirkFunctionsMustHaveReturnValue = true;
        }

        public OracleBase(string ConnectionString, string CurrentSchema, DbProviderFactory Factory)
            : this()
        {
            BackgroundProcParamsNeeded(ConnectionString, () =>
            {
                using (var con = Factory.CreateConnection())
                {
                    con.ConnectionString = ConnectionString;
                    con.Open();
                    ProceduresCache[ConnectionString] = con.GetSchema("Procedures");
                    ProcParamsCache[ConnectionString] = con.GetSchema("ProcedureParameters");
                }
            });

            MetaProcedureName   = "object_name";
            MetaParameterName   = "argument_name";
            MetaOrdinalPosition = "position";
            MetaDataType        = "data_type";
            MetaDirection       = "in_out";

            OnConnect.Add("ALTER SESSION SET CURRENT_SCHEMA = " + CurrentSchema);
        }

        /*===========================================================================================================
         * 
         * Oracle passes data sets through a special type of output or return parameter of type RefCursor, and that 
         * type has to be set not through IDbDataParameter.Type, but through .OracleType. Because we can't impose a 
         * reference to the Oracle database provider (not everyone will want to use it), we have to resort to some 
         * circuitous reflection.
         */

        protected string OracleDbTypeName = null;

        override internal IDbDataParameter[] PossiblyAddRefCursor(string procedureName, IDbDataParameter[] args)
        {
            if (dtProcedures == null) return args;

            /*
             * Get the (strongly typed) value of OracleDbType.RefCursor.
             */

            var OdpAssembly = Assembly.GetAssembly(Factory.GetType());
            var OracleDbType = OdpAssembly.GetType(OracleDbTypeName);
            var OracleDbTypeDotRefCursor = Enum.Parse(OracleDbType, "RefCursor");

            /*
             * If we're calling a function, change the return value to OracleDbType.RefCursor.
             */

            int numRefCursor = 0;
            if (args.Length > 0 &&
                args[0].Direction == ParameterDirection.ReturnValue)
            {
                ((DbParameter)args[0]).ResetDbType();
                args[0].Size = 0;
                PropertyInfo pi = args[0].GetType().GetProperty("OracleDbType");
                pi.SetValue(args[0], OracleDbTypeDotRefCursor, null);
                numRefCursor = 1;
            }

            /*
             * Add any required OracleDbType.RefCursor output parameters.
             */

            int nOutRefCursor = 0;
            List<IDbDataParameter> arglst = new List<IDbDataParameter>(args);
            foreach (DataRow dr in GetParameters(procedureName))
            {
                if (dr[MetaDirection].ToString() == "OUT" &&
                    dr[MetaDataType].ToString() == "REF CURSOR")
                {
                    if (++nOutRefCursor > numRefCursor)
                    {
                        IDbDataParameter p = Factory.CreateParameter();
                        p.Direction = ParameterDirection.Output;
                        PropertyInfo pi = p.GetType().GetProperty("OracleDbType");
                        pi.SetValue(p, OracleDbTypeDotRefCursor, null);
                        arglst.Add(p);
                    }
                }
            }
            return arglst.ToArray();
        }
    }
}