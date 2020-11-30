-- vim:set ts=4:

USE AnyDB;

-- SQL Server ---------------------------------------------------------
--
-- TransactSQL stored procedures are actually pretty straightforward,
-- particularly the way they return multiple queries. You just do the
-- SELECTs, and don't have to mess around with refcursors or anything 
-- like that.
--

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

CREATE TABLE AnyDB_Test
(
	column_one   INTEGER     NOT NULL,
	column_two   VARCHAR(80) NOT NULL,
	column_three DATETIME2   NOT NULL,
	column_four  FLOAT       NOT NULL,
	PRIMARY KEY (column_one),
	FOREIGN KEY (column_two) REFERENCES AnyDB_numbers,
	UNIQUE      (column_two),
	CHECK       (column_four > 0)
)
GO

CREATE PROCEDURE AnyDB_testProcedure1(@iKey INT,
	                                  @fAdd FLOAT) AS
BEGIN
	SELECT column_four + @fAdd
	FROM   AnyDB_test
	WHERE  column_one = @iKey;
END
GO

CREATE PROCEDURE AnyDB_testProcedure2(@iInput       INT,
	                                  @sOutputStr   VARCHAR(80) OUTPUT,
	                                  @sOutputFloat FLOAT OUTPUT) AS
BEGIN

	SELECT @sOutputStr = column_two,
	       @sOutputFloat = column_four
	FROM   AnyDB_test
	WHERE  column_one = @iInput;

END
GO

CREATE PROCEDURE AnyDB_testProcedure3 AS
BEGIN
   SELECT *
   FROM   AnyDB_test;
END
GO

CREATE PROCEDURE AnyDB_testProcedure4 AS
BEGIN

	SELECT   *
	FROM     AnyDB_test
	ORDER BY column_one ASC;
	
	SELECT   *
	FROM     AnyDB_test
	ORDER BY column_one DESC;
	
END
GO

--CREATE LOGIN test WITH PASSWORD='lemming', CHECK_POLICY=OFF;
CREATE USER test FOR LOGIN test;
GRANT SELECT,INSERT,UPDATE,DELETE ON AnyDB_test to test;
GRANT EXECUTE ON AnyDB_testProcedure1 TO test;
GRANT EXECUTE ON AnyDB_testProcedure2 TO test;
GRANT EXECUTE ON AnyDB_testProcedure3 TO test;
GRANT EXECUTE ON AnyDB_testProcedure4 TO test;
GO