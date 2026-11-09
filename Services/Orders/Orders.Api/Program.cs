using AutoMapper;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Orders.Bll;
using Orders.Dal;
using Orders.Domain;
using Platform.ServiceDefaults;
using Orders.Api.GrpcServices;
using Orders.Bll.Caching;
using Platform.ServiceDefaults.Caching;
using Platform.ServiceDefaults.Rpc;

var builder=WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddLogging();
builder.Services.AddOrdersDal(builder.Configuration);
builder.Services.AddScoped<ICustomerService,CustomerService>(); builder.Services.AddScoped<IProductService,ProductService>(); builder.AddRedisDistributedCache("redis");
builder.Services.AddTieredCache();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IOrderService>(sp => new CachedOrderService(
    sp.GetRequiredService<OrderService>(), sp.GetRequiredService<ITieredCache>()));
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcLoggingInterceptor>());
builder.Services.AddAutoMapper(_=>{},typeof(MappingProfile));
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options=>options.InvalidModelStateResponseFactory=context=>
{
    var problem=new ValidationProblemDetails(context.ModelState){Status=StatusCodes.Status400BadRequest,Title="Request validation failed."};
    return new BadRequestObjectResult(problem);
});
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen(); builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app=builder.Build();
if(app.Configuration.GetValue("Database:InitializeOnStartup",true))
{
    await using var scope=app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
}
app.UseExceptionHandler(); app.UseSwagger(); app.UseSwaggerUI(); app.MapGrpcService<OrdersGrpcService>(); app.MapControllers(); app.MapDefaultEndpoints(); app.Run();

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails,ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context,Exception exception,CancellationToken cancellationToken)
    {
        var (status,title,detail)=exception switch
        {
            EntityNotFoundException e=>(404,"Resource not found",e.Message),
            BusinessConflictException e=>(409,"Business conflict",e.Message),
            DomainValidationException e=>(400,"Validation failed",e.Message),
            _=>(500,"Unexpected server error","An unexpected error occurred.")
        };
        if(status>=500)logger.LogError(exception,"Unhandled error for {Path}",context.Request.Path); else logger.LogInformation(exception,"Request rejected with {StatusCode}",status);
        context.Response.StatusCode=status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext{HttpContext=context,ProblemDetails=new ProblemDetails{Status=status,Title=title,Detail=detail,Instance=context.Request.Path},Exception=exception});
    }
}
