using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Play.Common.MassTransit;
using Play.Catalog.Service.Entities;
using Play.Common.MongoDB;
using Play.Common.Settings;

namespace PLay.Catalog.Service
{
    public class Startup
    {
        private const string AllowedOriginSetting = "AllowedOrigin";
        private ServiceSettings serviceSettings;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //-----------------Now we are moving all this code to Repositories->Extensions.cs class:--------------------

            //Adding this BsonSerializer configuration for saving the records in Mongo DB with in a readable format
            // BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.BsonType.String)); //For example like a normal Guid
            // BsonSerializer.RegisterSerializer(new DateTimeSerializer(MongoDB.Bson.BsonType.String)); //For example a normal date format

            // //Retrieve the value from ServiceSettings. from Configuraton class we do this nameof(ServiceSettings) because we want to make sure that the class is staying exactly as the name of the configuration
            // //Then we turn it it into the actual type using Get<ServiceSettings>
            // //So, now we are deserializing the value from .Net conversion system of servicesettings into this serviceSettings value created here
             serviceSettings = Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

            // //Now we are going to construct out MongoDbClient, to do that we will be using ServiceCollection
            // //AddSingleton allow us to register a type or an object and make sure that will be only one instance of this object across the entire microservice in this case. any class that needs it will get this one instance
            // services.AddSingleton(serviceProvider => {
            //     var mongoDbSettings = Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
            //     var mongoClient = new MongoClient(mongoDbSettings.ConnectionString);
            //     return mongoClient.GetDatabase(serviceSettings.ServiceName);

            // });

            // //Also we are moving this code to Repositories-> Extensions.cs class in the section of AddMongoRepository
            // //Also, we inject the ItemsRepository Interface
            // //services.AddSingleton<IItemsRepository, ItemsRepository>();
            // services.AddSingleton<IRepository<Item>>(serviceProvider => 
            // {
            //     var database = serviceProvider.GetService<IMongoDatabase>();
            //     return new MongoRepository<Item>(database, "items");
            // });

            //----------------------------------------------------------------------------------------------------------

            services.AddMongo()
                    .AddMongoRepository<Item>("Items")
                    .ADdMassTRansitWithRabbitMq();

            //RABBITMQ code
            //We are moving this code to the Common.library project so every microservice can use it
            // services.AddMassTransit(x =>
            // {
            //     x.UsingRabbitMq((context, configurator) => 
            //     {
            //         var rabbitMQSettings = Configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
            //         configurator.Host(rabbitMQSettings.Host);
            //         //This line define how the queues are created in the RabbitMQ
            //         //we use first the prefix that we are going to be using for our queues "serviceSettings.ServiceName"
            //         //The "false" is because we are not going to include the namespace of the clasess in the queues
            //         configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(serviceSettings.ServiceName, false));
            //     });
            // });

            //services.AddMassTransitHostedService();

            services.AddControllers(options => 
            {
                options.SuppressAsyncSuffixInActionNames = false;
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "PLay.Catalog.Service", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PLay.Catalog.Service v1"));
                //This block of code is to allow CORS, which is allow petions from different servers, in this case the web app is hosted in nodejs server which is http://localhost:3000/
                //and the microservice is hosted in https://localhost:5001, so the petition is coming from a different server and this throws an CORS error in the browser
                //This is only nedded in development enviroment, that's why we add this code here into env.IsDevelopment() 
                app.UseCors(builder =>
                {
                    builder.WithOrigins(Configuration[AllowedOriginSetting])
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
