﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sparrow.CommonLibrary.Database.Query
{
    public class VariableNameExpression : Expression
    {
        public string Name { get; protected set; }

        protected VariableNameExpression(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            this.Name = name;
        }

        public override ExpressionType NodeType
        {
            get { return ExpressionType.VariableName; }
        }

        public override string OutputSqlString(SqlBuilder.ISqlBuilder builder, Database.ParameterCollection output)
        {
            return builder.BuildParameterName(Name);
        }

        internal static VariableNameExpression Expression(string name)
        {
            return new VariableNameExpression(name);
        }
    }
}
