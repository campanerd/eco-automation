using ECO.Application.Macros.Abstractions;
using ECO.Application.Macros.UseCases;
using ECO.Domain.Macros;

using NSubstitute;

namespace ECO.Application.Tests.Macros.UseCases;

public class ListMacrosUseCaseTests
{
    private readonly IMacroRepository _repository = Substitute.For<IMacroRepository>();
    private readonly ListMacrosUseCase _useCase;

    public ListMacrosUseCaseTests()
    {
        _useCase = new ListMacrosUseCase(_repository);
    }

    [Fact(DisplayName = "ListAsync devolve as macros vindas do repositório")]
    public async Task ListAsync_ReturnsMacrosFromRepository()
    {
        // Arrange
        IReadOnlyList<Macro> saved = [new Macro { Id = 1, Name = "Primeira" }, new Macro { Id = 2, Name = "Segunda" }];
        _repository.GetAllAsync().Returns(saved);

        // Act
        var result = await _useCase.ListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Primeira", result[0].Name);
        Assert.Equal("Segunda", result[1].Name);
    }
}
