using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Service.Dtos;
using System;
using System.Linq;
using System.Threading.Tasks;
using Play.Catalog.Service;
using Play.Catalog.Service.Entities;
using Play.Common;
using MassTransit;
using Play.Catalog.Contracts;

namespace Play.Catalog.Controllers
{
    //https://localhost:5001/items
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item> itemsRepository;
        //private static int requestCounter = 0;
        private readonly IPublishEndpoint publishEndPoint;

        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndPoint)
        {
            this.itemsRepository = itemsRepository;
            this.publishEndPoint = publishEndPoint;
        }
        // private static readonly List<ItemDto> items = new()
        // {
        //     new ItemDto(Guid.NewGuid(), "Potion", "Restores a small amount of HP", 5, DateTimeOffset.UtcNow),
        //     new ItemDto(Guid.NewGuid(), "Antidote", "Cures Poison", 7, DateTimeOffset.UtcNow),
        //     new ItemDto(Guid.NewGuid(), "Bronze sword", "Deals a small amount of damage", 20, DateTimeOffset.UtcNow)
        // };

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            //This code was added just for testing purposes, we wanted to simulate a hight trafic volume consulting the service and internals error 500
            // requestCounter++;
            // Console.WriteLine($"Request {requestCounter}: Starting...");

            // if (requestCounter <= 2)
            // {
            //     Console.WriteLine($"Request {requestCounter}: Delaying...");
            //     await Task.Delay(TimeSpan.FromSeconds(10));
            // }

            // if (requestCounter <= 4)
            // {
            //     Console.WriteLine($"Request {requestCounter}: 500 (Internal Server Error)...");
            //     return StatusCode(500);
            // }

            var items = (await itemsRepository.GetAllAsync()).Select(item => item.AsDto());

            //Console.WriteLine($"Request {requestCounter}: 200 (OK)...");
            return Ok(items);
        }

        //GET /items/12345 
        [HttpGet("{id}")]
        //using ActionResult we can return either the item ItemDto or the NotFound result, we can return more than one type of return
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await itemsRepository.GetAsync(id);
            if (item == null)
            {
                //In this case the user will get 404 result, Not Found
                return NotFound();
            }
            return item.AsDto();
        }

        //POST /items
        [HttpPost]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
        {
            var item = new Item{
                Name = createItemDto.Name,
                Description = createItemDto.Description,
                Price = createItemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow
                };

            await itemsRepository.CreateAsync(item);

            //This call will publish a message in the MessageBroker throw the lib we alrady installed called MassTransit
            await publishEndPoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));

            //CreatedAction is derived from ActionResult, it says that the item was created and is located using the function GetById(defined above)
            //From .NETCore 3, the suffix "async" is removed from the method's name, so the solution for this issue is go to the StartUp.cs file, ConsigureServices method's and add the option "SuprressAsyncSuffixInActionNames = false"
            return CreatedAtAction(nameof(GetByIdAsync), new {id = item.Id}, item);
        }

        //PUT /items/12345 
        [HttpPut("{id}")]
        //IAction because it doesn't return anything, we just need to execute a job with no return, No content
        public async Task <IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = await itemsRepository.GetAsync(id);
            if (existingItem == null)
            {
                return NotFound();
            }
            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await itemsRepository.UpdateAsync(existingItem);

            //This call will publish a message in the MessageBroker throw the lib we alrady installed called MassTransit
            await publishEndPoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description));

            //Nothing to return
            return NoContent();
        }

        //DELETE /items/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var item = await itemsRepository.GetAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            await itemsRepository.RemoveAsync(item.Id);

            //This call will publish a message in the MessageBroker throw the lib we alrady installed called MassTransit
            await publishEndPoint.Publish(new CatalogItemDeleted(id));

            return NoContent();
        }
    }
}