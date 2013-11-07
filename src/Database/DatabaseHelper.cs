﻿using Sparrow.CommonLibrary.Database.DbCommon;
using Sparrow.CommonLibrary.Entity;
using Sparrow.CommonLibrary.Mapper;
using Sparrow.CommonLibrary.Mapper.Metadata;
using Sparrow.CommonLibrary.Database.Query;
using Sparrow.CommonLibrary.Database.SqlBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using Sparrow.CommonLibrary.Utility;

namespace Sparrow.CommonLibrary.Database
{
    /// <summary>
    /// 辅助简化Sql语句、存储过程的执行，并支持实体与数据之间的映射。
    /// </summary>
    public partial class DatabaseHelper
    {
        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public ICommandExecuter Executer { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ISqlBuilder Builder { get; private set; }

        private EntityToSqlStatement _entityToSql;
        /// <summary>
        /// 
        /// </summary>
        public EntityToSqlStatement EntityToSql
        {
            get
            {
                if (_entityToSql == null)
                    _entityToSql = EntityToSqlStatement.Create(Builder);
                return _entityToSql;
            }
        }

        #endregion

        #region ctor

        static DatabaseHelper()
        {
            _databaseHelpers = new ConcurrentDictionary<string, DatabaseHelper>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandExecuter"></param>
        /// <param name="entityToSql"> </param>
        protected DatabaseHelper(ICommandExecuter commandExecuter, ISqlBuilder builder)
        {
            if (commandExecuter == null)
                throw new ArgumentNullException("commandExecuter");
            if (builder == null)
                throw new ArgumentNullException("builder");
            Executer = commandExecuter;
            Builder = builder;
        }

        #endregion

        #region Build

        /// <summary>
        /// 生成数据查询Sql语句
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition"></param>
        /// <param name="output"></param>
        /// <param name="options"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        internal protected string BuildDqlSql<T>(ConditionExpression condition, ParameterCollection output, SqlOptions options, out IMapper<T> mapper)
        {
            mapper = Map.GetIMapper<T>();
            var metaInfo = mapper.MetaInfo;
            var fields = metaInfo.GetFieldNames();
            return EntityToSql.StmBuilder.Query(mapper.MetaInfo, fields, condition, output, options);
        }

        /// <summary>
        /// 生成数据查询Sql语句
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition"></param>
        /// <param name="output"></param>
        /// <param name="options"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        internal protected string BuildDqlSql<T>(CompareExpression condition, ParameterCollection output, SqlOptions options, out IMapper<T> mapper)
        {
            mapper = Map.GetIMapper<T>();
            var metaInfo = mapper.MetaInfo;
            var fields = metaInfo.GetFieldNames();
            return EntityToSql.StmBuilder.Query(mapper.MetaInfo, fields, condition, output, options);
        }

        /// <summary>
        /// 生成command对象
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="commandText"></param>
        /// <param name="parameterCollection"></param>
        /// <returns></returns>
        internal protected DbCommand BuildDbCommand(CommandType commandType, string commandText, ParameterCollection parameterCollection)
        {
            if (commandText == null) throw new ArgumentNullException("commandText");
            var command = Executer.DbProvider.CreateDbCommand(commandType, commandText);

            //
            if (parameterCollection != null)
                command.Parameters.AddRange(parameterCollection.ToArray());
            //
            return command;
        }

        /// <summary>
        /// 生成command对象
        /// </summary>
        /// <param name="batch"></param>
        /// <returns></returns>
        internal protected DbCommand BuildDbCommand(SqlBatch batch)
        {
            var sql = new StringBuilder();
            var paramters = CreateParamterCollection();
            EntityExplain entityExplain = null;
            //统一生成sql之后统一去执行
            foreach (var item in batch)
            {
                if (item.ItemType == ItemCommandType.Text)
                {
                    var sqlStat = (string)item.Command;
                    if (item.Parameters == null || item.Parameters.Length == 0)
                    {
                        sql.Append(sqlStat);
                    }
                    else
                    {
                        var paras = paramters.Fill(item.Parameters);
                        sql.AppendFormat(sqlStat, paras);
                    }
                    continue;
                }

                // 实体转换成sql
                var entity = item.Command;
                var entyExpl = entity as IEntityExplain;
                if (entyExpl == null)
                {
                    if (entityExplain == null)
                        entityExplain = new EntityExplain(entity);
                    else
                        entityExplain.Switch(entity);
                }
                bool hasIncrement;
                var innerSql = EntityToSql.GenerateInsertOrUpdate(entyExpl ?? entityExplain, paramters, false, out hasIncrement);
                if (string.IsNullOrEmpty(innerSql))
                    continue;

                sql.AppendLine(innerSql);
            }
            // 没有sql语句，则直接退出。
            if (sql.Length == 0)
                return null;
            //
            return BuildDbCommand(CommandType.Text, sql.ToString(), paramters);
        }

        #endregion

        #region ReceiveIncrement

        /// <summary>
        /// 接收返回的增量标识，按实体对象的顺序从<paramref name="incrementReader"/>取出从数据库中返回的自增序列。
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="incrementReader"></param>
        /// <returns></returns>
        internal protected bool ReceiveIncrement(IEnumerable entities, IDataReader incrementReader)
        {
            if (incrementReader.Read())
            {
                EntityExplain entityExplain = null;
                foreach (var entity in entities)
                {
                    var entyExpl = entity as IEntityExplain;
                    if (entyExpl != null)
                    {
                        entyExpl[((IMetaInfoForDbTable)entyExpl).Identity.Name] = incrementReader.GetValue(0);
                    }
                    else
                    {
                        if (entityExplain == null)
                            entityExplain = new EntityExplain(entity);
                        else
                            entityExplain.Switch(entity);
                        //
                        entityExplain[((IMetaInfoForDbTable)entyExpl).Identity.Name] = incrementReader.GetValue(0);
                    }
                    //
                    if (incrementReader.NextResult() == false) break;
                    if (incrementReader.Read() == false) break;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 接收返回的增量标识，按<paramref name="incrementReader"/>自增序列中的别名取出数据写入对应的<paramref name="entities"/>实体中。
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="incrementReader"></param>
        /// <returns></returns>
        internal protected bool ReceiveIncrement(IDictionary<string, object> entities, IDataReader incrementReader)
        {
            EntityExplain entityExplain = null;
            var received = false;
            while (incrementReader.Read())
            {
                object entity;
                if (entities.TryGetValue(incrementReader.GetName(0), out entity) == false)
                    continue;
                var entyExpl = entity as IEntityExplain;
                if (entyExpl != null)
                {
                    entyExpl[((IMetaInfoForDbTable)entyExpl).Identity.Name] = incrementReader.GetValue(0);
                }
                else
                {
                    if (entityExplain == null)
                        entityExplain = new EntityExplain(entity);
                    else
                        entityExplain.Switch(entity);
                    //
                    entityExplain[((IMetaInfoForDbTable)entyExpl).Identity.Name] = incrementReader.GetValue(0);
                }

                if (received == false)
                    received = true;
                if (incrementReader.NextResult() == false) break;
            }
            return received;
        }

        #endregion

        #region basic

        /// <summary>
        /// 创建一个参数集合的实例对象
        /// </summary>
        /// <returns></returns>
        public virtual ParameterCollection CreateParamterCollection()
        {
            return new ParameterCollection(Builder, Executer.DbProvider);
        }

        /// <summary>
        /// 创建一个参数集合的实例对象
        /// </summary>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public virtual ParameterCollection CreateParamterCollection(int capacity)
        {
            return new ParameterCollection(Builder, Executer.DbProvider, capacity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="commandText"></param>
        /// <param name="parameterCollection"></param>
        /// <param name="dbTransaction"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IDataReader ExecuteReader(CommandType commandType, string commandText, ParameterCollection parameterCollection, DbTransaction dbTransaction = null)
        {
            var command = BuildDbCommand(commandType, commandText, parameterCollection);
            if (dbTransaction == null)
                return Executer.ExecuteReader(command);
            //
            return Executer.ExecuteReader(command, dbTransaction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="commandText"></param>
        /// <param name="parameterCollection"></param>
        /// <param name="dbTransaction"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public T ExecuteScalar<T>(CommandType commandType, string commandText, ParameterCollection parameterCollection, DbTransaction dbTransaction = null)
        {
            var command = BuildDbCommand(commandType, commandText, parameterCollection);
            if (dbTransaction == null)
                return DbValueCast.Cast<T>(Executer.ExecuteScalar(command));
            //
            return DbValueCast.Cast<T>(Executer.ExecuteScalar(command, dbTransaction));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="commandText"></param>
        /// <param name="parameterCollection"></param>
        /// <param name="dbTransaction"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int ExecuteNonQuery(CommandType commandType, string commandText, ParameterCollection parameterCollection, DbTransaction dbTransaction = null)
        {
            var command = BuildDbCommand(commandType, commandText, parameterCollection);
            if (dbTransaction == null)
                return Executer.ExecuteNonQuery(command);
            //
            return Executer.ExecuteNonQuery(command, dbTransaction);
        }

        #endregion

        #region Hide BaseMethod

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        #endregion

        #region CachedDatabaseHelper

        private static readonly ConcurrentDictionary<string, DatabaseHelper> _databaseHelpers;

        public static DatabaseHelper GetHelper(string connectionName)
        {
            return _databaseHelpers.GetOrAdd(connectionName, x =>
            {
                var cmdExecuter = ExecuterManager.Create(connectionName);
                var database = new DatabaseHelper(cmdExecuter, SqlBuilderManager.GetSqlBuilder(connectionName, cmdExecuter.DbProvider.ProviderName));
                return database;
            });
        }

        public static DatabaseHelper GetHelper(string connectionString, string providerName)
        {
            return _databaseHelpers.GetOrAdd(connectionString, x =>
            {
                var cmdExecuter = ExecuterManager.Create(connectionString, providerName);
                var database = new DatabaseHelper(cmdExecuter, SqlBuilderManager.GetSqlBuilder(null, cmdExecuter.DbProvider.ProviderName));
                return database;
            });
        }

        #endregion
    }
}
