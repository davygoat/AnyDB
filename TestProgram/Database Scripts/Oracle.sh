-- #!/bin/sh
-- 
-- Oracle ---------------------------------------------------------------
-- 
-- tnsnames.ora
-- 
-- 	ORCL =
-- 	  (DESCRIPTION =
-- 		(ADDRESS = (PROTOCOL = TCP)(HOST = localhost)(PORT = 1521))
-- 		(CONNECT_DATA =
-- 		  (SERVER = DEDICATED)
-- 		  (SERVICE_NAME = xe)
-- 		)
-- 	  )
--
-- First login as user SYSTEM, e.g. sqlplus system/database.
--
-- CREATE USER david IDENTIFIED BY whatever;
-- GRANT ALL PRIVILEGES TO david;
--
-- Then run the script as the new user.
--

CREATE TABLE AnyDB_numbers
(
   number_text VARCHAR(80) NOT NULL PRIMARY KEY
);
INSERT INTO AnyDB_numbers VALUES ('One');
INSERT INTO AnyDB_numbers VALUES ('Two');
INSERT INTO AnyDB_numbers VALUES ('Three');
INSERT INTO AnyDB_numbers VALUES ('Four');
INSERT INTO AnyDB_numbers VALUES ('Five');
INSERT INTO AnyDB_numbers VALUES ('OneOneOne');

CREATE TABLE AnyDB_test
(
   column_one   INTEGER     NOT NULL,
   column_two   VARCHAR(80) NOT NULL,
   column_three TIMESTAMP   NOT NULL,
   column_four  REAL        NOT NULL,
   PRIMARY KEY (column_one),
   FOREIGN KEY (column_two) REFERENCES AnyDB_numbers,
   UNIQUE      (column_two),
   CHECK       (column_four > 0)
);
show errors;

CREATE FUNCTION AnyDB_testProcedure1 (iKey INTEGER,
                                      fAdd REAL)
RETURN REAL
AS ret REAL;
BEGIN

   SELECT column_four + fAdd
   INTO   ret
   FROM   AnyDB_test
   WHERE  column_one = iKey;

   RETURN ret;

END;
/
show errors;

CREATE PROCEDURE AnyDB_testProcedure2 (iInput INT,
                                       sOutputStr OUT VARCHAR,
                                       sOutputFloat OUT REAL) AS
BEGIN

   SELECT column_two,
          column_four
   INTO   sOutputStr,
          sOutputFloat
   FROM   AnyDB_test
   WHERE  column_one = iInput;

END;
/
show errors;

CREATE FUNCTION AnyDB_testProcedure3
RETURN SYS_REFCURSOR
IS cur SYS_REFCURSOR;
BEGIN

   OPEN cur FOR 
   SELECT *
   FROM   AnyDB_test;

   RETURN cur;

END;
/
show errors;

CREATE PROCEDURE AnyDB_testProcedure4 (cur1 OUT SYS_REFCURSOR,
                                       cur2 OUT SYS_REFCURSOR) AS
BEGIN

   OPEN cur1 FOR 
   SELECT    *
   FROM      AnyDB_test
   ORDER BY  column_one ASC;

   OPEN cur2 FOR 
   SELECT    *
   FROM      AnyDB_test
   ORDER BY  column_one DESC;

END;
/
show errors;

CREATE USER test IDENTIFIED BY lemming;
GRANT CREATE SESSION TO test;
GRANT SELECT,INSERT,UPDATE,DELETE ON AnyDB_test TO test;
GRANT EXECUTE ON AnyDB_testProcedure1 TO test;
GRANT EXECUTE ON AnyDB_testProcedure2 TO test;
GRANT EXECUTE ON AnyDB_testProcedure3 TO test;
GRANT EXECUTE ON AnyDB_testProcedure4 TO test;
show errors;