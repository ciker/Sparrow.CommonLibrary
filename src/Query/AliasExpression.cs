﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sparrow.CommonLibrary.Query
{
    public class AliasExpression : SqlExpression
    {
        public SqlExpression Exp { get; protected set; }

        public new string Alias { get; protected set; }

        protected AliasExpression()
        {
        }

        public override ExpressionType NodeType
        {
            get { return ExpressionType.Alias; }
        }

        public override string OutputSqlString(Database.SqlBuilder.ISqlBuilder builder, Database.ParameterCollection output)
        {
            return string.Concat(Exp.OutputSqlString(builder, output), " AS ", Alias);
        }

        internal static AliasExpression Expression(SqlExpression expression, string alias)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");
            if (string.IsNullOrEmpty(alias))
                throw new ArgumentNullException("alias");

            return new AliasExpression() { Exp = expression, Alias = alias };
        }
    }
}
