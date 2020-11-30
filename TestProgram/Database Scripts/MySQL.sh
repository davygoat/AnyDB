#!/bin/sh 
# vim:set ts=4:

## MySQL ##############################################################
#
# grep bind-address /etc/mysql/my.conf         # must listen on 0.0.0.0
#

mysql -v -u root --password=database <<-EOD
	CREATE USER david@localhost;
	GRANT ALL PRIVILEGES ON *.* TO $USER@localhost WITH GRANT OPTION;
EOD

mysql -v <<-EOD

	CREATE DATABASE AnyDB;

	USE AnyDB;

	CREATE TABLE AnyDB_numbers
	(
	   number_text  VARCHAR(80) NOT NULL,
	   PRIMARY KEY (number_text)
	);

	INSERT INTO AnyDB_numbers VALUES ('One');
	INSERT INTO AnyDB_numbers VALUES ('Two');
	INSERT INTO AnyDB_numbers VALUES ('Three');
	INSERT INTO AnyDB_numbers VALUES ('Four');
	INSERT INTO AnyDB_numbers VALUES ('Five');
	INSERT INTO AnyDB_numbers VALUES ('Six');
	INSERT INTO AnyDB_numbers VALUES ('Seven');
	INSERT INTO AnyDB_numbers VALUES ('Eight');
	INSERT INTO AnyDB_numbers VALUES ('Nine');
	INSERT INTO AnyDB_numbers VALUES ('Ten');
	INSERT INTO AnyDB_numbers VALUES ('OneOneOne');

	CREATE TABLE AnyDB_test
	(
	   column_one   INTEGER      NOT NULL,
	   column_two   VARCHAR(80)  NOT NULL,
	   column_three DATETIME(6)  NOT NULL,
	   column_four  REAL         NOT NULL,
	   PRIMARY KEY (column_one),
	   FOREIGN KEY (column_two) REFERENCES AnyDB_numbers (number_text),
	   UNIQUE      (column_two),
	   CHECK       (column_four > 0)
	);

	DELIMITER //

	CREATE PROCEDURE AnyDB_testProcedure1 (iKey INTEGER,
	                                       fAdd DOUBLE)
	BEGIN

	   SELECT column_four + fAdd
	   FROM   AnyDB_test
	   WHERE  column_one = iKey;

	END//

	CREATE PROCEDURE AnyDB_testProcedure2 (IN  iInput  INTEGER,
	                                       OUT sOutput VARCHAR(80),
	                                       OUT fOutput DOUBLE)
	BEGIN

	   SELECT column_two,
	          column_four
	   INTO   sOutput,
	          fOutput
	   FROM   AnyDB_test
	   WHERE  column_one = iInput;

	END//

	CREATE PROCEDURE AnyDB_testProcedure3 ()
	BEGIN

	   SELECT *
	   FROM   AnyDB_test;

	END//

	CREATE PROCEDURE AnyDB_testProcedure4 ()
	BEGIN

	   SELECT   *
	   FROM     AnyDB_test
	   ORDER BY column_one ASC;

	   SELECT   *
	   FROM     AnyDB_test
	   ORDER BY column_one DESC;

	END//

	DELIMITER ;

	CREATE USER test IDENTIFIED BY 'lemming';

	GRANT SELECT,INSERT,UPDATE,DELETE ON TABLE AnyDB_test TO test;
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure1 TO test;
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure2 TO test;
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure3 TO test;
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure4 TO test;

	-- Make sure the .NET provider can see the procedure parameters.
	GRANT SELECT ON mysql.proc TO test;

EOD
