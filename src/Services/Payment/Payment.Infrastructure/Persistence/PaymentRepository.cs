using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions;
using Payment.Domain;

namespace Payment.Infrastructure.Persistence;

internal sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context) => _context = context;

    public Task<PaymentRecord?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken)
        => _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId, cancellationToken);

    public async Task AddAsync(PaymentRecord record, CancellationToken cancellationToken)
        => await _context.Payments.AddAsync(record, cancellationToken);
}

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly PaymentDbContext _context;

    public UnitOfWork(PaymentDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
