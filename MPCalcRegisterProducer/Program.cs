using MPCalcRegisterProducer.Services;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();

builder.Services.AddControllers();// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().ForwardToPrometheus();

builder.WebHost.UseUrls("http://0.0.0.0:5022");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseHealthChecks("/health");
app.UseHttpMetrics();
app.MapMetrics();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
