﻿namespace MyTested.Mvc.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Internal;

    /// <summary>
    /// Utility class helping parsing expression trees.
    /// </summary>
    public static class ExpressionParser
    {
        /// <summary>
        /// Parses method info from method call lambda expression.
        /// </summary>
        /// <param name="expression">Expression to be parsed.</param>
        /// <returns>Method info.</returns>
        public static MethodInfo GetMethodInfo(LambdaExpression expression)
        {
            var methodCallExpression = GetMethodCallExpression(expression);
            return methodCallExpression.Method;
        }

        /// <summary>
        /// Parses method name from method call lambda expression.
        /// </summary>
        /// <param name="expression">Expression to be parsed.</param>
        /// <returns>Method name as string.</returns>
        public static string GetMethodName(LambdaExpression expression)
        {
            var methodInfo = GetMethodInfo(expression);
            return methodInfo.Name;
        }

        /// <summary>
        /// Resolves arguments from method in method call lambda expression.
        /// </summary>
        /// <param name="expression">Expression to be parsed.</param>
        /// <returns>Collection of method argument information.</returns>
        public static IEnumerable<MethodArgumentContext> ResolveMethodArguments(LambdaExpression expression)
        {
            var methodCallExpression = GetMethodCallExpression(expression);
            return methodCallExpression.Arguments
                .Zip(
                    methodCallExpression.Method.GetParameters(),
                    (m, a) => new
                    {
                        a.Name,
                        Value = ResolveExpressionValue(m)
                    })
                .Select(ma => new MethodArgumentContext
                {
                    Name = ma.Name,
                    Type = ma.Value?.GetType(),
                    Value = ma.Value
                })
                .ToList();
        }

        public static object ResolveExpressionValue(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                // Expression which contains converting from type to type
                var expressionArgumentAsUnary = (UnaryExpression)expression;
                expression = expressionArgumentAsUnary.Operand;
            }

            if (expression.NodeType == ExpressionType.Call)
            {
                // Expression of type c => c.Action(With.No<int>()) - value should be ignored and can be skipped.
                var expressionArgumentAsMethodCall = (MethodCallExpression)expression;
                if (expressionArgumentAsMethodCall.Object == null
                    && expressionArgumentAsMethodCall.Method.DeclaringType == typeof(With))
                {
                    return null;
                }
            }

            object value;
            if (expression.NodeType == ExpressionType.Constant)
            {
                // Expression of type c => c.Action({const}) - value can be extracted without compiling.
                value = ((ConstantExpression)expression).Value;
            }
            else
            {
                // Expresion needs compiling because it is not of constant type.
                var convertExpression = Expression.Convert(expression, typeof(object));
                value = Expression.Lambda<Func<object>>(convertExpression).Compile().Invoke();
            }

            return value;
        }

        /// <summary>
        /// Retrieves custom attributes on a method from method call lambda expression.
        /// </summary>
        /// <param name="expression">Expression to be parsed.</param>
        /// <returns>Collection of attributes as objects.</returns>
        public static IEnumerable<object> GetMethodAttributes(LambdaExpression expression)
        {
            var methodCallExpression = GetMethodCallExpression(expression);
            return Reflection.GetCustomAttributes(methodCallExpression.Method);
        }

        /// <summary>
        /// Parses member name from member lambda expression.
        /// </summary>
        /// <param name="expression">Expression to be parsed.</param>
        /// <returns>Member name as string.</returns>
        public static string GetPropertyName(LambdaExpression expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
            {
                throw new ArgumentException("Provided expression is not a valid member expression.");
            }

            return memberExpression.Member.Name;
        }

        /// <summary>
        /// Gets instance method call expression from a lambda expression.
        /// </summary>
        /// <param name="expression">The lambda expression as MethodCallExpression.</param>
        /// <returns>Method call expression.</returns>
        public static MethodCallExpression GetMethodCallExpression(LambdaExpression expression)
        {
            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression == null)
            {
                throw new InvalidOperationException("Provided expression is not valid - expected instance method call but instead received other type of expression.");
            }

            var objectInstance = methodCallExpression.Object;
            if (objectInstance == null)
            {
                throw new InvalidOperationException("Provided expression is not valid - expected instance method call but instead received static method call.");
            }

            return methodCallExpression;
        }
    }
}
