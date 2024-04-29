using ExampleTest1.Models.DTOs;

namespace ExampleTest1.Repositories;

public interface IAnimalsRepository
{
    Task<bool> DoesAnimalExist(int id);
    Task<bool> DoesOwnerExist(int id);
    Task<bool> DoesProcedureExist(int id);
    Task<AnimalDto> GetAnimal(int id);
    
    // Version with implicit transaction
    Task AddNewAnimalWithProcedures(NewAnimalWithProcedures newAnimalWithProcedures);
    
    // Version with transaction scope
    Task<int> AddAnimal(NewAnimalDTO animal);
    Task AddProcedureAnimal(int animalId, ProcedureWithDate procedure);
}