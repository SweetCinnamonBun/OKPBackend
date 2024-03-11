using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OKPBackend.Data;
using OKPBackend.Models.Domain;
using OKPBackend.Models.DTO.Favorites;

namespace OKPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ControllerBase
    {
        private readonly OKPDbContext dbContext;
        private readonly IMapper mapper;

        public FavoritesController(OKPDbContext dbContext, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var favorites = await dbContext.Favorites.ToListAsync();
            return Ok(favorites);

        }

        [HttpGet("user-favorites/{userId}")]
        public async Task<IActionResult> GetUserFavorites(string userId)
        {
            // Check if the userId parameter is provided
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required.");
            }

            // Retrieve favorites associated with the provided userId
            var userFavorites = await dbContext.Favorites
                                        .Where(f => f.UserId == userId)
                                        .ToListAsync();


            return Ok(userFavorites);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AddFavoriteDto addFavoriteDto)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == addFavoriteDto.UserId);

            if (user == null)
            {
                return NotFound("User was not found");
            }

            var favorite = mapper.Map<Favorite>(addFavoriteDto);
            var existingFavorite = await dbContext.Favorites
                        .FirstOrDefaultAsync(f => f.UserId == addFavoriteDto.UserId && f.Key == addFavoriteDto.Key);

            if (existingFavorite != null)
            {
                return Conflict("Favorite already exists for this user");
            }

            var response = await dbContext.Favorites.AddAsync(favorite);
            await dbContext.SaveChangesAsync();


            return Ok(mapper.Map<FavoriteDto2>(favorite));

        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var favorite = await dbContext.Favorites.FirstOrDefaultAsync(x => x.Key == id);

            if (favorite == null)
            {
                return BadRequest("Invalid id");
            }

            dbContext.Favorites.Remove(favorite);
            await dbContext.SaveChangesAsync();

            return Ok("Favorite was deleted");

        }
    }
}