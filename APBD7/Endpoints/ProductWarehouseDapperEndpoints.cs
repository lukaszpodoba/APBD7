using System.Data.SqlClient;
using APBD7.DTOs;
using APBD7.Models;
using APBD7.Services;
using Dapper;
using FluentValidation;

namespace APBD7.Endpoints;

public static class ProductWarehouseDapperEndpoints
{
    public static void RegisterWarehouseDapperEndpoints(this WebApplication app)
    {
        app.MapPost("Product_Warehouse", AddProductWarehouse);
    }
    
    private static async Task<IResult> AddProductWarehouse(
        CreateProductWarehouse request,
        IDbServiceDapper db,
        IValidator<CreateProductWarehouse> validator
    )
    {
        // Validate request
        var validate = await validator.ValidateAsync(request);
        if (!validate.IsValid)
        {
            return Results.ValidationProblem(validate.ToDictionary());
        }
        
        //---------------------
        
        //Check if product exists
        var product = await db.GetProductById(request.IdProduct);
        if (product == null)
        {
            return Results.NotFound($"Product with id {request.IdProduct} not found");
        }
        
        //Check if warehouse exists
        var warehouse = await db.GetWarehouseById(request.IdWarehouse);
        if (warehouse == null)
        {
            return Results.NotFound($"Warehouse with id {request.IdWarehouse} not found");
        }
        
        //Check if amount is greater than 0
        if (request.Amount <= 0)
        {
            return Results.BadRequest("Amount must be greater than 0");
        }
        
        //Check if order exists
        var order = await db.GetOrderByProductIdAndAmount(request.IdProduct, request.Amount);
        if (order == null)
        {
            return Results.NotFound($"Order with product id {request.IdProduct} and amount {request.Amount} not found");
        }
        
        //Checking date od order
        if (order.CreatedAt > request.CreatedAt)
        {
            return Results.BadRequest("Wrong order date");
        }
        
        //Checking if order is not already completed
        var orderProductWarehouse = await db.GetProductWarehouseByOrderId(order.IdOrder);
        if (orderProductWarehouse != null)
        {
            return Results.BadRequest("Order is already completed");
        }

        //---------------------
        
        var result = await db.CreateProductWarehouse(
            new CreateProductWarehouse(
                request.IdProduct, 
                request.IdWarehouse, 
                request.Amount, 
                request.CreatedAt
            ));
        return Results.Created($"/product-warehouse/{result}", result);
    }
}