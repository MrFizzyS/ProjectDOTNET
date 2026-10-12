using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Domain;

namespace Orders.Dal;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task<int> UpdateAsync(T entity, CancellationToken ct = default);
    Task<int> DeleteAsync(object id, CancellationToken ct = default);
}
public interface ICustomerRepository : IGenericRepository<Customer>
{
    Task<bool> EmailExistsAsync(string email, Guid? exceptId = null, CancellationToken ct = default);
}
public interface ISalesOrderRepository : IGenericRepository<SalesOrder>
{
    Task<int> SetStatusAsync(Guid orderId, string status, byte[]? expectedRowVersion, CancellationToken ct = default);
    Task<IReadOnlyList<SalesOrder>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<SalesOrder?> GetWithCustomerAsync(Guid orderId, CancellationToken ct = default);
}
public interface IProductSnapshotRepository : IGenericRepository<ProductSnapshot>
{
    Task<bool> SkuExistsAsync(string sku, Guid? exceptId = null, CancellationToken ct = default);
}
public interface IOrderLineRepository : IGenericRepository<OrderLine>
{
    Task<IReadOnlyList<OrderLine>> GetByOrderAsync(Guid orderId, CancellationToken ct = default);
    Task<int> DeleteByOrderAsync(Guid orderId, CancellationToken ct = default);
}

// Scoped session: every repository in a Unit of Work receives this same connection and transaction.
public sealed class SqlSession(string connectionString) : IAsyncDisposable
{
    public SqlConnection Connection { get; } = new(connectionString);
    public SqlTransaction? Transaction { get; private set; }
    public async Task BeginAsync(IsolationLevel isolation = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        if (Transaction is not null) throw new InvalidOperationException("Transaction already active.");
        if (Connection.State != ConnectionState.Open) await Connection.OpenAsync(ct);
        Transaction = (SqlTransaction)await Connection.BeginTransactionAsync(isolation, ct);
    }
    public async Task CommitAsync(CancellationToken ct = default) { if (Transaction is null) throw new InvalidOperationException("No active transaction."); await Transaction.CommitAsync(ct); await Transaction.DisposeAsync(); Transaction = null; }
    public async Task RollbackAsync(CancellationToken ct = default) { if (Transaction is null) return; await Transaction.RollbackAsync(ct); await Transaction.DisposeAsync(); Transaction = null; }
    public async ValueTask DisposeAsync() { if (Transaction is not null) await RollbackAsync(); await Connection.DisposeAsync(); }
}

public interface IUnitOfWork : IAsyncDisposable
{
    ICustomerRepository Customers { get; } ISalesOrderRepository Orders { get; } IProductSnapshotRepository Products { get; } IOrderLineRepository Lines { get; }
    Task BeginAsync(IsolationLevel isolation = IsolationLevel.ReadCommitted, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default); Task RollbackAsync(CancellationToken ct = default);
}
public sealed class UnitOfWork(SqlSession session, ICustomerRepository customers, ISalesOrderRepository orders, IProductSnapshotRepository products, IOrderLineRepository lines) : IUnitOfWork
{
    public ICustomerRepository Customers { get; } = customers; public ISalesOrderRepository Orders { get; } = orders; public IProductSnapshotRepository Products { get; } = products; public IOrderLineRepository Lines { get; } = lines;
    public Task BeginAsync(IsolationLevel isolation = IsolationLevel.ReadCommitted, CancellationToken ct = default) => session.BeginAsync(isolation, ct);
    public Task CommitAsync(CancellationToken ct = default) => session.CommitAsync(ct); public Task RollbackAsync(CancellationToken ct = default) => session.RollbackAsync(ct);
    public ValueTask DisposeAsync() => session.DisposeAsync();
}

// Shared Dapper CRUD; each concrete repository supplies safe, fixed SQL for its entity.
public abstract class BaseDapperRepository<T>(SqlSession session) : IGenericRepository<T> where T : class
{
    protected SqlSession Session { get; } = session;
    protected abstract string SelectByIdSql { get; } protected abstract string SelectAllSql { get; } protected abstract string InsertSql { get; } protected abstract string UpdateSql { get; } protected abstract string SoftDeleteSql { get; }
    protected abstract object IdParameters(object id); protected abstract object EntityParameters(T entity); protected abstract Guid EntityId(T entity);
    protected virtual T PrepareForInsert(T entity) => entity;
    public Task<T?> GetByIdAsync(object id, CancellationToken ct = default) => Session.Connection.QuerySingleOrDefaultAsync<T>(new CommandDefinition(SelectByIdSql, IdParameters(id), Session.Transaction, cancellationToken: ct));
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) => (await Session.Connection.QueryAsync<T>(new CommandDefinition(SelectAllSql, transaction: Session.Transaction, cancellationToken: ct))).AsList();
    public async Task<T> AddAsync(T entity, CancellationToken ct = default) { entity=PrepareForInsert(entity); await Session.Connection.ExecuteAsync(new CommandDefinition(InsertSql,EntityParameters(entity),Session.Transaction,cancellationToken:ct)); return (await GetByIdAsync(EntityId(entity),ct))!; }
    public Task<int> UpdateAsync(T entity, CancellationToken ct = default) => Session.Connection.ExecuteAsync(new CommandDefinition(UpdateSql,EntityParameters(entity),Session.Transaction,cancellationToken:ct));
    public Task<int> DeleteAsync(object id, CancellationToken ct = default) => Session.Connection.ExecuteAsync(new CommandDefinition(SoftDeleteSql, IdParameters(id), Session.Transaction, cancellationToken: ct));
}

// Deliberately pure ADO.NET: parameterized SqlCommand and manual SqlDataReader mapping.
public sealed class CustomerRepository(SqlSession session) : ICustomerRepository
{
    private SqlConnection C => session.Connection; private SqlTransaction? Tx => session.Transaction;
    private static Customer Map(SqlDataReader r) => new() { CustomerId=r.GetGuid(0), Email=r.GetString(1), DisplayName=r.GetString(2), CreatedAt=r.GetDateTime(3), UpdatedAt=r.GetDateTime(4), IsDeleted=r.GetBoolean(5), RowVer=(byte[])r[6] };
    public async Task<Customer?> GetByIdAsync(object id, CancellationToken ct=default) { await EnsureOpen(ct); await using var cmd=new SqlCommand("SELECT CustomerId,Email,DisplayName,CreatedAt,UpdatedAt,IsDeleted,RowVer FROM dbo.Customer WHERE CustomerId=@id AND IsDeleted=0",C,Tx); cmd.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=(Guid)id; await using var r=await cmd.ExecuteReaderAsync(ct); return await r.ReadAsync(ct)?Map(r):null; }
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct=default) { await EnsureOpen(ct); await using var cmd=new SqlCommand("SELECT CustomerId,Email,DisplayName,CreatedAt,UpdatedAt,IsDeleted,RowVer FROM dbo.Customer WHERE IsDeleted=0 ORDER BY DisplayName",C,Tx); await using var r=await cmd.ExecuteReaderAsync(ct); var list=new List<Customer>(); while(await r.ReadAsync(ct)) list.Add(Map(r)); return list; }
    public async Task<Customer> AddAsync(Customer x,CancellationToken ct=default) { await EnsureOpen(ct); if(x.CustomerId==Guid.Empty)x.CustomerId=Guid.NewGuid(); await using var cmd=new SqlCommand("INSERT dbo.Customer(CustomerId,Email,DisplayName) VALUES(@id,@email,@name)",C,Tx); cmd.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=x.CustomerId; cmd.Parameters.Add("@email",SqlDbType.NVarChar,254).Value=x.Email; cmd.Parameters.Add("@name",SqlDbType.NVarChar,120).Value=x.DisplayName; await cmd.ExecuteNonQueryAsync(ct); return (await GetByIdAsync(x.CustomerId,ct))!; }
    public async Task<int> UpdateAsync(Customer x,CancellationToken ct=default) { await EnsureOpen(ct); await using var cmd=new SqlCommand("UPDATE dbo.Customer SET Email=@email,DisplayName=@name,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=@actor WHERE CustomerId=@id AND IsDeleted=0 AND RowVer=@rv",C,Tx); cmd.Parameters.Add("@email",SqlDbType.NVarChar,254).Value=x.Email; cmd.Parameters.Add("@name",SqlDbType.NVarChar,120).Value=x.DisplayName; cmd.Parameters.Add("@actor",SqlDbType.NVarChar,100).Value="orders-api"; cmd.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=x.CustomerId; cmd.Parameters.Add("@rv",SqlDbType.Binary,8).Value=x.RowVer; return await cmd.ExecuteNonQueryAsync(ct); }
    public async Task<int> DeleteAsync(object id,CancellationToken ct=default) { await EnsureOpen(ct); await using var cmd=new SqlCommand("UPDATE dbo.Customer SET IsDeleted=1,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=N'orders-api' WHERE CustomerId=@id AND IsDeleted=0",C,Tx); cmd.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=(Guid)id; return await cmd.ExecuteNonQueryAsync(ct); }
    public async Task<bool> EmailExistsAsync(string email,Guid? exceptId=null,CancellationToken ct=default) { await EnsureOpen(ct); await using var cmd=new SqlCommand("SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.Customer WHERE Email=@email AND (@except IS NULL OR CustomerId<>@except) AND IsDeleted=0) THEN 1 ELSE 0 END",C,Tx); cmd.Parameters.Add("@email",SqlDbType.NVarChar,254).Value=email; cmd.Parameters.Add("@except",SqlDbType.UniqueIdentifier).Value=(object?)exceptId??DBNull.Value; return (int)(await cmd.ExecuteScalarAsync(ct)??0)==1; }
    private async Task EnsureOpen(CancellationToken ct) { if(C.State!=ConnectionState.Open) await C.OpenAsync(ct); }
}

public sealed class SalesOrderRepository(SqlSession session) : BaseDapperRepository<SalesOrder>(session), ISalesOrderRepository
{
    protected override string SelectByIdSql => "SELECT OrderId,CustomerId,OrderNumber,Status,PlacedAt,CreatedAt,UpdatedAt,IsDeleted,RowVer FROM dbo.SalesOrder WHERE OrderId=@Id AND IsDeleted=0";
    protected override string SelectAllSql => "SELECT OrderId,CustomerId,OrderNumber,Status,PlacedAt,CreatedAt,UpdatedAt,IsDeleted,RowVer FROM dbo.SalesOrder WHERE IsDeleted=0 ORDER BY CreatedAt DESC";
    protected override string InsertSql => "INSERT dbo.SalesOrder(OrderId,CustomerId,Status) VALUES(@OrderId,@CustomerId,'Draft')";
    protected override string UpdateSql => "UPDATE dbo.SalesOrder SET CustomerId=@CustomerId,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=N'orders-api' WHERE OrderId=@OrderId AND IsDeleted=0 AND RowVer=@RowVer";
    protected override string SoftDeleteSql => "UPDATE dbo.SalesOrder SET IsDeleted=1,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=N'orders-api' WHERE OrderId=@Id AND IsDeleted=0";
    protected override object IdParameters(object id)=>new{Id=(Guid)id};
    protected override object EntityParameters(SalesOrder x)=>new{ x.OrderId,x.CustomerId,x.RowVer };
    protected override Guid EntityId(SalesOrder x)=>x.OrderId;
    protected override SalesOrder PrepareForInsert(SalesOrder x){if(x.OrderId==Guid.Empty)x.OrderId=Guid.NewGuid();return x;}
    public async Task<int> SetStatusAsync(Guid orderId,string status,byte[]? expectedRowVersion,CancellationToken ct=default) { var p=new DynamicParameters();p.Add("OrderId",orderId);p.Add("NewStatus",status);p.Add("ExpectedRowVer",expectedRowVersion,DbType.Binary, size:8);p.Add("Actor","orders-api"); return await Session.Connection.ExecuteAsync(new CommandDefinition("dbo.Order_SetStatus",p,Session.Transaction,commandType:CommandType.StoredProcedure,cancellationToken:ct)); }
    public async Task<IReadOnlyList<SalesOrder>> GetByCustomerAsync(Guid customerId,CancellationToken ct=default)=> (await Session.Connection.QueryAsync<SalesOrder>(new CommandDefinition("SELECT OrderId,CustomerId,OrderNumber,Status,PlacedAt,CreatedAt,UpdatedAt,IsDeleted,RowVer FROM dbo.SalesOrder WHERE CustomerId=@customerId AND IsDeleted=0 ORDER BY CreatedAt DESC",new{customerId},Session.Transaction,cancellationToken:ct))).AsList();
    public async Task<SalesOrder?> GetWithCustomerAsync(Guid orderId,CancellationToken ct=default)
    {
        const string sql="SELECT o.OrderId,o.CustomerId,o.OrderNumber,o.Status,o.PlacedAt,o.CreatedAt,o.UpdatedAt,o.IsDeleted,o.RowVer,c.CustomerId AS CustomerStart,c.Email,c.DisplayName,c.CreatedAt,c.UpdatedAt,c.IsDeleted,c.RowVer FROM dbo.SalesOrder o JOIN dbo.Customer c ON c.CustomerId=o.CustomerId WHERE o.OrderId=@orderId AND o.IsDeleted=0";
        var command=new CommandDefinition(sql,new{orderId},Session.Transaction,cancellationToken:ct);
        var rows=await Session.Connection.QueryAsync<SalesOrder,Customer,SalesOrder>(command,(o,c)=>{c.CustomerId=o.CustomerId;o.Customer=c;return o;},"CustomerStart");
        return rows.SingleOrDefault();
    }
}

public sealed class ProductSnapshotRepository(SqlSession session) : BaseDapperRepository<ProductSnapshot>(session), IProductSnapshotRepository
{
    protected override string SelectByIdSql=>"SELECT ProductId,ProductName,Sku,CurrentPrice,IsDeleted,RowVer FROM dbo.ProductSnapshot WHERE ProductId=@Id AND IsDeleted=0";
    protected override string SelectAllSql=>"SELECT ProductId,ProductName,Sku,CurrentPrice,IsDeleted,RowVer FROM dbo.ProductSnapshot WHERE IsDeleted=0 ORDER BY ProductName";
    protected override string InsertSql=>"INSERT dbo.ProductSnapshot(ProductId,ProductName,Sku,CurrentPrice) VALUES(@ProductId,@ProductName,@Sku,@CurrentPrice)";
    protected override string UpdateSql=>"UPDATE dbo.ProductSnapshot SET ProductName=@ProductName,Sku=@Sku,CurrentPrice=@CurrentPrice,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=N'orders-api' WHERE ProductId=@ProductId AND IsDeleted=0 AND RowVer=@RowVer";
    protected override string SoftDeleteSql=>"UPDATE dbo.ProductSnapshot SET IsDeleted=1,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=N'orders-api' WHERE ProductId=@Id AND IsDeleted=0";
    protected override object IdParameters(object id)=>new{Id=(Guid)id};
    protected override object EntityParameters(ProductSnapshot x)=>new{ x.ProductId,x.ProductName,x.Sku,x.CurrentPrice,x.RowVer };
    protected override Guid EntityId(ProductSnapshot x)=>x.ProductId;
    protected override ProductSnapshot PrepareForInsert(ProductSnapshot x){if(x.ProductId==Guid.Empty)x.ProductId=Guid.NewGuid();return x;}
    public async Task<bool> SkuExistsAsync(string sku,Guid? exceptId=null,CancellationToken ct=default)=>await Session.Connection.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.ProductSnapshot WHERE Sku=@sku AND (@except IS NULL OR ProductId<>@except) AND IsDeleted=0) THEN 1 ELSE 0 END",new{sku,except=exceptId},Session.Transaction,cancellationToken:ct));
}

public sealed class OrderLineRepository(SqlSession session) : IOrderLineRepository
{
    private SqlConnection C=>session.Connection; private SqlTransaction? Tx=>session.Transaction;
    private static OrderLine Map(SqlDataReader r)=>new(){OrderLineId=r.GetInt64(0),OrderId=r.GetGuid(1),ProductId=r.GetGuid(2),ProductNameSnapshot=r.GetString(3),Quantity=r.GetInt16(4),UnitPriceSnapshot=r.GetDecimal(5),IsDeleted=r.GetBoolean(6),RowVer=(byte[])r[7]};
    private async Task Open(CancellationToken ct){if(C.State!=ConnectionState.Open)await C.OpenAsync(ct);}
    public async Task<OrderLine?> GetByIdAsync(object id,CancellationToken ct=default){await Open(ct);await using var cmd=new SqlCommand("SELECT OrderLineId,OrderId,ProductId,ProductNameSnapshot,Quantity,UnitPriceSnapshot,IsDeleted,RowVer FROM dbo.OrderLine WHERE OrderLineId=@id AND IsDeleted=0",C,Tx);cmd.Parameters.Add("@id",SqlDbType.BigInt).Value=Convert.ToInt64(id);await using var r=await cmd.ExecuteReaderAsync(ct);return await r.ReadAsync(ct)?Map(r):null;}
    public async Task<IReadOnlyList<OrderLine>> GetAllAsync(CancellationToken ct=default){await Open(ct);await using var cmd=new SqlCommand("SELECT OrderLineId,OrderId,ProductId,ProductNameSnapshot,Quantity,UnitPriceSnapshot,IsDeleted,RowVer FROM dbo.OrderLine WHERE IsDeleted=0 ORDER BY OrderLineId",C,Tx);await using var r=await cmd.ExecuteReaderAsync(ct);var list=new List<OrderLine>();while(await r.ReadAsync(ct))list.Add(Map(r));return list;}
    public async Task<OrderLine> AddAsync(OrderLine x,CancellationToken ct=default){await Open(ct);await using var cmd=new SqlCommand("INSERT dbo.OrderLine(OrderId,ProductId,ProductNameSnapshot,Quantity,UnitPriceSnapshot) VALUES(@order,@product,@name,@qty,@price); SELECT CAST(SCOPE_IDENTITY() AS bigint)",C,Tx);cmd.Parameters.Add("@order",SqlDbType.UniqueIdentifier).Value=x.OrderId;cmd.Parameters.Add("@product",SqlDbType.UniqueIdentifier).Value=x.ProductId;cmd.Parameters.Add("@name",SqlDbType.NVarChar,200).Value=x.ProductNameSnapshot;cmd.Parameters.Add("@qty",SqlDbType.SmallInt).Value=x.Quantity;cmd.Parameters.Add("@price",SqlDbType.Decimal).Value=x.UnitPriceSnapshot; x.OrderLineId=(long)(await cmd.ExecuteScalarAsync(ct))!;return x;}
    public async Task<int> UpdateAsync(OrderLine x,CancellationToken ct=default){await Open(ct);await using var cmd=new SqlCommand("UPDATE dbo.OrderLine SET Quantity=@qty,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=N'orders-api' WHERE OrderLineId=@id AND IsDeleted=0 AND RowVer=@rv",C,Tx);cmd.Parameters.Add("@qty",SqlDbType.SmallInt).Value=x.Quantity;cmd.Parameters.Add("@id",SqlDbType.BigInt).Value=x.OrderLineId;cmd.Parameters.Add("@rv",SqlDbType.Binary,8).Value=x.RowVer;return await cmd.ExecuteNonQueryAsync(ct);}
    public async Task<IReadOnlyList<OrderLine>> GetByOrderAsync(Guid id,CancellationToken ct=default){await Open(ct);await using var cmd=new SqlCommand("SELECT OrderLineId,OrderId,ProductId,ProductNameSnapshot,Quantity,UnitPriceSnapshot,IsDeleted,RowVer FROM dbo.OrderLine WHERE OrderId=@id AND IsDeleted=0 ORDER BY OrderLineId",C,Tx);cmd.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=id;await using var r=await cmd.ExecuteReaderAsync(ct);var list=new List<OrderLine>();while(await r.ReadAsync(ct))list.Add(Map(r));return list;}
    public async Task<int> DeleteByOrderAsync(Guid id,CancellationToken ct=default){await Open(ct);await using var cmd=new SqlCommand("UPDATE dbo.OrderLine SET IsDeleted=1,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=N'orders-api' WHERE OrderId=@id AND IsDeleted=0",C,Tx);cmd.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=id;return await cmd.ExecuteNonQueryAsync(ct);}
    public async Task<int> DeleteAsync(object id,CancellationToken ct=default){await Open(ct);await using var cmd=new SqlCommand("UPDATE dbo.OrderLine SET IsDeleted=1,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=N'orders-api' WHERE OrderLineId=@id AND IsDeleted=0",C,Tx);cmd.Parameters.Add("@id",SqlDbType.BigInt).Value=Convert.ToInt64(id);return await cmd.ExecuteNonQueryAsync(ct);}
}

public interface IDatabaseInitializer { Task InitializeAsync(CancellationToken ct=default); }
public sealed class DatabaseInitializer(string connectionString) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken ct=default)
    {
        await using var connection=new SqlConnection(connectionString); await connection.OpenAsync(ct);
        foreach(var resource in new[]{"Orders.Dal.Sql.schema.sql","Orders.Dal.Sql.procedures.sql","Orders.Dal.Sql.seed.sql"})
        {
            await using var stream=typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException($"Missing embedded SQL script: {resource}");
            using var reader=new StreamReader(stream);var script=await reader.ReadToEndAsync(ct);
            foreach(var batch in System.Text.RegularExpressions.Regex.Split(script,@"^\s*GO\s*(?:--.*)?$",System.Text.RegularExpressions.RegexOptions.Multiline|System.Text.RegularExpressions.RegexOptions.IgnoreCase).Where(x=>!string.IsNullOrWhiteSpace(x)))
            { await using var command=new SqlCommand(batch,connection){CommandTimeout=120};await command.ExecuteNonQueryAsync(ct); }
        }
    }
}
