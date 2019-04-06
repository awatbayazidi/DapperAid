﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DapperAid.Helpers
{
    /// <summary>
    /// Expressionについてのヘルパーメソッドを提供します。
    /// </summary>
    public static class ExpressionHelper
    {
        /// <summary>
        /// ExpressionのBoxingを展開し、指定された型に変換して返します。
        /// </summary>
        public static T CastTo<T>(this Expression exp)
            where T : Expression
        {
            while (exp is UnaryExpression && exp.NodeType == ExpressionType.Convert)
            {
                exp = (exp as UnaryExpression).Operand;
            }
            return (exp as T);
        }

        /// <summary>
        /// 式木が表している具体的な値を返します。
        /// </summary>
        /// <param name="expression">値を指定している式木</param>
        /// <returns>値</returns>
        public static object EvaluateValue(this Expression expression)
        {
            if (expression is ConstantExpression)
            {   // 定数：値を返す
                return (expression as ConstantExpression).Value;
            }
            if (expression is NewExpression)
            {   // インスタンス生成：生成されたインスタンスを返す
                var expr = (expression as NewExpression);
                var parameters = expr.Arguments.Select(EvaluateValue).ToArray();
                return expr.Constructor.Invoke(parameters);
            }
            if (expression is NewArrayExpression)
            {   // 配列生成：生成された配列を返す
                var expr = (expression as NewArrayExpression);
                return expr.Expressions.Select(EvaluateValue).ToArray();
            }
            if (expression is MethodCallExpression)
            {   // メソッド呼び出し：呼び出し結果を返す
                var expr = (expression as MethodCallExpression);
                var parameters = expr.Arguments.Select(EvaluateValue).ToArray();
                var obj = (expr.Object == null) ? null : EvaluateValue(expr.Object);
                return expr.Method.Invoke(obj, parameters);
            }
            if (expression is InvocationExpression)
            {   // ラムダ等の呼び出し：呼び出し結果を返す
                var invocation = (expression as InvocationExpression);
                var parameters = invocation.Arguments.Select(x => Expression.Parameter(x.Type)).ToArray();
                var arguments = invocation.Arguments.Select(EvaluateValue).ToArray();
                var lambda = Expression.Lambda(invocation, parameters);
                return lambda.Compile().DynamicInvoke(arguments);
            }
            if (expression is BinaryExpression && expression.NodeType == ExpressionType.ArrayIndex)
            {   // 配列等のインデクサ：そのインデックスの値を返す
                var expr = (expression as BinaryExpression);
                var array = (Array)EvaluateValue(expr.Left);
                var index = expr.Right.Type == typeof(int)
                          ? (int)EvaluateValue(expr.Right)
                          : (long)EvaluateValue(expr.Right);
                return array.GetValue(index);
            }

            // フィールド/プロパティ：１つづつたどっていく
            var memberInfoList = new List<MemberInfo>();
            var temp = CastTo<Expression>(expression);
            while (!(temp is ConstantExpression))
            {
                var member = CastTo<MemberExpression>(temp);
                if (member == null)
                {   // フィールドでもプロパティでもない
                    if (expression != temp)
                    {   // 再帰
                        return EvaluateValue(temp);
                    }
                    else
                    {   // 再帰できない：ラムダ実行により値取得
                        var lambda = Expression.Lambda(expression);
                        return lambda.Compile().DynamicInvoke();
                    }
                }
                if (member.Expression == null)
                {   // static
                    if (member.Member.MemberType == MemberTypes.Property)
                    {
                        var info = (PropertyInfo)member.Member;
                        return MemberAccessor.GetStaticValue(info);
                    }
                    if (member.Member.MemberType == MemberTypes.Field)
                    {
                        var info = (FieldInfo)member.Member;
                        return MemberAccessor.GetStaticValue(info);
                    }
                    throw new EvaluateException("can't evaluate:" + member.Member.ToString());
                }
                // instance
                memberInfoList.Add(member.Member);
                temp = CastTo<Expression>(member.Expression);
            }
            var value = ((ConstantExpression)temp).Value;
            for (var i = memberInfoList.Count - 1; i >= 0; i--)
            {
                var mi = memberInfoList[i];
                if (mi is PropertyInfo)
                {
                    value = MemberAccessor.GetValue(value, (mi as PropertyInfo));
                }
                else if (mi is FieldInfo)
                {
                    value = MemberAccessor.GetValue(value, (mi as FieldInfo));
                }
            }
            return value;
        }

        /// <summary>
        /// 引数の式木で指定されている項目名を返します。
        /// </summary>
        /// <param name="expression">項目を指定している式木</param>
        /// <returns>項目名のコレクション</returns>
        public static IEnumerable<string> GetMemberNames(Expression expression)
        {
            if (expression is NewExpression)
            {
                var members = (expression as NewExpression).Members;
                if (members.Count > 0)
                {
                    return members.Select(m => m.Name);
                }
            }
            else if (expression is MemberInitExpression)
            {
                var members = (expression as MemberInitExpression).Bindings;
                if (members.Count > 0)
                {
                    return members.Select(m => m.Member.Name);
                }
            }
            else if (expression is MemberExpression)
            {
                return new[] { (expression as MemberExpression).Member.Name };
            }
            throw new ArgumentException("argument must be Expression specifiing an item name", expression.ToString());
        }
    }
}
