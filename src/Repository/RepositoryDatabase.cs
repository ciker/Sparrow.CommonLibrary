﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using Sparrow.CommonLibrary.Database;
using Sparrow.CommonLibrary.Database.SqlBuilder;
using Sparrow.CommonLibrary.Entity;
using Sparrow.CommonLibrary.Mapper;
using Sparrow.CommonLibrary.Mapper.Metadata;
using System.Linq.Expressions;
using System.Reflection;
using Sparrow.CommonLibrary.Mapper.DataSourceReader;
using Sparrow.CommonLibrary.Database.Query;
using Sparrow.CommonLibrary.Extenssions;

namespace Sparrow.CommonLibrary.Repository
{
    /// <summary>
    /// 基于数据库的<see cref="IRepository"/>实现
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RepositoryDatabase<T> : IRepository<T> where T : class
    {
        private readonly DatabaseHelper _database;

        protected DatabaseHelper Database { get { return _database; } }

        private readonly IMapper<T> mapper;

        public RepositoryDatabase(Database.DatabaseHelper database)
        {
            if (typeof(T) == typeof(DynamicEntity))
                throw new ArgumentException(string.Format("泛型T不能是{0}", typeof(DynamicEntity).FullName));
            _database = database;
            mapper = Mapper.Map.GetIMapper<T>();
        }

        protected ISqlBuilder SqlBuilder { get { return _database.Builder; } }

        protected EntityToSqlStatement EntityToSql { get { return _database.EntityToSql; } }

        protected ParameterCollection CreateParamterCollection()
        {
            return _database.CreateParamterCollection();
        }

        protected string FieldName(Expression<Func<T, object>> field)
        {
            var propertyInfo = (PropertyInfo)PropertyExpression.ExtractMemberExpression(field).Member;
            var fieldMap = mapper.MetaInfo[propertyInfo];
            if (fieldMap != null)
                return fieldMap.FieldName;

            throw new ArgumentException("参数不支持作为查询条件，因为无法获取该属性所映射的成员字段。");
        }

        #region BuildDmlSql

        protected string BuildDmlSql(T entity, ParameterCollection output, ref IDictionary<string, T> incrementEntity)
        {
            var expl = new EntityExplain<T>(entity);
            return BuildDmlSql(expl, expl.OperationState, output, ref incrementEntity);
        }

        protected string BuildDmlSql(T entity, DataState state, ParameterCollection output, ref IDictionary<string, T> incrementEntity)
        {
            var expl = new EntityExplain<T>(entity);
            return BuildDmlSql(expl, state, output, ref incrementEntity);
        }

        protected string BuildDmlSql(IEnumerable<T> entities, ParameterCollection output, ref IDictionary<string, T> incrementEntity)
        {
            StringBuilder sql = new StringBuilder();
            foreach (T entity in entities)
            {
                sql.AppendLine(BuildDmlSql(entity, output, ref incrementEntity));
            }
            return sql.ToString();
        }

        protected string BuildDmlSql(IEnumerable<T> entities, DataState state, ParameterCollection output, ref IDictionary<string, T> incrementEntity)
        {
            StringBuilder sql = new StringBuilder();
            foreach (T entity in entities)
            {
                sql.AppendLine(BuildDmlSql(entity, state, output, ref incrementEntity));
            }
            return sql.ToString();
        }

        private string BuildDmlSql(EntityExplain<T> expl, DataState state, ParameterCollection output, ref IDictionary<string, T> incrementEntity)
        {
            //
            string identityFieldName = null;
            if (expl.Identity != null)
            {
                if (incrementEntity == null)
                    incrementEntity = new Dictionary<string, T>();
                identityFieldName = expl.Identity.FieldInfo.FieldName + incrementEntity.Count;
            }
            //
            string innerSql;
            bool hasIncrement = false;
            switch (state)
            {
                case DataState.New:
                    innerSql = EntityToSql.GenerateInsert(expl, output, true, out hasIncrement, identityFieldName);
                    break;
                case DataState.Modify:
                    innerSql = EntityToSql.GenerateUpdate(expl, output);
                    break;
                case DataState.NewOrModify:
                    innerSql = EntityToSql.GenerateInsertOrUpdate(expl, output, true, out hasIncrement, identityFieldName);
                    break;
                default:
                    throw new ArgumentException("不支持枚举：" + expl.OperationState);
            }
            if (hasIncrement)
                incrementEntity.Add(identityFieldName, expl.Data);
            return innerSql;
        }

        #endregion

        #region ReceiveIncrement

        /// <summary>
        /// 接收返回的增量标识，按<paramref name="incrementReader"/>自增序列中的别名取出数据写入对应的<paramref name="entities"/>实体中。
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="incrementReader"></param>
        /// <returns></returns>
        internal protected bool ReceiveIncrement(IDictionary<string, T> entities, IDataReader incrementReader)
        {
            EntityExplain entityExplain = null;
            var received = false;
            while (incrementReader.Read())
            {
                T entity;
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

        #region DoExecute

        protected int DoExecute(string sql, ParameterCollection parameterCollection, IDictionary<string, T> incrementEntity)
        {
            if (string.IsNullOrEmpty(sql))
                return 0;
            if (incrementEntity == null || incrementEntity.Count == 0)
            {
                return _database.ExecuteNonQuery(CommandType.Text, sql, parameterCollection);
            }
            using (var reader = _database.ExecuteReader(CommandType.Text, sql, parameterCollection))
            {
                ReceiveIncrement(incrementEntity, reader);
                return reader.RecordsAffected;
            }
        }

        #endregion

        #region IRepository<T>.DML

        public int Insert(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");
            //
            IDictionary<string, T> incrementEntity = null;
            var parameters = CreateParamterCollection();
            var sql = BuildDmlSql(entity, DataState.New, parameters, ref incrementEntity);
            //
            return DoExecute(sql, parameters, incrementEntity);
        }

        public int Insert(IEnumerable<T> entities)
        {
            if (entities == null)
                throw new ArgumentNullException("entity");

            var parameters = CreateParamterCollection();
            IDictionary<string, T> incrementEntity = null;
            var sql = BuildDmlSql(entities, DataState.New, parameters, ref incrementEntity);
            //
            return DoExecute(sql, parameters, incrementEntity);
        }

        public int Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");
            //
            var parameters = CreateParamterCollection();
            IDictionary<string, T> incrementEntity = null;
            var sql = BuildDmlSql(entity, DataState.Modify, parameters, ref incrementEntity);
            //
            return DoExecute(sql, parameters, incrementEntity);
        }

        public int Update(IEnumerable<T> entities)
        {
            if (entities == null)
                throw new ArgumentNullException("entities");
            //
            var parameters = CreateParamterCollection();
            IDictionary<string, T> incrementEntity = null;
            var sql = BuildDmlSql(entities, DataState.Modify, parameters, ref incrementEntity);
            //
            return DoExecute(sql, parameters, incrementEntity);
        }

        public int Save(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");
            //
            var parameters = CreateParamterCollection();
            IDictionary<string, T> incrementEntity = null;
            var sql = BuildDmlSql(entity, parameters, ref incrementEntity);
            //
            return DoExecute(sql, parameters, incrementEntity);
        }

        public int Save(IEnumerable<T> entities)
        {
            if (entities == null)
                throw new ArgumentNullException("entities");
            //
            var parameters = CreateParamterCollection();
            IDictionary<string, T> incrementEntity = null;
            var sql = BuildDmlSql(entities, parameters, ref incrementEntity);
            //
            return DoExecute(sql, parameters, incrementEntity);
        }

        public int Delete(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");
            //
            var parameters = CreateParamterCollection();
            var expl = new EntityExplain<T>(entity);
            var sql = EntityToSql.GenerateDelete(expl, parameters);
            //
            return DoExecute(sql, parameters, null);
        }

        public int Delete(CompareExpression condition)
        {
            throw new NotImplementedException();
        }

        public int Delete(ConditionExpression condition)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");
            //
            var parameters = CreateParamterCollection();
            var sql = SqlBuilder.Delete(mapper.MetaInfo, condition, parameters, SqlOptions.None);
            //
            return DoExecute(sql, parameters, null);
        }

        #endregion

        #region IRepository<T>.Query

        public IList<T> GetList()
        {
            return _database.ExecuteList<T>(SqlBuilder.Query(mapper.MetaInfo, mapper.MetaInfo.GetFieldNames(), SqlOptions.NoLock));
        }

        public IList<T> GetList(CompareExpression condition)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");

            return _database.ExecuteList<T>(condition, SqlOptions.NoLock);
        }

        public IList<T> GetList(ConditionExpression condition)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");

            return _database.ExecuteList<T>(condition, SqlOptions.NoLock);
        }

        public T Get(CompareExpression condition)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");

            return _database.ExecuteFirst<T>(condition, SqlOptions.NoLock);
        }

        public T Get(object id)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (mapper.MetaInfo.KeyCount < 1)
                throw new ArgumentException("没有主键信息");
            if (mapper.MetaInfo.KeyCount != 1)
                throw new ArgumentException("复合主键的实体对象，无法使用该方法。");

            var condition = SqlExpression.Equal(mapper.MetaInfo.GetKeys()[0], id);
            return _database.ExecuteFirst<T>(condition, SqlOptions.NoLock);
        }

        public T Get(ConditionExpression condition)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");

            return _database.ExecuteFirst<T>(condition, SqlOptions.NoLock);
        }

        public TValue Sum<TValue>(System.Linq.Expressions.Expression<Func<T, object>> field)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Sum(field)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Sum<TValue>(Expression<Func<T, object>> field, CompareExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Sum(field)
                .Where(condition)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Sum<TValue>(System.Linq.Expressions.Expression<Func<T, object>> field, ConditionExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Sum(field)
                .Where(condition)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Min<TValue>(System.Linq.Expressions.Expression<Func<T, object>> field)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Min(field)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Min<TValue>(Expression<Func<T, object>> field, CompareExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Min(field)
                .Where(condition)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Min<TValue>(System.Linq.Expressions.Expression<Func<T, object>> field, ConditionExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Min(field)
                .Where(condition)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Max<TValue>(System.Linq.Expressions.Expression<Func<T, object>> field)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Max(field)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Max<TValue>(Expression<Func<T, object>> field, CompareExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Max(field)
                .Where(condition)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Max<TValue>(System.Linq.Expressions.Expression<Func<T, object>> field, ConditionExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Max(field)
                .Where(condition)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Avg<TValue>(System.Linq.Expressions.Expression<Func<T, object>> field)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Avg(field)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Avg<TValue>(Expression<Func<T, object>> field, CompareExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Avg(field)
                .Where(condition)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Avg<TValue>(System.Linq.Expressions.Expression<Func<T, object>> field, ConditionExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Avg(field)
                .Where(condition)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Count<TValue>(System.Linq.Expressions.Expression<Func<T, object>> field)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Count(field)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Count<TValue>(Expression<Func<T, object>> field, CompareExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Count(field)
                .Where(condition)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public TValue Count<TValue>(System.Linq.Expressions.Expression<Func<T, object>> field, ConditionExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Count(field)
                .Where(condition)
                .OutputSqlString(parameters);
            return Database.ExecuteScalar<TValue>(sql, parameters);
        }

        public IDictionary<TKey, TValue> Groupby<TKey, TValue>(Expression<Func<T, object>> field, Expression<Func<T, object>> groupby)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Select(groupby)
                .Count(field)
                .GroupBy(groupby)
                .OutputSqlString(parameters);
            using (var reader = Database.ExecuteReader(sql, parameters))
            {
                return reader.ToDictionary<TKey, TValue>();
            }
        }

        public IDictionary<TKey, TValue> Groupby<TKey, TValue>(Expression<Func<T, object>> field, Expression<Func<T, object>> groupby, CompareExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Select(groupby)
                .Count(field)
                .Where(condition)
                .GroupBy(groupby)
                .OutputSqlString(parameters);
            using (var reader = Database.ExecuteReader(sql, parameters))
            {
                return reader.ToDictionary<TKey, TValue>();
            }
        }

        public IDictionary<TKey, TValue> Groupby<TKey, TValue>(Expression<Func<T, object>> field, Expression<Func<T, object>> groupby, ConditionExpression condition)
        {
            var parameters = CreateParamterCollection();
            string sql = new Queryable<T>(Database)
                .Select(groupby)
                .Count(field)
                .Where(condition)
                .GroupBy(groupby)
                .OutputSqlString(parameters);
            using (var reader = Database.ExecuteReader(sql, parameters))
            {
                return reader.ToDictionary<TKey, TValue>();
            }
        }

        #endregion

    }
}