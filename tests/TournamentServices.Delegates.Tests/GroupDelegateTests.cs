using System.Threading.Tasks;
using Moq;
using Xunit;
using TournamentServices.Domain;
using TournamentServices.Repositories;
using TournamentServices.Delegates;
using TournamentServices.TestBuilders;

namespace TournamentServices.Delegates.Tests
{
    public class GroupDelegateTests
    {
        [Fact]
        public async Task AddAsync_ShouldCallRepositoryOnce()
        {
            // Arrange (Preparación)
            // Creamos un repositorio simulado (Mock)
            var mockRepository = new Mock<IGroupRepository>();
            var groupDelegate = new GroupDelegate(mockRepository.Object);
            var group = new GroupBuilder().Build();

            // Act (Ejecución)
            await groupDelegate.AddAsync(group);

            // Assert (Verificación)
            // Comprobamos que el delegado haya llamado al método AddAsync del repositorio exactamente 1 vez.
            mockRepository.Verify(repo => repo.AddAsync(group), Times.Once);
        }
    }
}