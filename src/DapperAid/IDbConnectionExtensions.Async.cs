﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DapperAid
{
    // Dapperを用いたDB操作をIDbConnectionの拡張メソッドとして提供します。
    // （非同期実行メソッドをこのファイルに記述）
    public static partial class IDbConnectionExtensions
    {
        /// <summary>
        /// 指定されたテーブルのレコード数を非同期で取得します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="where">レコード絞り込み条件（絞り込みを行わず全件対象とする場合はnull）</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        /// <returns>レコード数</returns>
        public static Task<ulong> CountAsync<T>(this IDbConnection connection, Expression<Func<T, bool>> where = null, int? timeout = null)
        {
            return new QueryRunner(connection, timeout).CountAsync(where);
        }

        /// <summary>
        /// 指定されたテーブルから特定のレコード１件を非同期で取得します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="keyValues">レコード特定Key値を初期化子で指定するラムダ式。例：「<c>() => new Tbl1 { Key1 = 1, Key2 = 99 }</c>」</param>
        /// <param name="targetColumns">値取得対象カラムを限定する場合は、対象カラムについての匿名型を返すラムダ式。例：「<c>t => new { t.Col1, t.Col2 }</c>」</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        /// <returns>取得したレコード（１件、レコード不存在の場合はnull）</returns>
        public static Task<T> SelectAsync<T>(this IDbConnection connection, Expression<Func<T>> keyValues, Expression<Func<T, dynamic>> targetColumns = null, int? timeout = null)
        {
            return new QueryRunner(connection, timeout).SelectAsync(keyValues, targetColumns);
        }

        /// <summary>
        /// 指定されたテーブルからレコードを非同期で取得します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="where">レコード絞り込み条件（絞り込みを行わず全件対象とする場合はnull）</param>
        /// <param name="targetColumns">値取得対象カラムを限定する場合は、対象カラムについての匿名型を返すラムダ式。例：「<c>t => new { t.Col1, t.Col2 }</c>」</param>
        /// <param name="otherClauses">SQL文のwhere条件より後ろに付加するorderBy条件/limit/offset指定などがあれば、その内容</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        /// <returns>取得したレコード</returns>
        public static Task<IReadOnlyList<T>> SelectAsync<T>(this IDbConnection connection, Expression<Func<T, bool>> where = null, Expression<Func<T, dynamic>> targetColumns = null, string otherClauses = null, int? timeout = null)
        {
            return new QueryRunner(connection, timeout).SelectAsync(where, targetColumns, otherClauses);
        }


        /// <summary>
        /// 指定されたテーブルにレコードを非同期で挿入します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="values">設定値を初期化子で指定するラムダ式：例：「<c>() => new Tbl1 { Key1 = 1, Value = 99 }</c>」</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        /// <returns>挿入された行数</returns>
        public static Task<int> InsertAsync<T>(this IDbConnection connection, Expression<Func<T>> values)
        {
            return new QueryRunner(connection, null).InsertAsync(values);
        }

        /// <summary>
        /// 指定されたテーブルにレコードを非同期で挿入します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="values">設定値を初期化子で指定するラムダ式。例：「<c>() => new Tbl1 { Key1 = 1, Value = 99 }</c>」</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        /// <returns>挿入された行数</returns>
        public static Task<int> InsertAsync<T>(this IDbConnection connection, Expression<Func<T>> values, int? timeout = null)
        {
            return new QueryRunner(connection, timeout).InsertAsync(values);
        }

        /// <summary>
        /// 指定されたテーブルにレコードを非同期で挿入します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="data">挿入するレコード</param>
        /// <param name="targetColumns">値設定対象カラムを限定する場合は、対象カラムについての匿名型を返すラムダ式。例：「<c>t => new { t.Col1, t.Col2 }</c>」</param>
        /// <param name="retrieveInsertedId">[InsertSQL(RetrieveInsertedId = true)]属性で指定された自動連番カラムについて、挿入時に採番されたIDを当該プロパティにセットする場合は、trueを指定</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        /// <returns>挿入された行数</returns>
        public static Task<int> InsertAsync<T>(this IDbConnection connection, T data, Expression<Func<T, dynamic>> targetColumns = null, bool retrieveInsertedId = false, int? timeout = null)
        {
            return new QueryRunner(connection, timeout).InsertAsync(data, targetColumns, retrieveInsertedId);
        }

        /// <summary>
        /// 指定されたテーブルにレコードを非同期で一括挿入します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="data">挿入するレコード（複数件）</param>
        /// <param name="targetColumns">値設定対象カラムを限定する場合は、対象カラムについての匿名型を返すラムダ式。例：「<c>t => new { t.Col1, t.Col2 }</c>」</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        /// <returns>挿入された行数</returns>
        public static Task<int> InsertRowsAsync<T>(this IDbConnection connection, IEnumerable<T> data, Expression<Func<T, dynamic>> targetColumns = null, int? timeout = null)
        {
            return new QueryRunner(connection, timeout).InsertRowsAsync(data, targetColumns);
        }


        /// <summary>
        /// 指定された条件のレコードを非同期で更新します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="values">更新値を初期化子で指定するラムダ式。例：「<c>() => new Tbl1 { Value1 = 99, Flg = true }</c>」</param>
        /// <param name="where">更新対象レコードの条件</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        /// <returns>更新された行数</returns>
        public static Task<int> UpdateAsync<T>(this IDbConnection connection, Expression<Func<T>> values, Expression<Func<T, bool>> where, int? timeout = null)
        {
            return new QueryRunner(connection, timeout).UpdateAsync(values, where);
        }

        /// <summary>
        /// 指定されたレコードを非同期で更新します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="data">更新するレコード</param>
        /// <param name="targetColumns">値更新対象カラムを限定する場合は、対象カラムについての匿名型を返すラムダ式。例：「<c>t => new { t.Col1, t.Col2 }</c>」</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        /// <returns>更新された行数</returns>
        public static Task<int> UpdateAsync<T>(this IDbConnection connection, T data, Expression<Func<T, dynamic>> targetColumns = null, int? timeout = null)
        {
            return new QueryRunner(connection, timeout).UpdateAsync(data, targetColumns);
        }


        /// <summary>
        /// 指定された条件のレコードを非同期で削除します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="where">削除対象レコードの条件</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        /// <returns>削除された行数</returns>
        public static Task<int> DeleteAsync<T>(this IDbConnection connection, Expression<Func<T, bool>> where)
        {
            return new QueryRunner(connection, null).DeleteAsync(where);
        }

        /// <summary>
        /// 指定された条件のレコードを非同期で削除します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="where">削除対象レコードの条件</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        /// <returns>削除された行数</returns>
        public static Task<int> DeleteAsync<T>(this IDbConnection connection, Expression<Func<T, bool>> where, int? timeout = null)
        {
            return new QueryRunner(connection, timeout).DeleteAsync(where);
        }

        /// <summary>
        /// 指定されたレコードを非同期で削除します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="data">削除するレコード</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <returns>削除された行数</returns>
        public static Task<int> DeleteAsync<T>(this IDbConnection connection, T data, int? timeout = null)
        {
            return new QueryRunner(connection, timeout).DeleteAsync(data);
        }

        /// <summary>
        /// 指定されたテーブルの全レコードを非同期で削除します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <typeparam name="T">テーブルにマッピングされた型</typeparam>
        public static Task TruncateAsync<T>(this IDbConnection connection, int? timeout = null)
        {
            return new QueryRunner(connection, timeout).TruncateAsync<T>();
        }
    }
}