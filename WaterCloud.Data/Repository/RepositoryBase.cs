﻿/*******************************************************************************
 * Copyright © 2020 WaterCloud.Framework 版权所有
 * Author: WaterCloud
 * Description: WaterCloud快速开发平台
 * Website：
*********************************************************************************/
using Chloe;
using Chloe.SqlServer;
using WaterCloud.Code;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WaterCloud.DataBase
{
    /// <summary>
    /// 仓储实现
    /// </summary>
    public class RepositoryBase : IRepositoryBase, IDisposable
    {
        private DbContext dbcontext;
        public RepositoryBase()
        {
            dbcontext = DBContexHelper.Contex();
        }
        public DbContext GetDbContext()
        {
            return dbcontext;
        }
        public RepositoryBase(string ConnectStr, string providerName)
        {
            dbcontext = DBContexHelper.Contex(ConnectStr, providerName);
        }
        public IRepositoryBase BeginTrans()
        {
            if (dbcontext.Session.CurrentTransaction == null)
            {
                dbcontext.Session.BeginTransaction();
            }
            return this;
        }
        public void Commit()
        {
            try
            {
                if (dbcontext.Session.CurrentTransaction != null)
                {
                    dbcontext.Session.CommitTransaction();
                }
            }
            catch (Exception)
            {
                if (dbcontext.Session.CurrentTransaction != null)
                {
                    dbcontext.Session.RollbackTransaction();
                }
                throw;
            }
        }
        public void Dispose()
        {
            if (dbcontext.Session.CurrentTransaction != null)
            {
                dbcontext.Session.Dispose();
            }
        }
        public void Rollback()
        {
            if (dbcontext.Session.CurrentTransaction != null)
            {
                dbcontext.Session.RollbackTransaction();
            }
        }
        public async Task<TEntity> Insert<TEntity>(TEntity entity) where TEntity : class
        {
           return await dbcontext.InsertAsync(entity);
        }
        public async Task<int> Insert<TEntity>(List<TEntity> entitys) where TEntity : class
        {
            int i = 1;
            await dbcontext.InsertRangeAsync(entitys);
            return 1;
        }
        public async Task<int> Update<TEntity>(TEntity entity) where TEntity : class
        {

            TEntity newentity = dbcontext.QueryByKey<TEntity>(entity);
            dbcontext.TrackEntity(newentity);
            PropertyInfo[] newprops = newentity.GetType().GetProperties();
            PropertyInfo[] props = entity.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                if (prop.GetValue(entity, null) != null)
                {
                    PropertyInfo item = newprops.Where(a => a.Name == prop.Name).FirstOrDefault();
                    if (item != null)
                    {
                        item.SetValue(newentity, prop.GetValue(entity, null), null);
                        if (prop.GetValue(entity, null).ToString() == "&nbsp;")
                            item.SetValue(newentity, null, null);
                    }
                }
            }
            return await dbcontext.UpdateAsync(newentity);
        }
        public async Task<int> Update<TEntity>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TEntity>> content) where TEntity : class
        {
            return await dbcontext.UpdateAsync(predicate, content);
        }
        public async Task<int> Delete<TEntity>(TEntity entity) where TEntity : class
        {
            return await dbcontext.DeleteAsync(entity);
        }
        public async Task<int> Delete<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return await dbcontext.DeleteAsync(predicate);
        }
        public async Task<TEntity> FindEntity<TEntity>(object keyValue) where TEntity : class
        {
            return await dbcontext.QueryByKeyAsync<TEntity>(keyValue);
        }
        public async Task<TEntity> FindEntity<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return dbcontext.Query<TEntity>().FirstOrDefault(predicate);
        }
        public IQuery<TEntity> IQueryable<TEntity>() where TEntity : class
        {
            return dbcontext.Query<TEntity>();
        }
        public IQuery<TEntity> IQueryable<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return dbcontext.Query<TEntity>().Where(predicate);
        }
        public async Task<List<TEntity>> FindList<TEntity>(string strSql) where TEntity : class
        {
            return await dbcontext.SqlQueryAsync<TEntity>(strSql);
        }
        public async Task<List<TEntity>> FindList<TEntity>(string strSql, DbParam[] dbParameter) where TEntity : class
        {
            return await dbcontext.SqlQueryAsync<TEntity>(strSql, dbParameter);
        }
        public async Task<List<TEntity>> FindList<TEntity>(Pagination pagination) where TEntity : class, new()
        {
            var tempData = dbcontext.Query<TEntity>();
            tempData = tempData.OrderBy(pagination.sort);
            pagination.records = tempData.Count();
            tempData = tempData.TakePage(pagination.page, pagination.rows);
            return tempData.ToList();
        }
        public async Task<List<TEntity>> FindList<TEntity>(Expression<Func<TEntity, bool>> predicate, Pagination pagination) where TEntity : class, new()
        {
            var tempData = dbcontext.Query<TEntity>().Where(predicate);
            tempData = tempData.OrderBy(pagination.sort);
            pagination.records = tempData.Count();
            tempData = tempData.TakePage(pagination.page, pagination.rows);
            return tempData.ToList();
        }
        public async Task<List<T>> OrderList<T>(IQuery<T> query, Pagination pagination)
        {
            var tempData = query;
            tempData = tempData.OrderBy(pagination.sort);
            pagination.records = tempData.Count();
            tempData = tempData.TakePage(pagination.page, pagination.rows);
            return tempData.ToList();
        }
        public async Task<List<TEntity>> CheckCacheList<TEntity>(string cacheKey, long old = 0) where TEntity : class
        {
            var cachedata = await CacheHelper.Get<List<TEntity>>(cacheKey);
            if (cachedata == null || cachedata.Count() == 0)
            {
                cachedata = dbcontext.Query<TEntity>().ToList();
                await CacheHelper.Set(cacheKey, cachedata);
            }
            return cachedata;
        }

        public async Task<TEntity> CheckCache<TEntity>(string cacheKey, string keyValue, long old = 0) where TEntity : class
        {
            var cachedata = await CacheHelper.Get<TEntity>(cacheKey + keyValue);
            if (cachedata == null)
            {
                cachedata = await dbcontext.QueryByKeyAsync<TEntity>(keyValue);
                await CacheHelper.Set(cacheKey + keyValue, cachedata);
            }
            return cachedata;
        }
    }
}
