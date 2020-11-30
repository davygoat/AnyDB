/*
 * Headers taken from "Le Gipoulateur" for reference.
 */

/*************************************************************************************************
 * 
 * DatabaseTable_PostgreSQL.cs
 * 
 * Code for dealing with a PostgreSQL database table and its equivalent XML document.
 * 
 * - Postgres converts unquoted identifiers to lowercase, against the standard (should be upper
 *   case, but that's just ugly).
 * - Quoted identifiers are strictly case sensitive. You cannot used mixed case idents unquoted.
 * - Schemas are fully supported by Postgres, but they aren't visible in Npgsql. You can, however,
 *   make use of the SearchPath connection string parameter to make them visible.
 * 
 * Required DLLs:
 * 
 * - Npgsql.dll
 * - Mono.Security.dll
 * 
 * A few psql metacommands:
 * 
 * \c  -- CONNECT, ATTACH or USE
 * \d  -- SHOW TABLE or DESCRIBE
 * \dn -- SHOW SCHEMAS
 * \dp -- SHOW GRANTS or SHOW PRIVILEGES
 * \l  -- SHOW DATABASES
 * \q  -- QUIT
 * 
 * SHOW SEARCH_PATH;                   -- show current schema path
 * SET SEARCH_PATH TO public,another;  -- include 'another' in the schema path
 * 
 * Admin user:
 * 
 * - postgres
 */

/*************************************************************************************************
 * 
 * DatabaseTable_MySQL.cs
 * 
 * Code for dealing with a MySQL database table and its equivalent XML document. The MySQL driver
 * is called Connector/Net, and comes with MySQL. Because MariaDB is a "drop-in replacement" for
 * MySQL, the .NET provider also works with MariaDB.
 *
 * - MySQL on Windows stores table names in lowercase. This means you can't use a plug-in that
 *   was built for another database that does not lowercase its table names. This can be fixed
 *   with by setting lower_case_table_names to 2. But other databases (e.g. Firebird, Postgres)
 *   have similar problems.
 *   
 * - MySQL uses backticks for quoting identifiers such as table names. Backticks will be used by
 *   default, as per MySQL tradition. If you don't want to use MySQL backticks, you can use the 
 *   <QuotedIdents> option to override the default. (But the global SQL mode must include ANSI_QUOTES
 *   for this to work.)
 *   
 * - REAL is DOUBLE, but there is a REAL_AS_FLOAT mode for ANSI compliance. MySql.Data.MySqlClient 
 *   seems to return the underlying data type correctly in both modes. The DESCRIBE command also 
 *   reports the correct size float.
 *   
 * - DATETIME defaults to seconds precision (the time_t heritage), and drops any subsecond digits
 *   you provide. However, both implementations (Oracle MySQL and MariaDB) now have DATETIME(n) 
 *   where n is anything between 0 (seconds) and 6 (microseconds). Beware, precision is *fixed*, 
 *   any extra digits will get truncated/zeroed (e.g. if n is 3, then .123987 becomes .123000).
 *   
 * - MySQL treats schemas as syntactical sugar for separate tables. Hence the internal confusion
 *   between catalog and schema.
 *   
 * Required DLLs:
 * 
 * - MySql.Data.MySqlClient.dll (installed globally)
 * 
 * Useful commands:
 * 
 * - SHOW DATABASES;
 * - SHOW TABLES;
 * - DESCRIBE table;
 * 
 * Admin user:
 * 
 * - root
 */

/*************************************************************************************************
 * 
 * DatabaseTable_Firebird.cs
 * 
 * Code for dealing with a Firebird database table and its equivalent XML document.
 * 
 * - Tested with "NETProvider-3.1.1.0-NET35.7z" from SourceForge, which still works with .NET 2.0.
 * 
 * - Later versions require .NET 4.5 or 4.0, which won't load in Le Gipoulateur as a .NET 2.0 app.
 * 
 * - Firebird stores non-quoted identifiers in uppercase, as per SQL standard. This leads to some
 *   really 1980s looking XML, and more or less forces you to use underscores.
 *   
 * - The DATETIME format accepts up to four subsecond digits, 1/10 millisecond. But not more.
 * 
 * - As with MySQL, there is a CREATE SCHEMA command, but it's a synonym for CREATE DATABASE. But
 *   unlike MySQL, Firebird doesn't support the dotted notation to refer to a table in another
 *   schema or database. Because the pretend schemas are in separate databases, they don't show
 *   up in the DbProviderFactory collections.
 * 
 * Required DLLs:
 * 
 * - FirebirdSql.Data.FirebirdClient.dll
 * 
 * Useful commands:
 * 
 * - CONNECT 'c:\dave\test.fdb' USER 'username' PASSWORD 'password';
 * - SHOW TABLES;
 * - SHOW TABLE table-name;
 * - SHOW GRANTS;
 * - QUIT; (requires semicolon)
 * - There is no SHOW DATABASES (databases are not stored in one place)
 * 
 * Admin user:
 * 
 * - sysdba
 */

/*************************************************************************************************
 * 
 * DatabaseTable_SQLite.cs
 * 
 * Code for dealing with a SQLite database table and its equivalent XML document.
 * 
 * - Must have the correct DLLs for your machine (e.g. x64 on my Windows 7 laptop).
 * - Tested with sqlite-netFx20-static-binary-bundle-x64-2005-1.0.93.0.zip download.
 *   "Precompiled Statically-Linked Binaries for 64-bit Windows (.NET Framework 
 *   2.0 SP2)"
 * - Application config should not need editing because it's done programmatically.
 * - SQLite schemas seem to be less consistent. Perhaps not relevant to SQLite users.
 * 
 * Required DLLs:
 * 
 * - System.Data.SQLite.dll
 * - Depending on the bundle or package you use, there may also be satellite assemblies.
 * 
 * Useful commands:
 * 
 * - .open FILENAME (or specify database name on command line)
 * - .tables
 * - .schema [TABLE-NAME]
 * - .quit
 * 
 * Admin user:
 * 
 * - Not applicable.
 */

