﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using Dapper;
using DapperAid.Helpers;

namespace DapperAid
{
    partial class QueryBuilder
    {
        /// <summary>
        /// MySQL系のSQL/パラメータを組み立てるクラスです。
        /// </summary>
        public class MySql : QueryBuilder
        {
            /// <summary>テーブル名/カラム名のエスケープに使用する文字</summary>
            protected readonly string EscapeMark;

            /// <summary>一括Insert等のSQLの最大文字列長</summary>
            protected readonly int SqlMaxLength;

            /// <summary>
            /// インスタンスを初期化します。
            /// </summary>
            /// <param name="isAnsiMode">ANSI_MODEではない場合、明示的にfalseを指定</param>
            /// <param name="sqlMaxLength">一括InsertのSQLの最大文字列長、既定では16MB。大量データの一括Insertを行う際はmax_allowed_packetの指定に応じた値を設定</param>
            public MySql(bool isAnsiMode = true, int sqlMaxLength = 16000000)
            {
                EscapeMark = (isAnsiMode ? "\"" : "`");
                SqlMaxLength = sqlMaxLength;
            }

            /// <summary>SQL識別子（テーブル名/カラム名等）をエスケープします。MySQL系では「"」または「`]を使用します。</summary>
            public override string EscapeIdentifier(string identifier)
            {
                return EscapeMark + identifier.Replace(EscapeMark, EscapeMark + EscapeMark) + EscapeMark;
            }

            /// <summary>自動連番値を取得するSQL句として、セミコロンで区切った別のSQL文を付加します。</summary>
            protected override string GetInsertedIdReturningSql<T>(TableInfo.Column column)
            {
                return "; select LAST_INSERT_ID()";
            }

            /// <summary>
            /// 標準的な一括InsertのSQLを用いて、指定されたテーブルにレコードを一括挿入します。
            /// </summary>
            public override int InsertRows<T>(IEnumerable<T> data, Expression<System.Func<T, dynamic>> targetColumns, IDbConnection connection, IDbTransaction transaction, int? timeout = null)
            {
                var ret = 0;
                foreach (var sql in BulkInsertHelper.BuildBulkInsert(this, data, targetColumns, Value2SqlLiteral, SqlMaxLength))
                {
                    ret += connection.Execute(sql, null, transaction, timeout);
                }
                return ret;
            }


            /// <summary>
            /// 引数で指定された値をMySQLにおけるSQLリテラル値表記へと変換します（主に一括Insert用）
            /// </summary>
            /// <param name="value">エスケープ前の値</param>
            /// <returns>SQLリテラル値表記</returns>
            public static string Value2SqlLiteral(object value)
            {
                if (value == null || value is System.DBNull) { return "null"; }
                if (value is string)
                {
                    // MySQLの仕様に基づき文字列をエスケープ
                    // NO_BACKSLASH_ESCAPES を on にしている環境のことはひとまず考慮しない。
                    var sb = new StringBuilder();
                    sb.Append("'");
                    foreach (var ch in (value as string))
                    {
                        switch (ch)
                        {
                            case '\u0000': sb.Append(@"\0"); break;
                            case '\'': sb.Append(@"\'"); break;
                            case '\"': sb.Append(@"\" + "\""); break;
                            case '\b': sb.Append(@"\b"); break;
                            case '\n': sb.Append(@"\n"); break;
                            case '\r': sb.Append(@"\r"); break;
                            case '\t': sb.Append(@"\t"); break;
                            case '\u001A': sb.Append(@"\z"); break;
                            case '\\': sb.Append(@"\\"); break;
                            default: sb.Append(ch); break;
                        }
                    }
                    sb.Append("'");
                    return sb.ToString();
                }
                if (value is bool) { return ((bool)value ? "TRUE" : "FALSE"); }
                if (value is DateTime) { return "timestamp '" + ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss") + "'"; }
                if (value is Enum) { return ((Enum)value).ToString("d"); }
                return value.ToString();
            }
        }
    }
}