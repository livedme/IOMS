namespace IOMS.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

public class InsufficientStockException : DomainException
{
    public Guid ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientStockException(Guid productId, int requested, int available)
        : base($"Insufficient stock for product {productId}. Requested: {requested}, Available: {available}")
    {
        ProductId = productId;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}

public class JournalEntryImbalanceException : DomainException
{
    public JournalEntryImbalanceException(decimal debits, decimal credits)
        : base($"Journal entry is not balanced. Total debits: {debits}, Total credits: {credits}") { }
}

public class CreditLimitExceededException : DomainException
{
    public CreditLimitExceededException(Guid customerId, decimal limit, decimal currentBalance, decimal orderAmount)
        : base($"Customer {customerId} credit limit ({limit:C}) would be exceeded. Current balance: {currentBalance:C}, Order amount: {orderAmount:C}") { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, Guid id)
        : base($"{entityName} with Id {id} was not found.") { }
}
