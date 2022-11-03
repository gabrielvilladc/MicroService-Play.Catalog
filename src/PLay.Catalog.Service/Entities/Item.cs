using System;
using Play.Common;

namespace Play.Catalog.Service.Entities
//to install the mondoDB driver we execute the next comand in the terminal:
//cd src
//cd .\Play.Catalog.Service\
//dotnet add package MongoDB.Driver
//And we go to the file Play.Catalog.Service.csproj we should see the PackageReference to MongoDB.Driver
{

    public class Item : IEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
    }
}