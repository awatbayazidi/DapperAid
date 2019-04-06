﻿using DapperAid.Helpers;

namespace DapperAid
{
    partial class QueryBuilder
    {
        /// <summary>
        /// Microsoft Access用(SqlCEも流用可)のSQL/パラメータを組み立てるクラスです。
        /// </summary>
        public class MsAccess : QueryBuilder
        {
            /// <summary>SQL識別子（テーブル名/カラム名等）をエスケープします。MsAccessでは"[","]"を使用します。</summary>
            public override string EscapeIdentifier(string identifier)
            {
                return "[" + identifier + "]";
            }

            /// <summary>自動連番値を取得するSQL句として、セミコロンで区切った別のSQL文を付加します。</summary>
            protected override string GetInsertedIdReturningSql<T>(TableInfo.Column column)
            {
                return "; select @@IDENTITY";
            }

            /// <summary>MsAccessはTruncate使用不可のため、代替としてDeleteSQLを返します。</summary>
            public override string BuildTruncate<T>()
            {
                return base.BuildDelete<T>();
            }
        }
    }
}