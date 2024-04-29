using System.Data;
using System.Data.SqlClient;
using APBD7.DTOs;
using APBD7.Models;
using Dapper;

namespace APBD7.Services;

public interface IDbServiceDapper
{
    Task<Product?> GetProductById(int id);
    Task<Warehouse?> GetWarehouseById(int id);
    Task<Order?> GetOrderById(int id);
    Task<Order?> GetOrderByProductIdAndAmount(int id, float amount);
    Task<ProductWarehouse?> GetProductWarehouseByOrderId(int id);
    Task<int> CreateProductWarehouse(CreateProductWarehouse productWarehouse); //int
}

public class DbServiceDapper(IConfiguration configuration) : IDbServiceDapper
{
    // Helper method for creating and opening connection
    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }
    
    public async Task<Order?> GetOrderById(int id)
    {
        await using var connection = await GetConnection(); //Connecting
        var result = await connection.QueryFirstOrDefaultAsync<Order>(
            "SELECT * FROM [Order] WHERE IdOrder = @idO",
            new
            {
                idO = id
            });

        return result;
    }

    public async Task<Order?> GetOrderByProductIdAndAmount(int id, float amount)
    {
        await using var connection = await GetConnection(); //Connecting
        var result = await connection.QueryFirstOrDefaultAsync<Order>(
            "SELECT * FROM [Order] WHERE idProduct = @idP AND Amount = @Amount", new
            {
                idP = id,
                Amount = amount
            });
        return result;
    }
    
    public async Task<ProductWarehouse?> GetProductWarehouseByOrderId(int id)
    {
        await using var connection = await GetConnection(); //Connecting
        var result = await connection.QueryFirstOrDefaultAsync<ProductWarehouse>(
            "SELECT * FROM Product_Warehouse WHERE IdOrder = @idO", new
            {
                idO = id
            });
        return result;
    }

    public async Task<Product?> GetProductById(int id)
    {
        await using var connection = await GetConnection(); //Connecting
        var result = await connection.QueryFirstOrDefaultAsync<Product>(
            "SELECT * FROM Product WHERE IdProduct = @idP", new
            {
                idP = id
            });
        return result;
    }

    public async Task<Warehouse?> GetWarehouseById(int id)
    {
        await using var connection = await GetConnection(); //Connecting
        var result = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "SELECT * FROM Warehouse WHERE IdWarehouse = @idW", new
            {
                idW = id
            });
        return result;
    }

    public async Task<int> CreateProductWarehouse(CreateProductWarehouse productWarehouse)
    {
        await using var connection = await GetConnection(); //Connecting
        await using var transaction = await connection.BeginTransactionAsync(); //Series of operations
        
        //Product
        var product = await GetProductById(productWarehouse.IdProduct);
        
        //Warehouse
        var warehouse = await GetWarehouseById(productWarehouse.IdWarehouse);
        
        //Order
        var order = await GetOrderByProductIdAndAmount(productWarehouse.IdProduct, productWarehouse.Amount);
        try
        {
            //Updating FulfilledAt in Order
            await connection.ExecuteAsync(
                "UPDATE [Order] SET FulfilledAt = @FulfilledAt WHERE idOrder = @idOrder", new
                {
                    FulfilledAt = productWarehouse.CreatedAt,
                    idOrder = order.IdOrder
                }, transaction);

            //Inserting into Product_Warehouse
            await connection.ExecuteAsync(
                "INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt)",
                new
                {
                    IdWarehouse = productWarehouse.IdWarehouse,
                    IdProduct = productWarehouse.IdProduct,
                    IdOrder = order.IdOrder,
                    Amount = productWarehouse.Amount,
                    Price = product.Price * productWarehouse.Amount,
                    CreatedAt = productWarehouse.CreatedAt
                }, transaction);

            //Committing transaction
            await transaction.CommitAsync();
            
            //ExecuteAsync is for Insert, Update, Delete
        
            var idResult = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT MAX(IdProductWarehouse) FROM Product_Warehouse", transaction);
        
            return idResult;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}