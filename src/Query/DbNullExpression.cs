﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sparrow.CommonLibrary.Query
{
    public class DbNullExpression : ConstantExpression
    {
        public static readonly DbNullExpression Instance;

        static DbNullExpression()
        {
            Instance = new DbNullExpression();
        }

        private DbNullExpression()
            : base(null)
        {
        }

        public override ExpressionType NodeType
        {
            get { return ExpressionType.Null; }
        }

        public override string OutputSqlString(Database.SqlBuilder.ISqlBuilder builder, Database.ParameterCollection output)
        {
            return "NULL";
        }
    }
}
