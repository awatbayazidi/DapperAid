# DapperAid
DapperAidは、[Dapper](https://github.com/StackExchange/Dapper)によるデータベースのCRUD操作を支援するライブラリです。
- データベースのSelect, Insert, Update, Deleteの操作を、IDbConnection / IDbTransactionの拡張メソッドとして提供します。
- 実行SQLは、POCOオブジェクトに付与した属性に基づき自動生成されます。
- 実行SQLのWhere条件は、POCOオブジェクトのKey項目の値、または、ラムダ式（式木）の記述をもとに生成されます。
- 属性付与/メソッド引数指定により、以下のような内容をカスタマイズできます。
  - Select時のorderby, offset / limit条件, groupby要否, distinct指定など
  - Select, Insert, Update対象とするカラムの限定
  - Insert時 / Update時の設定値(設定せずDBデフォルト値に任せることも可)
  - Insert時のIdentity/AutoIncrement自動採番値把握(各DBMS対応)
- その他オプション機能（使用任意） :
  - 簡易コードファースト(POCO定義内容からCreateTableのSQLを生成)
  - SQL実行ログ取得

DapperAid is a library that assists database CRUD operation using [Dapper](https://github.com/StackExchange/Dapper).
- Provides Select, Insert, Update and Delete operations of the database as extension methods of IDbConnection / IDbTransaction.
- Execution SQL is automatically generated based on the attribute given to the POCO object.
- The execution SQL Where condition is generated based on the value of the key item of POCO object or the description of lambda expression (expression tree).
- The following can be customized by attribute assignment / method argument specification.
  - Order-by, offset / limit conditions, need of group-by, specification of distinct and so on at the time of Select
  - Select / Insert / Update only specific columns
  - Setting value at Insert / Update (It is also possible to leave it to the DB default value without setting)
  - Retrieve inserted Identity / AutoIncrement value (for each DBMS)
- Other extra features (use is optional) :
  - A little code-first (Generate Create-Table SQL from POCO definition contents)
  - SQL execution log acquisition

# Installation
from NuGet  https://www.nuget.org/packages/DapperAid
```
PM> Install-Package DapperAid
```
```
> dotnet add package DapperAid
```

# Examples
## Sample table
```cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DapperAid.DataAnnotations;

[Table("Members")]
[SelectSql(DefaultOtherClauses = "order by Id")]
class Member
{
    [Key]
    [InsertSql(false, RetrieveInsertedId = true)]
    [DapperAid.Ddl.DDL("INTEGER")] // (for extra feature, generating Create-Table-SQL as SQLite Identity Column)
    public int Id { get; set; }

    public string Name { get; set; }

    [Column("Phone_No")]
    public string Tel { get; set; }

    [InsertSql("CURRENT_TIMESTAMP"), UpdateSql(false)]
    public DateTime? CreatedAt { get; set; }

    [InsertSql("CURRENT_TIMESTAMP"), UpdateSql("CURRENT_TIMESTAMP")]
    private DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public string TemporaryPassword { get; set; }
}
```
- Members declared as "Property" are subject to automatic SQL generation / execution.
- See [About Table Attributes](#attributes) for attribute details.

## Operation example
```cs
using System.Collections.Generic;
using System.Data;
using DapperAid;

void OperationExample() {
```
### Initializing
```cs
  QueryBuilder.DefaultInstance = new QueryBuilder.SQLite(); // (example for SQLite)
```
- <a name="querybuilders"></a>Set `DefaultInstance` corresponding to your DBMS from below.
  - new QueryBuilder.Oracle()
  - new QueryBuilder.MySql()
  - new QueryBuilder.Postgres()
  - new QueryBuilder.SQLite()
  - new QueryBuilder.SqlServer()
  - new QueryBuilder.MsAccess() // can also be used for SQLServerCE
  - new QueryBuilder.DB2()

  These instance generates appropriate SQL statement for your DBMS.  
  (You can also customize the QueryBuilder class as needed)
```cs
  using (IDbConnection connection = GetYourDbConnection()) 
  {   
```
### `Select(by Key [, targetColumns])` -> returns one row or null
```cs
    Member select1 = connection.Select(
        () => new Member { Id = 1 });
    // -> select "Id", "Name", Phone_No as "Tel", "CreatedAt", "UpdatedAt" from Members where "Id"=@Id(=1)

    Member select2 = connection.Select(
        () => new Member { Id = 1 },
        r => new { r.Id, r.Name });
    // -> select "Id", "Name" from Members where "Id"=@Id
```
### `Select<T>([ where[, targetColumns[, otherClauses]]])` -> returns list&lt;T&gt;
```cs
    IReadOnlyList<Member> list1 = connection.Select<Member>();
    // -> select (all columns) from Members order by Id

    IReadOnlyList<Member> list2 = connection.Select<Member>(
        r => r.Name == "TEST");
    // -> select (all columns) from Members where "Name"=@Name(="TEST") order by Id

    IReadOnlyList<Member> list3 = connection.Select<Member>(
        r => r.Name != "TEST", 
        r => new { r.Id, r.Name });
    // -> select "Id", "Name" from Members where "Name"<>@Name order by Id

    IReadOnlyList<Member> list4 = connection.Select<Member>(
        r => r.Tel != null,
        r => new { r.Id, r.Name },
        "ORDER BY Name LIMIT 5 OFFSET 10");
    // -> select "Id", "Name" from Members where Phone_No is not null
    //           ORDER BY Name LIMIT 5 OFFSET 10
```
### `Count<T>([where])` -> returns the number of rows
```cs
    ulong count1 = connection.Count<Member>();
    // -> select count(*) from Members

    ulong count2 = connection.Count<Member>(
        r => (r.Id >= 3 && r.Id <= 9));
    // -> select count(*) from Members where "Id">=@Id(=3) and "Id"<=@P01(=9)
```
### `Insert(record[, targetColumns[, retrieveInsertedId:bool]])` -> returns 1(inserted row)
```cs
    var rec1 = new Member { Name = "InsertTest", Tel = "177" };
    int insert1 = connection.Insert(rec1);
    // -> insert into Members("Name", Phone_No, "CreatedAt", "UpdatedAt")  
    //                values (@Name, @Tel, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)

    var rec2 = new Member { Name = "ParticularColumnOnly1", CreatedAt = null };
    int insert2 = connection.Insert(rec2,
        r => new { r.Name, r.CreatedAt });
    // -> insert into Members("Name", "CreatedAt") values (@Name, @CreatedAt(=null))

    var rec3 = new Member { Name = "IdentityTest", Tel = "7777" };
    int insert3 = connection.Insert(rec3, 
        retrieveInsertedId: true);
    // -> insert into Members("Name", Phone_No, "CreatedAt", "UpdatedAt")
    //    values (@Name, @Tel, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) ; select LAST_INSERT_ROWID()
    Trace.WriteLine("insertedID=" + rec3.Id); // The value assigned to the "Id" column is set
```
- Note: In this example, the [[InsertSql](#insertsqlattribute)] attribute is specified that  
  the "Id" column is autoincrement and obtains the registered value.

### `Insert(specifiedColumnValue)` -> returns 1(inserted row)
```cs
    int insertX = connection.Insert(
        () => new Member { Id = 888, Name = "ParticularColumnOnly2" });
    // -> insert into Members("Id", "Name") values (@Id, @Name)
```

### `InsertRows(records)` -> returns the number of inserted rows
```cs
    int insertMulti = connection.InsertRows(new[] {
        new Member { Name = "MultiInsert1", Tel = null },
        new Member { Name = "MultiInsert2", Tel = "999-999-9999" },
        new Member { Name = "MultiInsert3", Tel = "88-8888-8888" },
    });
    // -> execute insert 3 rows
```
- Note: PostgreSQL, MySQL and SQLite are inserted faster by Bulk-Insert.

### `Update(record[, targetColumns])` -> returns the number of updated rows
```cs
    var rec1 = new Member { Id = 555, ... };
    int update1 = connection.Update(rec1);
    // update Members set "Name"=@Name, Phone_No=@Tel, "UpdatedAt"=CURRENT_TIMESTAMP where "Id"=@Id

    var rec2 = new Member { Id = 666, Tel = "123-456-7890" };
    int update2 = connection.Update(rec2, r => new { r.Tel });
    // -> update Members set Phone_No=@Tel where "Id"=@Id
```
### `Update(specifiedColumnValue, where)` -> returns the number of updated rows
```cs
    int update3 = connection.Update(
        () => new Member { Name = "updateName" },
        r => r.Tel == "55555-5-5555");
    // -> update Members set "Name"=@Name where Phone_No=@Tel
```
### `Delete(record)` -> returns the number of deleted rows
```cs
    var delRec = new Member { Id = 999, ... };
    int delete1 = connection.Delete(delRec);
    // -> delete from Members where "Id"=@Id
```
### `Delete<T>(where)` -> returns the number of deleted rows
```cs
    int delete2 = connection.Delete<Member>(
        r => r.Name == null);
    // -> delete from Members where "Name" is null
```
### `Truncate<T>()`
```cs
    connection.Truncate<Member>();
    // -> truncate table Members 
    //    (For DBMS without "truncate" syntax, execute delete instead)
```
### Extra : `CreateTable<T>()`
```cs
using DapperAid.Ddl;

    var createTableSql = DDLAttribute.GenerateCreateSQL<Member>();
    // ->  create table Members
    //     (
    //      "Id" INTEGER,
    //      "Name",
    //      Phone_No,
    //      "CreatedAt",
    //      "UpdatedAt",
    //      primary key( "Id")
    //     )
    connection.Execute(createTableSql);
```
- Note: If you use this feature, you should describe [[DDL](#ddlattribute)] attribute in each column  
        and specify database column types, constraints, default values, etc.

### Extra2 : Loggable DB Connection
```cs
using System.Data;
using System.Data.SQLite; // (example for SQLite)
using DapperAid.DbAccess;

IDbConnection GetSqliteDbConnection()
{
    // Prepare a normal DB connection 
    var connectionSb = new SQLiteConnectionStringBuilder { DataSource = ":memory:" };
    var conn = new SQLiteConnection(connectionSb.ToString());
    conn.Open();

    // Set into LoggableDbConnection object
    return new LoggableDbConnection(conn,
        (Exception ex, DbCommand cmd) =>
        {   // Write Error Log
            Trace.WriteLine(ex.ToString() + (cmd != null ? ":" + cmd.CommandText : null));
        },
        (string resultSummary, long mSec, DbCommand cmd) =>
        {   // Write SQL Execution Trace Log
            Trace.WriteLine(resultSummary + "(" + mSec + "ms)" + (cmd != null ? ":" + cmd.CommandText : null));
        });
}
```
- Note: The log method specified in the argument is called when SQL is executed / error occurs.  
Implement it to be logged.


# About Where Clause
Expression trees in LambdaExpression is converted to SQL search condition.  
Condition values are bound to parameters.  
## Comparison Operator
```cs
    int? val1 = 100; // (bound to @IntCol)
    .Select<T>(t => t.IntCol == val1); // -> where "IntCol"=@IntCol
    .Select<T>(t => t.IntCol != val1); // -> where "IntCol"<>@IntCol
    .Select<T>(t => t.IntCol < val1); // -> where "IntCol"<@IntCol
    .Select<T>(t => t.IntCol > val1); // -> where "IntCol">@IntCol
    .Select<T>(t => t.IntCol <= val1); // -> where "IntCol"<=@IntCol
    .Select<T>(t => t.IntCol >= val1); // -> where "IntCol">=@IntCol

    // If the value is null, SQL is also generated as "is"
    int? val2 = null; 
    .Select<T>(t => t.IntCol == val2); // -> where "IntCol" is null
    .Select<T>(t => t.IntCol != val2); // -> where "IntCol" is not null

    // can also compare columns and columns.
    .Select<T>(t => t.IntCol == t.OtherCol); // -> where "IntCol"="OtherCol"
```
SQL-specific comparison operators `in`, `like`, and `between` are also supported.
```cs
using DapperAid; // uses "ToSql" static class

    string[] inValues = {"111", "222", "333"}; // (bound to @TextCol)
    .Select<T>(t => t.TextCol == ToSql.In(inValues)); // -> where "TextCol" in(@TextCol)
    
    string likeValue = "%test%"; // (bound to @TextCol)
    .Select<T>(t => t.TextCol == ToSql.Like(likeValue)); // -> where "TextCol" like @TextCol

    int b1 = 1; // (bound to @IntCol)
    int b2 = 99; // (bound to @P01)
    .Select<T>(t => t.IntCol == ToSql.Between(b1, b2)); // -> where "IntCol" between @IntCol and @P01

    // when "!=" is used, SQL is also generated as "not"
    .Select<T>(t => t.TextCol != ToSql.In(inValues)); // -> where "TextCol" not in(@TextCol)
```
## Logical Operator
Supports And(`&&`), Or(`||`), Not(`!`).
```cs
    .Select<T>(t => t.TextCol == "111" && t.IntCol < 200);
    // -> where "TextCol"=@TextCol and "IntCol"<@IntCol

    .Select<T>(t => t.TextCol == "111" || t.IntCol < 200);
    // -> where ("TextCol"=@TextCol) or ("IntCol"<@IntCol)

    .Select<T>(t => !(t.TextCol == "111" || t.IntCol < 200));
    // -> where not(("TextCol"=@TextCol) or ("IntCol"<@IntCol))
```
It can also be combined with the condition judgment not based on SQL.
```cs
    // The part where the boolean value is found in advance is not converted to SQL, and is omitted
    string text1 = "111";
    .Select<T>(t => text1 == null || t.TextCol == text1); // -> where "TextCol"=@TextCol
    .Select<T>(t => text1 != null && t.TextCol == text1); // -> where "TextCol"=@TextCol

    // If the result is determined only by the left side, SQL is not generated
    string text2 = null;
    .Select<T>(t => text2 == null || t.TextCol == text2); // -> where true
    .Select<T>(t => text2 != null && t.TextCol == text2); // -> where false
```

## SQL direct description
You can also describe conditional expressions and subqueries directly.
```cs
using DapperAid; // uses "ToSql" static class

    .Select<T>(t => t.TextCol == ToSql.In<string>("select text from otherTable where..."));
    // --> where "TextCol" in(select text from otherTable where...)

    .Select<T>(t => ToSql.Eval("ABS(IntCol) < 5"));
    // --> where (ABS(IntCol) < 5)

    .Select<T>(t => ToSql.Eval("exists(select * from otherTable where...)"));
    // --> where (exists(select * from otherTable where...))
```

# <a name="attributes"></a>About Table Attributes
```cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DapperAid.DataAnnotations;
using DapperAid.Ddl; // (for extra feature)
```
## for Class
### `[Table]` : apply if tablename != classname or you want to customize the from clause
```cs
    [Table("TABLE_NAME")] // specify table name
    // -> select .... from TABLE_NAME

    [Table("TABLE_NAME", Schema = "SCHEMA_NAME")] // specify schema
    // -> select .... from SCHEMA_NAME.TABLE_NAME

    [Table("TABLE1 T1 INNER JOIN TABLE2 T2 ON T1.ID=T2.ID")] // join
    // -> select .... from TABLE1 T1 INNER JOIN TABLE2 T2 ON T1.ID=T2.ID
    // Note: Also specify the acquisition source table in the column definition
```
### `[SelectSql]` : apply if you want to customize select statement
```cs
    [SelectSql(Beginning = "SELECT DISTINCT")] // customize the beginning of select sql
    // -> SELECT DISTINCT ... from ....

    [SelectSql(GroupByKey = true)] // generate group-by clause
    // -> select ... from ... where ... GROUP BY (colums with [Key] attributes)

    [SelectSql(DefaultOtherClauses = "ORDER BY NAME NULLS LAST")] // append the end of select sql by default
    // -> select ... from ... where ... ORDER BY NAME NULLS LAST 
    //    (when {otherClauses} is not specified)
```
### (for extra feature) `[DDL]` : apply if you want to specify a table constraint of DDL
```cs
    [DDL("FOREIGN KEY (C1,C2) REFERENCES MASTERTBL(C1,C2)")] // specify FK
    // -> create table ...(
    //     ...,
    //     primary key ...,
    //     FOREIGN KEY (C1,C2) REFERENCES MASTERTBL(C1,C2)
    //    )
```
## for Properties
### `[Column]` : apply if columnname != propertyname or you want to customize the column values to retrieve
```cs
    [Column("COLUMN_NAME")] // specify column name
    public string ColumnName { get; set; }
    //   -> select ... COLUMN_NAME as "ColumnName", ... 

    [Column("T1.CODE")] // specify table alias and column name
    public string T1Code { get; set; }
    //   -> select ... T1.CODE as "T1Code", ... 

    [Column("MONTH(DateOfBirth)")] // customize value
    public int BirthMonth { get; set; }
    //   -> select ... MONTH(DateOfBirth) as "BirthMonth", ... 

    [Column("COUNT(*)")] // tally value
    public int TotalCount { get; set; }
    //   -> select ... COUNT(*) as "TotalCount", ... 
```
### `[Key]` : apply if you want to update/delete by record-object, or use [Select(GroupByKey = true)]
```cs
    [Key]
    // -> update/delete .... where (columns with [Key] attributes)=@(bindvalue)
    
    // when [SelectSql(GroupByKey = true)] is applied to the class
    // -> select .... where ... GROUP BY (colums with [Key] attributes)
```
- Note: You can also specify [Key] for multiple columns (as a composite key)

### <a name="insertsqlattribute">`[InsertSql]`</a> : apply if you want to modify the insert value
```cs
    [InsertSql("CURRENT_TIMESTAMP")] // Specify the value to set with SQL instead of bind value
    public DateTime CreatedAt { get; set; }
    // -> insert into ...(..., "CreatedAt", ...) values(..., CURRENT_TIMESTAMP, ...)

    [InsertSql("date(@DateOfBirth)")] // Edit bind value with SQL
    public DateTime DateOfBirth
    // -> insert into ...(..., "BirtyDay", ...) values(..., date(@DateOfBirth), ...)

    // Do not set column (DB default value is set)
    [InsertSql(false)] 

    // set DB default value(ID etc.), and obtain value on Insert(retrieveInsertedId: true)
    [InsertSql(false, RetrieveInsertedId = true)] 

    // set sequence value and obtain (works only PostgreSQL, Oracle)
    [InsertSql("nextval(SEQUENCENAME)", RetrieveInsertedId = true)]
```
- Note: If you call Insert() with the target column explicitly specified,  
  The bind value is set instead of the value by this attribute.

### `[UpdateSql]` : apply if you want to modify the value on update
```cs
    [UpdateSql("CURRENT_TIMESTAMP")] : // Specify the value to set with SQL instead of bind value
    public DateTime UpdatedAt { get; set; }
    // -> update ... set ..., "UpdatedAt"=CURRENT_TIMESTAMP, ....

    [UpdateSql("COALESCE(@DCnt, 0)")] // Edit bind value with SQL
    public Int? DCnt { get; set; }
    // -> update ... set ..., "DCnt"=COALESCE(@DCnt, 0), ...

    // Do not set column (not be updated)
    [UpdateSql(false)] 
```
- Note: If you call Update() with the target column explicitly specified,  
  The bind value is set instead of the value by this attribute.


### `[NotMapped]` : Denotes that a property should be excluded from database mapping
```cs
    [NotMapped] // Do not select, insert, update 
    public Object NotMappedProperty { get; set; }
```
### (for extra feature) <a name="ddlattribute">`[DDL]`</a> : apply if you want to specify database column types, constraints, default values, etc.
```cs
    [DDL("NUMERIC(5) DEFAULT 0 NOT NULL")]
    public int Value { get; set; }
    // -> create table ...(
    //      :
    //     Value NUMERIC(5) DEFAULT 0 NOT NULL,
    //      : 
```
# Misc.
## When you want to execute a query during transaction.
- use extension methods in `IDbTransaction`. 
It provides the same method as the `IDbConnection` extension method.
## When you want to execute a asynchronus query.
- use ～～Async methods.
## When not using as an extension method.
- use `QueryRunner` class.
It Provides almost the same content as an extension method as an instance method.
## When you want to use only the SQL generation function.
- Use the [`QueryBuilder`](#querybuilders) class appropriate for your DBMS.

# License
[MIT License](http://opensource.org/licenses/MIT).

# About Author
hnx8(H.Takahashi) is a software developer in Japan.  
(I wrote English sentences relying on Google translation. Please let me know if you find a strange expression)