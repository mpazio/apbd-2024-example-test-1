using ExampleTest1.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace ExampleTest1.Repositories;

public class AnimalsRepository : IAnimalsRepository
{
    private readonly IConfiguration _configuration;
    public AnimalsRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> DoesAnimalExist(int id)
    {
        var query = "SELECT 1 FROM Animal WHERE ID = @ID";

        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        command.Parameters.AddWithValue("@ID", id);

        await connection.OpenAsync();

        var res = await command.ExecuteScalarAsync();

        return res is not null;
    }

    public async Task<bool> DoesOwnerExist(int id)
    {
	    var query = "SELECT 1 FROM Owner WHERE ID = @ID";

	    await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
	    await using SqlCommand command = new SqlCommand();

	    command.Connection = connection;
	    command.CommandText = query;
	    command.Parameters.AddWithValue("@ID", id);

	    await connection.OpenAsync();

	    var res = await command.ExecuteScalarAsync();

	    return res is not null;
    }

    public async Task<bool> DoesProcedureExist(int id)
    {
	    var query = "SELECT 1 FROM [Procedure] WHERE ID = @ID";

	    await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
	    await using SqlCommand command = new SqlCommand();

	    command.Connection = connection;
	    command.CommandText = query;
	    command.Parameters.AddWithValue("@ID", id);

	    await connection.OpenAsync();

	    var res = await command.ExecuteScalarAsync();

	    return res is not null;
    }

    public async Task<AnimalDto> GetAnimal(int id)
    {
	    var query = @"SELECT 
							Animal.ID AS AnimalID,
							Animal.Name AS AnimalName,
							Type,
							AdmissionDate,
							Owner.ID as OwnerID,
							FirstName,
							LastName,
							Date,
							[Procedure].Name AS ProcedureName,
							Description
						FROM Animal
						JOIN Owner ON Owner.ID = Animal.Owner_ID
						JOIN Procedure_Animal ON Procedure_Animal.Animal_ID = Animal.ID
						JOIN [Procedure] ON [Procedure].ID = Procedure_Animal.Procedure_ID
						WHERE Animal.ID = @ID";
	    
	    await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
	    await using SqlCommand command = new SqlCommand();

	    command.Connection = connection;
	    command.CommandText = query;
	    command.Parameters.AddWithValue("@ID", id);
	    
	    await connection.OpenAsync();

	    var reader = await command.ExecuteReaderAsync();

	    var animalIdOrdinal = reader.GetOrdinal("AnimalID");
	    var animalNameOrdinal = reader.GetOrdinal("AnimalName");
	    var animalTypeOrdinal = reader.GetOrdinal("Type");
	    var admissionDateOrdinal = reader.GetOrdinal("AdmissionDate");
	    var ownerIdOrdinal = reader.GetOrdinal("OwnerID");
	    var firstNameOrdinal = reader.GetOrdinal("FirstName");
	    var lastNameOrdinal = reader.GetOrdinal("LastName");
	    var dateOrdinal = reader.GetOrdinal("Date");
	    var procedureNameOrdinal = reader.GetOrdinal("ProcedureName");
	    var procedureDescriptionOrdinal = reader.GetOrdinal("Description");

	    AnimalDto animalDto = null;

	    while (await reader.ReadAsync())
	    {
		    if (animalDto is not null)
		    {
			    animalDto.Procedures.Add(new ProcedureDto()
			    {
				    Date = reader.GetDateTime(dateOrdinal),
				    Name = reader.GetString(procedureNameOrdinal),
				    Description = reader.GetString(procedureDescriptionOrdinal)
			    });
		    }
		    else
		    {
			    animalDto = new AnimalDto()
			    {
				    Id = reader.GetInt32(animalIdOrdinal),
				    Name = reader.GetString(animalNameOrdinal),
				    Type = reader.GetString(animalTypeOrdinal),
				    AdmissionDate = reader.GetDateTime(admissionDateOrdinal),
				    Owner = new OwnerDto()
				    {
					    Id = reader.GetInt32(ownerIdOrdinal),
					    FirstName = reader.GetString(firstNameOrdinal),
					    LastName = reader.GetString(lastNameOrdinal),
				    },
				    Procedures = new List<ProcedureDto>()
				    {
					    new ProcedureDto()
					    {
						    Date = reader.GetDateTime(dateOrdinal),
						    Name = reader.GetString(procedureNameOrdinal),
						    Description = reader.GetString(procedureDescriptionOrdinal)
					    }
				    }
			    };
		    }
	    }

	    if (animalDto is null) throw new Exception();
        
        return animalDto;
    }

    public async Task AddNewAnimalWithProcedures(NewAnimalWithProcedures newAnimalWithProcedures)
    {
	    var insert = @"INSERT INTO Animal VALUES(@Name, @Type, @AdmissionDate, @OwnerId);
					   SELECT @@IDENTITY AS ID;";
	    
	    await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
	    await using SqlCommand command = new SqlCommand();
	    
	    command.Connection = connection;
	    command.CommandText = insert;
	    
	    command.Parameters.AddWithValue("@Name", newAnimalWithProcedures.Name);
	    command.Parameters.AddWithValue("@Type", newAnimalWithProcedures.Type);
	    command.Parameters.AddWithValue("@AdmissionDate", newAnimalWithProcedures.AdmissionDate);
	    command.Parameters.AddWithValue("@OwnerId", newAnimalWithProcedures.OwnerId);
	    
	    await connection.OpenAsync();

	    var transaction = await connection.BeginTransactionAsync();
	    command.Transaction = transaction as SqlTransaction;
	    
	    try
	    {
		    var id = await command.ExecuteScalarAsync();
    
		    foreach (var procedure in newAnimalWithProcedures.Procedures)
		    {
			    command.Parameters.Clear();
			    command.CommandText = "INSERT INTO Procedure_Animal VALUES(@ProcedureId, @AnimalId, @Date)";
			    command.Parameters.AddWithValue("@ProcedureId", procedure.ProcedureId);
			    command.Parameters.AddWithValue("@AnimalId", id);
			    command.Parameters.AddWithValue("@Date", procedure.Date);

			    await command.ExecuteNonQueryAsync();
		    }

		    await transaction.CommitAsync();
	    }
	    catch (Exception)
	    {
		    await transaction.RollbackAsync();
		    throw;
	    }
    }

    public async Task<int> AddAnimal(NewAnimalDTO animal)
    {
	    var insert = @"INSERT INTO Animal VALUES(@Name, @Type, @AdmissionDate, @OwnerId);
					   SELECT @@IDENTITY AS ID;";
	    
	    await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
	    await using SqlCommand command = new SqlCommand();
	    
	    command.Connection = connection;
	    command.CommandText = insert;
	    
	    command.Parameters.AddWithValue("@Name", animal.Name);
	    command.Parameters.AddWithValue("@Type", animal.Type);
	    command.Parameters.AddWithValue("@AdmissionDate", animal.AdmissionDate);
	    command.Parameters.AddWithValue("@OwnerId", animal.OwnerId);
	    
	    await connection.OpenAsync();
	    
	    var id = await command.ExecuteScalarAsync();

	    if (id is null) throw new Exception();
	    
	    return Convert.ToInt32(id);
    }

    public async Task AddProcedureAnimal(int animalId, ProcedureWithDate procedure)
    {
	    var query = $"INSERT INTO Procedure_Animal VALUES(@ProcedureID, @AnimalID, @Date)";

	    await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
	    await using SqlCommand command = new SqlCommand();

	    command.Connection = connection;
	    command.CommandText = query;
	    command.Parameters.AddWithValue("@ProcedureID", procedure.ProcedureId);
	    command.Parameters.AddWithValue("@AnimalID", animalId);
	    command.Parameters.AddWithValue("@Date", procedure.Date);

	    await connection.OpenAsync();

	    await command.ExecuteNonQueryAsync();
    }
}