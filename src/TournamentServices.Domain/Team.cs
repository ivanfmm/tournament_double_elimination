using TournamentServices.Domain.Exceptions;
namespace TournamentServices.Domain;

public class Team
{
    //campo
    private string _name = string.Empty; 
    public string Id { get; set; } = string.Empty;

    //Version publica de _name, con validacion de dominio (no puede ser vacio)
    //Propiedad-> se le puede aplicar codigo
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainValidationException("El nombre del equipo no puede estar vacío.");
            }
            _name = value.Trim();
        }
    }

    // Constructor vacío: lo necesita el TeamBuilder (WithId/WithName)
    // y algunos frameworks de mapeo/serialización.
    public Team() { }

    public Team(string id, string name)
    {
        Id = id;
        Name = name;
    }
}