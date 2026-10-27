namespace TournamentServices.Domain.Exceptions;

public class DuplicateNameException(string message) : BusinessRuleException(message);