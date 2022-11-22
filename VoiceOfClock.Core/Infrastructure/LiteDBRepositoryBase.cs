using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Models.Infrastructure;

public abstract class LiteDBRepositoryBase<T>
{
    protected ILiteCollection<T> _collection;

    public LiteDBRepositoryBase(ILiteDatabase liteDatabase)
    {
        _collection = liteDatabase.GetCollection<T>();
    }

    public virtual T FindById(BsonValue id)
    {
        return _collection.FindById(id);
    }

    public virtual T CreateItem(T item)
    {
        _collection.Insert(item);
        return item;
    }

    public virtual T UpdateItem(T item)
    {
        _collection.Upsert(item);
        return item;
    }

    public virtual int UpdateItem(IEnumerable<T> items)
    {
        return _collection.Upsert(items);
    }

    public virtual bool DeleteItem(BsonValue id)
    {
        return _collection.Delete(id);
    }

    public virtual List<T> ReadAllItems()
    {
        return _collection.FindAll().ToList();
    }

    public bool Exists(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return _collection.Exists(predicate);
    }

    public int Count()
    {
        return _collection.Count();
    }

    public int Count(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return _collection.Count(predicate);
    }
}
