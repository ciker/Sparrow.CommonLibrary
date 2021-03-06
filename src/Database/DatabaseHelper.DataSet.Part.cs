﻿using Sparrow.CommonLibrary.Query;
using Sparrow.CommonLibrary.Database.SqlBuilder;
using Sparrow.CommonLibrary.Mapper;
using System;
using System.Data;
using System.Data.Common;

namespace Sparrow.CommonLibrary.Database
{
    public partial class DatabaseHelper
    {

        /// <summary>
        /// 执行sql语句，返回DataSet。
        /// </summary>
        /// <param name="commandText">sql语句</param>
        /// <returns>返回DataSet</returns>
        public DataSet ExecuteDataSet(string commandText)
        {
            var command = BuildDbCommand(CommandType.Text, commandText, (ParameterCollection)null);
            return ExecuteDataSet(command);
        }

        /// <summary>
        /// 执行sql语句，返回DataSet。
        /// </summary>
        /// <param name="commandText">sql语句</param>
        /// <param name="parameters">sql语句的参数</param>
        /// <returns>返回DataSet</returns>
        public DataSet ExecuteDataSet(string commandText, ParameterCollection parameters)
        {
            var command = BuildDbCommand(CommandType.Text, commandText, parameters);
            return ExecuteDataSet(command);
        }

        /// <summary>
        /// 执行sql语句，返回DataSet。
        /// </summary>
        /// <param name="commandText">sql语句</param>
        /// <param name="parameters">sql语句的参数</param>
        /// <returns>返回DataSet</returns>
        public DataSet ExecuteDataSet(string commandText, params object[] parameters)
        {
            var command = BuildDbCommand(CommandType.Text, commandText, parameters);
            return ExecuteDataSet(command);
        }

        /// <summary>
        /// 执行sql语句，返回DataSet。
        /// </summary>
        /// <param name="commandText">sql语句</param>
        /// <param name="parameters">sql语句的参数</param>
        /// <param name="transaction"> </param>
        /// <returns>返回DataSet</returns>
        public DataSet ExecuteDataSet(string commandText, ParameterCollection parameters, DbTransaction transaction)
        {
            var command = BuildDbCommand(CommandType.Text, commandText, parameters);
            return ExecuteDataSet(command, transaction);
        }

        /// <summary>
        /// 执行sql语句，返回DataSet。
        /// </summary>
        /// <param name="commandText">sql语句</param>
        /// <param name="parameters">sql语句的参数</param>
        /// <param name="transaction"> </param>
        /// <returns>返回DataSet</returns>
        public DataSet ExecuteDataSet(string commandText, DbTransaction transaction, params object[] parameters)
        {
            var command = BuildDbCommand(CommandType.Text, commandText, parameters);
            return ExecuteDataSet(command);
        }

        /// <summary>
        /// 执行sql语句，返回DataSet。
        /// </summary>
        /// <param name="batch">sql批次</param>
        /// <returns>受影响的行数</returns>
        /// <remarks><paramref name="batch"/> 中包含增量标识的实体对象，不会返回自增序列的值。</remarks>
        public DataSet ExecuteDataSet(SqlBatch batch)
        {
            var command = BuildDbCommand(batch);
            return ExecuteDataSet(command);
        }

        /// <summary>
        /// 执行sql语句，返回DataSet。
        /// </summary>
        /// <param name="batch">sql批次</param>
        /// <param name="transaction">事务处理</param>
        /// <returns>受影响的行数</returns>
        /// <remarks><paramref name="batch"/> 中包含增量标识的实体对象，不会返回自增序列的值。</remarks>
        public DataSet ExecuteDataSet(SqlBatch batch, DbTransaction transaction)
        {
            var command = BuildDbCommand(batch);
            return ExecuteDataSet(command, transaction);
        }


        /// <summary>
        /// 执行存储过程，返回DataSet。
        /// </summary>
        /// <param name="commandText">存储过程</param>
        /// <returns>返回DataSet</returns>
        public DataSet SprocExecuteDataSet(string commandText)
        {
            var command = BuildDbCommand(CommandType.StoredProcedure, commandText, (ParameterCollection)null);
            return ExecuteDataSet(command);
        }

        /// <summary>
        /// 执行存储过程，返回DataSet。
        /// </summary>
        /// <param name="commandText">存储过程</param>
        /// <param name="parameters">存储过程的参数</param>
        /// <returns>返回DataSet</returns>
        public DataSet SprocExecuteDataSet(string commandText, ParameterCollection parameters)
        {
            var command = BuildDbCommand(CommandType.Text, commandText, parameters);
            return ExecuteDataSet(command);
        }

        /// <summary>
        /// 执行存储过程，返回DataSet。
        /// </summary>
        /// <param name="commandText">存储过程</param>
        /// <param name="parameters">存储过程的参数</param>
        /// <param name="transaction"> </param>
        /// <returns>返回DataSet</returns>
        public DataSet SprocExecuteDataSet(string commandText, ParameterCollection parameters, DbTransaction transaction)
        {
            var command = BuildDbCommand(CommandType.Text, commandText, parameters);
            return ExecuteDataSet(command, transaction);
        }
    }
}
