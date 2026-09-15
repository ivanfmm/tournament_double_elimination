using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit; 
using TournamentServices.Domain;
using TournamentServices.Repositories;
using TournamentServices.TestBuilders; // Importamos los builders de tu equipo

namespace TournamentServices.Repositories.Tests
{
    public class GroupRepositoryTests
    {
        // Cumplimos la regla: un nombre de DB único por test para aislamiento
        private TournamentDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<TournamentDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new TournamentDbContext(options);
        }

        [Fact]
        public async Task AddAsync_ShouldSaveGroupInDatabase()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var repository = new GroupRepository(context);
            
            // Usamos el builder predeterminado
            var group = new GroupBuilder().Build();

            // Act
            await repository.AddAsync(group);

            // Assert
            var savedGroup = await context.Groups.FirstOrDefaultAsync(g => g.Id == group.Id);
            Assert.NotNull(savedGroup);
            Assert.Equal(group.Name, savedGroup.Name);
        }
    }
}